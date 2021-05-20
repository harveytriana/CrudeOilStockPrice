using System;

namespace CrudeOilStockPrice.Shared
{
    public class StockPriceCorrelate
    {
        public DateTime Date { get; set; }
        public float Price { get; set; }
        public float PredictedPrice { get; set; }
    }
}
