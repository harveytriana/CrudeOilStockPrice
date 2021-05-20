using Microsoft.ML;
using System;
using CrudeOilStockPrice.Shared;
using System.Linq;
using Microsoft.ML.Transforms;
using Microsoft.ML.Trainers.FastTree;
using System.Collections.Generic;
//
using static System.Console;
using System.Globalization;

namespace ConsoleMLApp
{
    class Program
    {
        static readonly string
            DATA_PATH = @"C:\_study\ML\CrudeOilStockPrice\CrudeOilStockPrice\Server\Data\",
            TRAIN_DATA = DATA_PATH + "crudeoil_price-raw.csv",
            MODEL_FILE = DATA_PATH + "crudeoil-price-model.zip";

        static void Main()
        {
            WriteLine("Crude Oil Stock Price");

            // data exloration previous works
            var dataFile = Utils.CleanNullLines(TRAIN_DATA);

            Train(dataFile);
        }


        static void Train(string dataFile)
        {
            MLContext mlContext = new(seed: 0);

            var dataView = mlContext.Data.LoadFromTextFile<StockPrice>(dataFile, hasHeader: true, separatorChar: ',');

            // transform data
            var pipeline = mlContext.Transforms
                // the output of the model
                .CopyColumns(outputColumnName: "Label", inputColumnName: "Price")

                // transforms to numeric features
                .Append(mlContext.Transforms.Text.FeaturizeText("DateNumber", "Date"))

                // combines all of the feature columns into the Features column
                .Append(mlContext.Transforms.Concatenate("Features", "DateNumber"))

                // add the learning algorithm
                .Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "Price", featureColumnName: "Features"));

            // train the model
            var model = pipeline.Fit(dataView);

            // EVALUATE
            var crossValidate = mlContext.Regression.CrossValidate(dataView, pipeline, numberOfFolds: 5, labelColumnName: "Price");

            var L1 = crossValidate.Select(r => r.Metrics.MeanAbsoluteError);
            var L2 = crossValidate.Select(r => r.Metrics.MeanSquaredError);
            var MS = crossValidate.Select(r => r.Metrics.RootMeanSquaredError);
            var LF = crossValidate.Select(r => r.Metrics.LossFunction);
            var R2 = crossValidate.Select(r => r.Metrics.RSquared);

            WriteLine($"************************************************");
            WriteLine($" Metrics for Regression model");
            WriteLine($"*-----------------------------------------------");
            WriteLine($" Average L1 Loss:       {L1.Average():0.###}");
            WriteLine($" Average L2 Loss:       {L2.Average():0.###}");
            WriteLine($" Average RMS:           {MS.Average():0.###}");
            WriteLine($" Average Loss Function: {LF.Average():0.###}");
            WriteLine($" Average R-squared:     {R2.Average():0.###}");
            WriteLine($"************************************************\n");

            WriteLine("Wrap up");
            if (R2.Average() > 0.8) {
                mlContext.Model.Save(model, dataView.Schema, MODEL_FILE);
                WriteLine("The model was published.");

                // optional
                PredictionExample();

                // optional
                CreatePredictionsTable(mlContext, dataView, model);
            }
            else {
                WriteLine("The model is not accurate enough.");
            }
        }

        static void PredictionExample()
        {
            var mlContext = new MLContext();

            var model = mlContext.Model.Load(MODEL_FILE, out _);

            var predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(model);

            var example = new StockPrice {
                Date = "2000-03-22",
                // others fields are not features
            };

            var prediction = predictionEngine.Predict(example);

            WriteLine($"\nPrediction example:");
            WriteLine($"Date: {example.Date}");
            WriteLine($"Predicted Price: {prediction.Score}\n\n");
        }

        private static void CreatePredictionsTable(MLContext mlContext, IDataView dataView, ITransformer model)
        {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(model);

            var sp = new StockPrice();
            var dates = dataView.Preview().ColumnView[0];
            var prices = dataView.Preview().ColumnView[5];

            var ls = new List<StockPriceCorrelate>();

            for (int i = 0; i < dates.Values.Length; i++) {
                sp.Date = dates.Values[i].ToString();
                sp.Price = float.Parse(prices.Values[i].ToString());
                float p = predictionEngine.Predict(sp).Score;
                WriteLine($"{sp.Date}\t{sp.Price:N2}\t{p:N2}");

                ls.Add(new StockPriceCorrelate {
                    Date = DateTime.ParseExact(sp.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Price = sp.Price,
                    PredictedPrice = p
                });
            }

            // save a json file
            Utils.SaveJsonFile(DATA_PATH + "StockPricePlot.json", ls);
        }

    }
}
