using System.Collections.Generic;

namespace ReportTestts
{
    public static class ClinicalRules
    {
        public static List<PassFailResult> EvaluatePTV(
            PTVMetrics ptv,
            PTVRxInfo rx)
        {
            var list = new List<PassFailResult>();

            list.Add(new PassFailResult
            {
                Metric = "D95",
                ActualValue = ptv.D95,
                Unit = "Гр",
                IsPass = ptv.D95 >= 0.95 * rx.TotalDoseGy,
                Criterion = "D95% ≥ 95% Rx",
                Source = "ICRU"
            });

            list.Add(new PassFailResult
            {
                Metric = "D50",
                ActualValue = ptv.D50,
                Unit = "Гр",
                IsPass = ptv.D50 >= rx.TotalDoseGy,
                Criterion = "D50% ≥ 100% Rx",
                Source = "ICRU"
            });

            // D2% с расширенным допуском до 110%
            bool d2Pass = ptv.D2 <= 1.07 * rx.TotalDoseGy;
            bool d2Warning = ptv.D2 > 1.07 * rx.TotalDoseGy && ptv.D2 <= 1.10 * rx.TotalDoseGy;
            
            list.Add(new PassFailResult
            {
                Metric = "D2",
                ActualValue = ptv.D2,
                Unit = "Гр",
                IsPass = d2Pass || d2Warning,
                IsWarning = d2Warning,
                Criterion = "D2% ≤ 107% Rx (допуск ≤110%)",
                Source = "ICRU"
            });

            list.Add(new PassFailResult
            {
                Metric = "HI_ICRU",
                ActualValue = ptv.HI_ICRU,
                Unit = "",
                IsPass = ptv.HI_ICRU <= 0.15,
                Criterion = "HI ≤ 0.15",
                Source = "ICRU"
            });

            list.Add(new PassFailResult
            {
                Metric = "HI_D5_D95",
                ActualValue = ptv.HI_D5_D95,
                Unit = "",
                IsPass = ptv.HI_D5_D95 <= 1.1,
                Criterion = "HI ≤ 1.10",
                Source = "ICRU"
            });

            list.Add(new PassFailResult
            {
                Metric = "CI",
                ActualValue = ptv.CI,
                Unit = "",
                IsPass = ptv.CI >= 0.7,
                Criterion = "CI ≥ 0.7",
                Source = "RTOG"
            });

            list.Add(new PassFailResult
            {
                Metric = "GI",
                ActualValue = ptv.GI,
                Unit = "",
                IsPass = ptv.GI <= 3.5,
                Criterion = "GI ≤ 3.5",
                Source = "RTOG"
            });

            return list;
        }
    }
}