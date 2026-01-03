using System.Collections.Generic;

namespace ReportTestts
{
    public static class ClinicalRules
    {
        public static List<PassFailResult> EvaluatePTV(
            PTVMetrics ptv,
            PrescriptionInfo rx)
        {
            var list = new List<PassFailResult>();

            // ---------------- DOSE COVERAGE ----------------

            list.Add(new PassFailResult
            {
                Metric = "D95",
                Value = ptv.D95,
                Unit = "Гр",
                IsPass = ptv.D95 >= 0.95 * rx.TotalDoseGy,
                Criterion = "D95% ≥ 95% Rx",
                Source = "ICRU",
                Comment = "D95 ≥ 95% от предписанной дозы"
            });

            list.Add(new PassFailResult
            {
                Metric = "D50",
                Value = ptv.D50,
                Unit = "Гр",
                IsPass = ptv.D50 >= rx.TotalDoseGy,
                Criterion = "D50% ≥ 100% Rx",
                Source = "ICRU",
                Comment = "D50 ≥ 100% от предписанной дозы"
            });

            list.Add(new PassFailResult
            {
                Metric = "D2",
                Value = ptv.D2,
                Unit = "Гр",
                IsPass = ptv.D2 <= 1.07 * rx.TotalDoseGy,
                Criterion = "D2% ≤ 107% Rx",
                Source = "ICRU",
                Comment = "D2 ≤ 107% от предписанной дозы"
            });

            // ---------------- HOMOGENEITY ----------------

            list.Add(new PassFailResult
            {
                Metric = "HI_ICRU",
                Value = ptv.HI_ICRU,
                Unit = "",
                IsPass = ptv.HI_ICRU <= 0.15,
                Criterion = "HI ≤ 0.15",
                Source = "ICRU",
                Comment = "HI (ICRU) ≤ 0.15"
            });

            list.Add(new PassFailResult
            {
                Metric = "HI_D5_D95",
                Value = ptv.HI_D5_D95,
                Unit = "",
                IsPass = ptv.HI_D5_D95 <= 1.1,
                Criterion = "HI ≤ 1.10",
                Source = "ICRU",
                Comment = "HI (D5/D95) ≤ 1.10"
            });

            // ---------------- CONFORMITY ----------------

            list.Add(new PassFailResult
            {
                Metric = "CI",
                Value = ptv.CI,
                Unit = "",
                IsPass = ptv.CI >= 0.7,
                Criterion = "CI ≥ 0.7",
                Source = "RTOG",
                Comment = "Индекс конформности (Paddick)"
            });

            list.Add(new PassFailResult
            {
                Metric = "GI",
                Value = ptv.GI,
                Unit = "",
                IsPass = ptv.GI <= 3.5,
                Criterion = "GI ≤ 3.5",
                Source = "RTOG",
                Comment = "Градиентный индекс"
            });

            return list;
        }
    }
}
