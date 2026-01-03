using System;
using System.Linq;
using PDF_Report_Final;
using ReportTestts;

namespace ReportTestts
{
    public static class PTVMetricsCalculator
    {
        /// <summary>
        /// Расчёт PTV-метрик (D*, HI, CI, GI)
        /// CI и GI считаются по Patient DVH (как в Monaco)
        /// </summary>
        public static PTVMetrics Calculate(
            DVHResult ptvDvh,
            DVHResult patientDvh,
            double rxDoseGy)
        {
            if (ptvDvh == null)
                throw new ArgumentNullException(nameof(ptvDvh));
            if (patientDvh == null)
                throw new ArgumentNullException(nameof(patientDvh));

            double D(double v) => GetDoseAtVolume(ptvDvh, v);

            var metrics = new PTVMetrics
            {
                D2 = D(2),
                D5 = D(5),
                D50 = D(50),
                D95 = D(95),
                D98 = D(98)
            };

            // ---------- HI ----------
            metrics.HI_ICRU =
                (metrics.D2 - metrics.D98) / metrics.D50;

            metrics.HI_D5_D95 =
                metrics.D5 / metrics.D95;

            // ---------- CI / GI ----------
            double Vptv = ptvDvh.VolumeCm3;

            double V100 = GetVolumeAtDose(patientDvh, rxDoseGy);
            double V50 = GetVolumeAtDose(patientDvh, 0.5 * rxDoseGy);

            metrics.CI = V100 / Vptv;
            metrics.GI = V50 / V100;

            return metrics;
        }

        // ================= HELPERS =================

        private static double GetDoseAtVolume(
            DVHResult dvh,
            double volumePct)
        {
            return dvh.CumulativeDVH
                .OrderByDescending(p => p.Key)
                .First(p => p.Value >= volumePct)
                .Key;
        }

        private static double GetVolumeAtDose(
            DVHResult dvh,
            double doseGy)
        {
            return dvh.CumulativeDVH
                .Where(p => p.Key <= doseGy)
                .Last().Value * dvh.VolumeCm3 / 100.0;
        }
    }
}
