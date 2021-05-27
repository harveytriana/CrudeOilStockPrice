using Microsoft.ML;
using CrudeOilStockPrice.Shared;
using System.Linq;
using System.Collections.Generic;
//
using static System.Console;
using System.Text.Json;
using System.IO;

namespace ConsoleMLApp
{
    class Program
    {
        static readonly string
            DATA_PATH = @"C:\_study\Blazor\Intents\CrudeOilStockPrice\CrudeOilStockPrice\Server\Data\",
            TRAIN_DATA = DATA_PATH + "crudeoil_price-raw.csv",
            MODEL_FILE = DATA_PATH + "crudeoil-price-model.zip";

        static void Main()
        {
            WriteLine("Crude Oil Stock Price");

            // data exloration previous works
            var dataFile = Utils.FilterNoiseLines(TRAIN_DATA);

            Train(dataFile);
        }


        static void Train(string dataFile)
        {
            MLContext mlContext = new(seed: 0);

            var dataView = mlContext.Data.LoadFromTextFile<StockPrice>(dataFile, hasHeader: true, separatorChar: ',');

            Utils.LogDataView(dataView, "Data File");

            // transform data
            var pipeline = mlContext.Transforms
                // the output of the model
                .CopyColumns(outputColumnName: "Label", inputColumnName: "Close")

                // transforms to numeric features
                .Append(mlContext.Transforms.Text.FeaturizeText("DateNumber", "Date"))

                // combines all of the feature columns into the Features column
                .Append(mlContext.Transforms.Concatenate("Features", "DateNumber"))

                // add the learning algorithm
                .Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "Close", featureColumnName: "Features"));

            // train the model
            var model = pipeline.Fit(dataView);

            // EVALUATE
            var crossValidate = mlContext.Regression.CrossValidate(dataView, pipeline, numberOfFolds: 5, labelColumnName: "Close");

            var metrics = new AverageMetrics
            {
                MeanAbsoluteError = crossValidate.Select(r => r.Metrics.MeanAbsoluteError).Average(),
                MeanSquaredError = crossValidate.Select(r => r.Metrics.MeanSquaredError).Average(),
                RootMeanSquaredError = crossValidate.Select(r => r.Metrics.RootMeanSquaredError).Average(),
                LossFunction = crossValidate.Select(r => r.Metrics.LossFunction).Average(),
                RSquared = crossValidate.Select(r => r.Metrics.RSquared).Average()
            };

            WriteLine($"************************************************");
            WriteLine($" Metrics for Regression model");
            WriteLine($"*-----------------------------------------------");
            WriteLine($" Average L1 Loss:       {metrics.MeanAbsoluteError:0.###}");
            WriteLine($" Average L2 Loss:       {metrics.MeanSquaredError:0.###}");
            WriteLine($" Average RMS:           {metrics.RootMeanSquaredError:0.###}");
            WriteLine($" Average Loss Function: {metrics.LossFunction:0.###}");
            WriteLine($" Average R-squared:     {metrics.RSquared:0.###}");
            WriteLine($"************************************************\n");

            WriteLine("Wrap up");
            if (metrics.RSquared > 0.8) {
                mlContext.Model.Save(model, dataView.Schema, MODEL_FILE);
                WriteLine("The model was published.");

                Utils.SaveJsonFile(DATA_PATH + "AverageMetrics.json", metrics, true);

                // optional
                PredictionExample();

                // for ui data
                CreatePredictionsFile(mlContext, dataView, model);
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

            var example = new StockPrice
            {
                Date = "2020-03-22",
                // others fields are not features
            };

            var prediction = predictionEngine.Predict(example);

            WriteLine($"\nPrediction example:");
            WriteLine($"Date: {example.Date}");
            WriteLine($"Predicted Price: {prediction.Score}\n\n");
        }

        private static void CreatePredictionsFile(MLContext mlContext, IDataView dataView, ITransformer model)
        {
            var transformedData = model.Transform(dataView);

            var predictions = mlContext.Data.CreateEnumerable<StockPricePrediction>(transformedData, reuseRowObject: false);

            // save a json file last 125
            Utils.SaveJsonFile(DATA_PATH + "PredictionsPartial.json", predictions.TakeLast(125));
            // save a json file all
            Utils.SaveJsonFile(DATA_PATH + "Predictions.json", predictions);

            // TEST
            // var z = JsonSerializer.Deserialize<List<StockPricePrediction>>(File.ReadAllText(DATA_PATH + "PredictionsPartial.json"));
            // z.ForEach(x => WriteLine(x));
        }
    }
}
