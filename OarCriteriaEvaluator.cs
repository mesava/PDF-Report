using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportTestts
{
    public static class OarCriteriaEvaluator
    {
        public static List<PassFailResult> Evaluate(
            DVHResult dvh,
            List<DoseConstraint> constraints)
        {
            var results = new List<PassFailResult>();

            var structureConstraints = constraints
                .Where(c => c.Structure.Equals(dvh.Structure, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var constraint in structureConstraints)
            {
                double actualValue = CalculateMetric(dvh, constraint);

                bool isPass = constraint.Relation switch
                {
                    ConstraintRelation.LessOrEqual => actualValue <= constraint.Limit,
                    ConstraintRelation.GreaterOrEqual => actualValue >= constraint.Limit,
                    _ => true
                };

                results.Add(new PassFailResult
                {
                    Metric = BuildMetricName(constraint),
                    ActualValue = actualValue,
                    Unit = constraint.Unit,
                    IsPass = isPass,
                    Criterion = $"{constraint.MetricType} {constraint.Relation} {constraint.Limit} {constraint.Unit}",
                    Source = constraint.Source,
                    Comment = constraint.Comment
                });
            }

            return results;
        }
        private static double CalculateMetric(DVHResult dvh, DoseConstraint c)
        {
            return c.MetricType switch
            {
                DoseMetricType.Dmax => dvh.Max,
                DoseMetricType.Dmean => dvh.Mean,

                DoseMetricType.Dx => dvh.GetDoseAtVolume(c.MetricValue),
                DoseMetricType.Vx => dvh.GetVolumeAtDose(c.MetricValue),

                _ => 0.0
            };
        }
        private static string BuildMetricName(DoseConstraint c)
        {
            return c.MetricType switch
            {
                DoseMetricType.Dmax => "Dmax",
                DoseMetricType.Dmean => "Dmean",
                DoseMetricType.Dx => $"D{c.MetricValue}",
                DoseMetricType.Vx => $"V{c.MetricValue}",
                _ => "Metric"
            };
        }
    }
}
