using CrudeOilStockPrice.Server.Services;
using CrudeOilStockPrice.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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

        [HttpGet("GetPredictions")]
        public IEnumerable<StockPricePrediction> GetCorrelate()
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "Predictions.json");

            return JsonSerializer.Deserialize<List<StockPricePrediction>>(jsonData);
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
    }
}
