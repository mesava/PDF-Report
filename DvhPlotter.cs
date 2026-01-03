using System.Collections.Generic;
using System.Linq;
using ScottPlot;
using System;

namespace ReportTestts
{
    public static class DVHPlotter
    {
        public static void SaveDVHPlot(
            List<DVHResult> dvhs,
            string outputPath)
        {
            var plot = new Plot();

            // ================== DVH CURVES ==================
            // Соберём список OAR (те, которым нужны уникальные цвета)
            var oarList = dvhs
                .Where(dvh => dvh.CumulativeDVH != null && dvh.CumulativeDVH.Count > 0)
                .Select(dvh => dvh.Structure)
                .Where(name => !name.Contains("CTV", StringComparison.OrdinalIgnoreCase)
                               && !name.Contains("GTV", StringComparison.OrdinalIgnoreCase)
                               && !name.Contains("PTV", StringComparison.OrdinalIgnoreCase)
                               && !name.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            var colors = GenerateColors(Math.Max(1, oarList.Count));
            int colorIndex = 0;

            foreach (var dvh in dvhs)
            {
                if (dvh.CumulativeDVH == null || dvh.CumulativeDVH.Count == 0)
                    continue;

                string name = dvh.Structure;

                // исключаем CTV и GTV
                if (name.Contains("CTV", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("GTV", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var ordered = dvh.CumulativeDVH
                    .OrderBy(p => p.Key)
                    .ToList();

                double[] dose = ordered.Select(p => p.Key).ToArray();
                double[] volume = ordered.Select(p => p.Value).ToArray();

                var scatter = plot.Add.Scatter(dose, volume);
                scatter.LegendText = name;

                // стили по типу структуры
                if (name.Contains("PTV", System.StringComparison.OrdinalIgnoreCase))
                {
                    scatter.Color = Colors.Red;
                    scatter.LineWidth = 3;
                }
                else if (name.Equals("Patient", System.StringComparison.OrdinalIgnoreCase))
                {
                    scatter.Color = Colors.Brown;
                    scatter.LineWidth = 2;
                    scatter.LinePattern = LinePattern.Dashed;
                }
                else
                {
                    // OAR: уникальные цвета, тонкая линия
                    scatter.Color = colors[colorIndex % colors.Length];
                    scatter.LineWidth = 1;
                    colorIndex++;
                }
            }

            // ================== AXES ==================
            plot.Title("Кумулятивная DVH");
            plot.XLabel("Доза (Гр)");
            plot.YLabel("Объём (%)");

            plot.Axes.SetLimits(
                double.NaN,
                double.NaN,
                0,
                100);

            // ================== LEGEND ==================
            plot.ShowLegend(Edge.Bottom);
            plot.Legend.Orientation = Orientation.Horizontal;
            plot.Legend.FontSize = 10;
            plot.Legend.Location = Alignment.LowerCenter;

            plot.Legend.Margin = new PixelPadding(0, 0, 10, 0); // смещение вниз

            // ================== SAVE ==================
            plot.SavePng(outputPath, 900, 750);
        }

        // Генерация N отличимых цветов через равномерное распределение по hue (HSV -> RGB)
        private static ScottPlot.Color[] GenerateColors(int count)
        {
            var result = new ScottPlot.Color[count];
            for (int i = 0; i < count; i++)
            {
                double hue = (360.0 * i) / count;
                result[i] = HsvToColor(hue, 0.65, 0.95);
            }
            return result;
        }

        private static ScottPlot.Color HsvToColor(double h, double s, double v)
        {
            double c = v * s;
            double hh = h / 60.0;
            double x = c * (1 - Math.Abs(hh % 2 - 1));
            double r1 = 0, g1 = 0, b1 = 0;

            if (hh >= 0 && hh < 1) { r1 = c; g1 = x; b1 = 0; }
            else if (hh >= 1 && hh < 2) { r1 = x; g1 = c; b1 = 0; }
            else if (hh >= 2 && hh < 3) { r1 = 0; g1 = c; b1 = x; }
            else if (hh >= 3 && hh < 4) { r1 = 0; g1 = x; b1 = c; }
            else if (hh >= 4 && hh < 5) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            double m = v - c;
            int r = (int)Math.Round((r1 + m) * 255);
            int g = (int)Math.Round((g1 + m) * 255);
            int b = (int)Math.Round((b1 + m) * 255);
            return new ScottPlot.Color(r, g, b);
        }
    }
}
