using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReportTestts
{
    public static class OarClinicalRules
    {
        public static List<PassFailResult> Evaluate(
            DVHResult dvh,
            List<OarCriterion> criteria)
        {
            var results = new List<PassFailResult>();

            foreach (var c in criteria)
            {
                double actual = CalculateActualValue(dvh, c);
                bool pass = Compare(actual, c.Operator, c.Value);

                results.Add(new PassFailResult
                {
                    Metric = c.Metric,
                    Criterion = $"{FormatMetric(c.Metric)} {c.Operator} {c.Value} {FormatUnit(c.Unit)}",
                    ActualValue = actual,
                    Unit = FormatUnit(c.Unit),
                    IsPass = pass,
                    Source = c.Source
                });
            }

            return results;
        }

        // =====================================================
        // ACTUAL VALUE FROM DVH
        // =====================================================
        private static double CalculateActualValue(DVHResult dvh, OarCriterion c)
        {
            string metric = c.Metric
                .ToUpperInvariant()
                .Replace(" ", "");

            // ---------- Dmax ----------
            if (metric == "DMAX")
                return dvh.Max;

            // ---------- Dmean ----------
            if (metric == "DMEAN")
                return dvh.Mean;

            // ---------- Dxx% ----------
            if (metric.StartsWith("D") && metric.EndsWith("%"))
            {
                string number = metric.Substring(1, metric.Length - 2);

                if (double.TryParse(number, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double percent))
                {
                    return dvh.GetDoseAtVolume(percent);
                }
            }

            // ---------- Dxxcm3 / Dxxcm? / Dxxcm³ ----------
            if (metric.StartsWith("D") && metric.Contains("CM"))
            {
                string number = metric
                    .Replace("D", "")
                    .Replace("CM3", "")
                    .Replace("CM³", "")
                    .Replace("CM?", "")
                    .Replace("CM", "");

                if (double.TryParse(number, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double volumeCm3))
                {
                    if (dvh.VolumeCm3 <= 0)
                        return 0;

                    double percent = volumeCm3 / dvh.VolumeCm3 * 100.0;
                    return dvh.GetDoseAtVolume(percent);
                }
            }

            // ---------- VxGy ----------
            if (metric.StartsWith("V") && metric.EndsWith("GY"))
            {
                string number = metric
                    .Replace("V", "")
                    .Replace("GY", "");

                double doseGy = double.Parse(
                    number,
                    CultureInfo.InvariantCulture);

                double volumePercent = dvh.GetVolumeAtDose(doseGy);

                string unit = c.Unit
                    .ToLowerInvariant()
                    .Replace("³", "3")
                    .Replace("^", "");

                if (unit == "%")
                    return volumePercent;

                if (unit.Contains("cm3"))
                    return volumePercent / 100.0 * dvh.VolumeCm3;
            }

            throw new InvalidOperationException(
                $"Unsupported OAR metric: {c.Metric}");
        }

        // =====================================================
        // COMPARISON
        // =====================================================
        private static bool Compare(double actual, string op, double limit)
        {
            return op switch
            {
                "<" => actual < limit,
                "<=" => actual <= limit,
                ">" => actual > limit,
                ">=" => actual >= limit,
                _ => throw new InvalidOperationException(
                        $"Unsupported operator: {op}")
            };
        }

        // =====================================================
        // FORMAT METRIC (pretty)
        // =====================================================
        private static string FormatMetric(string metric)
        {
            metric = metric.ToUpperInvariant();

            if (metric == "DMAX") return "Dmax";
            if (metric == "DMEAN") return "Dmean";

            if (metric.StartsWith("D") && metric.Contains("CM"))
                return metric
                    .Replace("CM3", "cm³")
                    .Replace("CM³", "cm³")
                    .Replace("CM?", "cm³");

            if (metric.StartsWith("V") && metric.EndsWith("GY"))
                return metric.Replace("GY", "Gy");

            return metric;
        }

        // =====================================================
        // FORMAT UNIT (pretty)
        // =====================================================
        private static string FormatUnit(string unit)
        {
            if (unit.Equals("GY", StringComparison.OrdinalIgnoreCase))
                return "Gy";

            if (unit.Contains("cm³") || unit.Contains("CM³"))
                return "cm³";

            return unit;
        }
    }
}
