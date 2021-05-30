using CrudeOilStockPrice.Shared;
using Microsoft.ML;
using System;

namespace CrudeOilStockPrice.Server.Services
{
    public class StockPricePredictor
    {
        PredictionEngine<StockPrice, StockPricePrediction> _predictionEngine;

        public StockPricePredictor()
        {
            LoadModel();
        }

        public StockPricePrediction Predict(StockPrice input)
        {
            if (_predictionEngine == null) {// unexpected
                return null;
            }
            return _predictionEngine.Predict(input);
        }

        public void LoadModel()
        {
            _predictionEngine = null;
            try {
                var mlContext = new MLContext();
                var mlModel = mlContext.Model.Load(Startup.DATA_PATH + "crudeoil-price-model.zip", out _);
                _predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(mlModel);
            }
            catch (Exception exception) {
                Console.WriteLine("StockPricePredictor Exception:\n" + exception.Message);
            }
        }
    }
}
