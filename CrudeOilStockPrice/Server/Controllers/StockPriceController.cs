using CrudeOilStockPrice.Server.Services;
using CrudeOilStockPrice.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using IO = System.IO;

namespace CrudeOilStockPrice.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        StockPricePredictor _predictor;

        public StockPriceController(StockPricePredictor predictor)
        {
            _predictor = predictor;
        }

        [HttpGet("GetPredictions/{takeLast}")]
        public IEnumerable<StockPricePrediction> GetPredictions(int takeLast)
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "Predictions.json");
            var data = JsonSerializer.Deserialize<List<StockPricePrediction>>(jsonData);
            if (takeLast < 0) {
                return data;
            }
            return data.ToList().TakeLast(takeLast);
        }

        [HttpGet("GetMetrics")]
        public AverageMetrics GetMetrics()
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "AverageMetrics.json");

            return JsonSerializer.Deserialize<AverageMetrics>(jsonData);
        }

        [HttpPost("Prediction")]
        public StockPricePrediction Prediction([FromBody] StockPrice input)
        {
            return _predictor.Predict(input);
        }

        [HttpGet("ReloadModel")]
        // Run from Trainer is the model was updated.
        // In a production case it should have authorization
        public void ReloadModel()
        {
            _predictor.LoadModel();
        }
    }
}
