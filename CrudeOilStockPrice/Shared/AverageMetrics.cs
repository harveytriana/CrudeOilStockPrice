using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrudeOilStockPrice.Shared
{
    public class AverageMetrics
    {
        public double MeanAbsoluteError { get; set; }
        public double MeanSquaredError { get; set; }
        public double RootMeanSquaredError { get; set; }
        public double LossFunction { get; set; }
        public double RSquared { get; set; }
    }
}
