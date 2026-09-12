using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportTestts
{
    public class PTVRxInfo
    {
        public string StructureName { get; set; } = "";
        public double TotalDoseGy { get; set; }
        public int NumberOfFractions { get; set; }

        public double FractionDoseGy =>
            NumberOfFractions > 0
                ? TotalDoseGy / NumberOfFractions
                : 0.0;
    }
}