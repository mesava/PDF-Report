using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportTestts
{
    public static class DVHPlotter
    {
        public static void SaveDVHPlot(
            List<DVHResult> dvhs,
            string outputPath,
            Dictionary<string, double> ptvRxDoses = null)
        {
            if (dvhs == null || dvhs.Count == 0)
                return;

            string baseDir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(baseDir);

            string dvhTemp = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(outputPath) + "_dvh.png");
            string legendTemp = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(outputPath) + "_legend.png");

            // =========================
            // UNIQUE STRUCTURES
            // =========================
            var uniqueDvhs = dvhs
                .GroupBy(d => d.Structure, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            // =========================
            // MAIN DVH
            // =========================
            var plot = new Plot();

            plot.Title("Кумулятивная DVH", size: 32);
            plot.XLabel("Доза (Гр)", size: 26);
            plot.YLabel("Объём (%)", size: 26);

            plot.Axes.Bottom.TickLabelStyle.FontSize = 18;
            plot.Axes.Left.TickLabelStyle.FontSize = 18;

            double maxDose = uniqueDvhs
                .SelectMany(d => d.CumulativeDVH.Keys)
                .DefaultIfEmpty(0)
                .Max();

            plot.Axes.SetLimits(0, maxDose * 1.05, 0, 100);
            plot.Grid.IsVisible = true;

            // =========================
            // CURVES + Rx LINES
            // =========================
            var ptvRxLines = new Dictionary<string, ScottPlot.Color>(); // Отслеживаем цвета Rx линий
            var ptvColors = new Dictionary<string, ScottPlot.Color>(); // Отслеживаем цвета PTV кривых
            
            foreach (var dvh in uniqueDvhs)
            {
                double[] xs = dvh.CumulativeDVH.Keys.ToArray();
                double[] ys = dvh.CumulativeDVH.Values.ToArray();

                var curve = plot.Add.Scatter(xs, ys);
                curve.LineWidth = 2;

                if (dvh.Structure.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    curve.Color = new ScottPlot.Color(139, 69, 19); // brown
                    curve.LineWidth = 1;
                }
                else if (dvh.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase))
                {
                    // Назначаем разные цвета для каждого PTV
                    var ptvColor = GetPTVColor(ptvColors.Count);
                    curve.Color = ptvColor;
                    curve.LineWidth = 3;
                    ptvColors[dvh.Structure] = ptvColor;

                    // Rx доза для данного PTV
                    double rxDoseGy = 0;
                    if (ptvRxDoses != null && ptvRxDoses.TryGetValue(dvh.Structure, out var rx))
                    {
                        rxDoseGy = rx;
                    }
                    else if (dvh.RxDoseGy.HasValue)
                    {
                        rxDoseGy = dvh.RxDoseGy.Value;
                    }

                    if (rxDoseGy > 0)
                    {
                        ptvRxLines[dvh.Structure] = ptvColor;
                        
                        plot.Add.VerticalLine(
                            rxDoseGy,
                            color: ptvColor,
                            width: 2,
                            pattern: LinePattern.Dashed);
                    }
                }
            }

            plot.SavePng(dvhTemp, 1600, 1000);

            // =========================
            // LEGEND (2 COLUMNS)
            // =========================
            var legend = new Plot();

            legend.Axes.Bottom.IsVisible = false;
            legend.Axes.Left.IsVisible = false;
            legend.Axes.Right.IsVisible = false;
            legend.Axes.Top.IsVisible = false;
            legend.Grid.IsVisible = false;

            int rows = (int)Math.Ceiling(uniqueDvhs.Count / 2.0);
            // Увеличиваем высоту.legend`Legend` с учетом количества PTV для Rx
            int ptvCount = uniqueDvhs.Count(d => d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase));
            legend.Axes.SetLimits(0, 2, -(ptvCount + 1), rows + 1);

            int index = 0;
            double minRow = rows; // Отслеживаем минимальную строку
            
            foreach (var dvh in uniqueDvhs)
            {
                int col = index % 2;
                int row = rows - index / 2;
                minRow = Math.Min(minRow, row);

                double x0 = col == 0 ? 0.1 : 1.1;
                double x1 = x0 + 0.25;

                var line = legend.Add.Line(x0, row, x1, row);
                line.LineWidth = 4;

                if (dvh.Structure.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    line.Color = new ScottPlot.Color(139, 69, 19);
                }
                else if (dvh.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase))
                {
                    // Используем цвет из словаря ptvColors, который уже установлен для этого PTV
                    line.Color = ptvColors[dvh.Structure];
                }

                var text = legend.Add.Text(
                  dvh.Structure,
                  x1 + 0.05,
                  row);

                text.FontSize = 25;

                index++;
            }

            // Добавить обозначение Rx дозы для каждого PTV
            var ptvsWithRx = uniqueDvhs
                .Where(d => d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase))
                .Where(d => ptvRxDoses != null && ptvRxDoses.ContainsKey(d.Structure))
                .ToList();

            double rxStartY = minRow - 1.5;
            for (int i = 0; i < ptvsWithRx.Count; i++)
            {
                double rxY = rxStartY - (i * 1);

                var rxLine = legend.Add.Line(0.1, rxY, 0.35, rxY);
                rxLine.LineWidth = 2;
                rxLine.Color = ptvRxLines[ptvsWithRx[i].Structure];
                rxLine.LinePattern = LinePattern.Dashed;

                string rxLabel = $"СД ({ptvsWithRx[i].Structure})";
                var rxText = legend.Add.Text(rxLabel, 0.4, rxY);
                rxText.FontSize = 25;
            }

            legend.SavePng(legendTemp, 1600, 350);

            // =========================
            // MERGE IMAGES
            // =========================
            using var mainImg = System.Drawing.Image.FromFile(dvhTemp);
            using var legImg = System.Drawing.Image.FromFile(legendTemp);

            int spacing = 20;
            int width = Math.Max(mainImg.Width, legImg.Width);
            int height = mainImg.Height + spacing + legImg.Height;

            using var finalBmp = new System.Drawing.Bitmap(width, height);
            using var g = System.Drawing.Graphics.FromImage(finalBmp);

            g.Clear(System.Drawing.Color.White);
            g.DrawImage(mainImg, (width - mainImg.Width) / 2, 0);
            g.DrawImage(legImg, (width - legImg.Width) / 2, mainImg.Height + spacing);

            finalBmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);

            TryDelete(dvhTemp);
            TryDelete(legendTemp);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static ScottPlot.Color GetPTVColor(int index)
        {
            // Палитра различных цветов для PTV структур
            var colors = new[]
            {
                ScottPlot.Colors.Red,
                ScottPlot.Colors.Blue,
                ScottPlot.Colors.Green,
                ScottPlot.Colors.Orange,
                ScottPlot.Colors.Purple,
                ScottPlot.Colors.Brown,
                ScottPlot.Colors.Pink,
                ScottPlot.Colors.Cyan
            };

            return colors[index % colors.Length];
        }
    }
}
