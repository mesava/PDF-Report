using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportTestts
{
    public static class JsonCriteriaParser
    {
        // Пример входа: "Dmax <= 21 Gy", "Dmean < 3 Gy", "V20 < 30 %"
        public static DoseConstraint Parse(
            string structure,
            string criterion,
            string source = "JSON")
        {
            var regex = new Regex(
                @"(?<metric>Dmax|Dmean|D\d+|V\d+)\s*(?<op><=|>=|<|>)\s*(?<value>\d+(\.\d+)?)\s*(?<unit>Gy|%)",
                RegexOptions.IgnoreCase);

            var match = regex.Match(criterion);
            if (!match.Success)
                throw new FormatException($"Cannot parse criterion: {criterion}");

            string metric = match.Groups["metric"].Value;
            string op = match.Groups["op"].Value;
            double limit = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
            string unit = match.Groups["unit"].Value;

            var constraint = new DoseConstraint
            {
                Structure = structure,
                Limit = limit,
                Unit = unit,
                Source = source,
                Relation = (op == "<" || op == "<=")
                    ? ConstraintRelation.LessOrEqual
                    : ConstraintRelation.GreaterOrEqual
            };

            if (metric.StartsWith("D"))
            {
                constraint.MetricType =
                    metric == "Dmax" ? DoseMetricType.Dmax :
                    metric == "Dmean" ? DoseMetricType.Dmean :
                    DoseMetricType.Dx;

                if (metric.StartsWith("D") && metric.Length > 1)
                    constraint.MetricValue = double.Parse(metric.Substring(1));
            }
            else if (metric.StartsWith("V"))
            {
                constraint.MetricType = DoseMetricType.Vx;
                constraint.MetricValue = double.Parse(metric.Substring(1));
            }

            return constraint;
        }
    }
}
