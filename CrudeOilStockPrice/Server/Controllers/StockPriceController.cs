using CrudeOilStockPrice.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

namespace CrudeOilStockPrice.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        [HttpGet("GetCorrelate")]
        public IEnumerable<StockPriceCorrelate> GetCorrelate()
        {
            var jsonData = IO.File.ReadAllText(Startup.DATA_PATH + "StockPricePlot.json");

            return JsonSerializer.Deserialize<List<StockPriceCorrelate>>(jsonData);
        }
    }
}
