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
        [HttpGet("GetPredictions")]
        public IEnumerable<StockPricePrediction> GetCorrelate()
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "Predictions.json");

            return JsonSerializer.Deserialize<List<StockPricePrediction>>(jsonData);
        }
    }
}
