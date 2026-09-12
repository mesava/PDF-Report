using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportTestts
{
    public enum DoseMetricType
    {
        Dmax,
        Dmean,
        Dx,     // D95, D50 и т.д.
        Vx      // V20, V30 и т.д.
    }

    public enum ConstraintRelation
    {
        LessOrEqual,
        GreaterOrEqual
    }

    public class DoseConstraint
    {
        public string Structure { get; set; }        // PTV, Rectum, Lens_L
        public DoseMetricType MetricType { get; set; }

        public double MetricValue { get; set; }       // x в Dx / Vx
        public double Limit { get; set; }             // порог

        public ConstraintRelation Relation { get; set; }

        public string Unit { get; set; }               // Gy / %
        public string Source { get; set; }             // Monaco / ICRU / RTOG / JSON
        public string Comment { get; set; }
    }
}
