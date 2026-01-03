using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportTestts
{
    /// <summary>
    /// Результаты расчёта дозиметрических метрик PTV
    /// </summary>
    public class PTVMetrics
    {
        public double D2 { get; set; }
        public double D5 { get; set; }
        public double D50 { get; set; }
        public double D95 { get; set; }
        public double D98 { get; set; }

        public double HI_ICRU { get; set; }
        public double HI_D5_D95 { get; set; }

        public double CI { get; set; }
        public double GI { get; set; }
    }
}
