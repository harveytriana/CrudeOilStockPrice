// ==================================
// BlazorSpread.net
// ===================================
using Microsoft.ML;
using System;
using System.Linq;
using CrudeOilStockPrice.Shared;
using System.Threading.Tasks;
using System.Net.Http;
//
using static System.Console;

namespace TrainerConsole
{
    class Program
    {
        static readonly string
            TRAIN_DATA = Utils.DataPath("crudeoil_price-raw.csv"),
            MODEL_FILE = Utils.PublishPath("crudeoil-price-model.zip");

        static void Main()
        {
            WriteLine("Crude Oil Stock Price Model Trainer");

            // data exloration previous works
            var dataFile = Utils.FilterNoiseLines(TRAIN_DATA);

            Train(dataFile);
        }

        static void Train(string dataFile)
        {
            MLContext mlContext = new(seed: 0);

            var dataView = mlContext.Data.LoadFromTextFile<StockPrice>(dataFile, hasHeader: true, separatorChar: ',');

            Utils.LogDataView(dataView, "Data File");

            WriteLine("\nMaking Transforms");

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
            WriteLine($" Average Metrics for Regression model");
            WriteLine($"*-----------------------------------------------");
            WriteLine($" MeanAbsoluteError     {metrics.MeanAbsoluteError:0.###}");
            WriteLine($" MeanSquaredError      {metrics.MeanSquaredError:0.###}");
            WriteLine($" RootMeanSquaredError  {metrics.RootMeanSquaredError:0.###}");
            WriteLine($" LossFunction          {metrics.LossFunction:0.###}");
            WriteLine($" RSquared              {metrics.RSquared:0.###}");
            WriteLine($"************************************************\n");

            WriteLine("Wrap up");
            if (metrics.RSquared > 0.8) {
                mlContext.Model.Save(model, dataView.Schema, MODEL_FILE);
                WriteLine("The model was published.");

                Utils.SaveJsonFile(Utils.PublishPath("AverageMetrics.json"), metrics, true);

                // for ui data
                CreatePredictionsFile(mlContext, dataView, model);
            }
            else {
                WriteLine("The model is not accurate enough.");
            }
        }

        // for blazor page 
        private static void CreatePredictionsFile(MLContext mlContext, IDataView dataView, ITransformer model)
        {
            var transformedData = model.Transform(dataView);
            var predictions = mlContext.Data.CreateEnumerable<StockPricePrediction>(transformedData, reuseRowObject: false);
            // save a json file all
            Utils.SaveJsonFile(Utils.PublishPath("Predictions.json"), predictions);
        }

        // TEST
        //static void PredictionExample()
        //{
        //    var mlContext = new MLContext();
        //    var model = mlContext.Model.Load(MODEL_FILE, out _);
        //    var predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(model);
        //    var example = new StockPrice { Date = "2018-01-01" };
        //    var prediction = predictionEngine.Predict(example);
        //    WriteLine($"\nPrediction example:");
        //    WriteLine($"Date: {example.Date}, Predicted Price: {prediction.Score}\n\n");
        //}

        public static async Task PublishModelToRemoteServer()
        {
            // TODO
            // Upload AverageMetrics.json
            // Upload Predictions.json
            // Upload the model: MODEL_FILE
            // Update remote service for reload model

            // api for upload files
            var serverUrl = "http://localhost:8071";
            var uri = serverUrl + "/api/FileUploader";

            // upload files
            FileUploader.UploadFile(Utils.PublishPath("AverageMetrics.json"), uri).Wait();
            FileUploader.UploadFile(Utils.PublishPath("Predictions.json"), uri).Wait();
            FileUploader.UploadFile(MODEL_FILE, uri).Wait();

            // Update remote service for reload model
            using (var httpClient = new HttpClient()) {
                await httpClient.GetAsync(serverUrl + "/StockPrice/ReloadModel");
            }
        }
    }
}
