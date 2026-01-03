using System.Linq;
using ReportTestts;

namespace ReportTestts
{
    public static class DVHExtensions
    {
        /// <summary>
        /// Средняя доза (Gy) по кумулятивной DVH
        /// </summary>
        public static double GetMeanDose(this DVHResult dvh)
        {
            var pts = dvh.CumulativeDVH
                .OrderBy(p => p.Key)
                .ToList();

            double mean = 0.0;

            for (int i = 1; i < pts.Count; i++)
            {
                double d1 = pts[i - 1].Key;
                double d2 = pts[i].Key;

                double v1 = pts[i - 1].Value / 100.0;
                double v2 = pts[i].Value / 100.0;

                mean += (v1 - v2) * (d1 + d2) / 2.0;
            }

            return mean;
        }

        /// <summary>
        /// Максимальная доза (Gy)
        /// </summary>
        public static double GetMaxDose(this DVHResult dvh)
        {
            return dvh.CumulativeDVH.Keys.Max();
        }
    }
}
