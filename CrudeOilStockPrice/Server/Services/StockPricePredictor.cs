// ==================================
// BlazorSpread.net
// ===================================
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
            if (_predictionEngine == null) {
                return null; 
            }
            return _predictionEngine.Predict(input);
        }

        public bool LoadModel()
        {
            _predictionEngine = null;
            try {
                var mlContext = new MLContext();
                var mlModel = mlContext.Model.Load(Startup.DATA_PATH + "crudeoil-price-model.zip", out _);
                _predictionEngine = mlContext.Model.CreatePredictionEngine<StockPrice, StockPricePrediction>(mlModel);
                return true;
            }
            catch (Exception exception) {
                Console.WriteLine("StockPricePredictor Exception:\n" + exception.Message);
            }
            return false;
        }
    }
}
