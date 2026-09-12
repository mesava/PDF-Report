using FellowOakDicom;
using PDF_Report_Final;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReportTestts
{
    internal class Program
    {
        static async Task Main()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Console.WriteLine("=== MONACO RUS PLAN REPORT ===");

            Console.Write("Папка с DICOM (RTPLAN / RTDOSE / RTSTRUCT): ");
            string dicomFolder = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dicomFolder) || !Directory.Exists(dicomFolder))
                return;

            Console.Write("Папка с JSON OAR-критериями: ");
            string jsonFolder = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(jsonFolder) || !Directory.Exists(jsonFolder))
                return;

            string jsonPath = Directory
                .GetFiles(jsonFolder, "*.json")
                .FirstOrDefault();

            if (jsonPath == null)
            {
                Console.WriteLine("❌ JSON не найден");
                return;
            }

            Console.Write("Имя пациента: ");
            string patientName = Console.ReadLine();

            Console.Write("ID пациента: ");
            string patientId = Console.ReadLine();

            Console.Write("Выполнено кем: ");
            string performedBy = Console.ReadLine();

            Console.Write("Проверено кем: ");
            string checkedBy = Console.ReadLine();

            Console.Write("Радиотерапевт: ");
            string radiationOncologist = Console.ReadLine();

            // ================= OUTPUT =================
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string baseName = $"{patientName}_{date}";
            string pdfPath = Path.Combine(desktop, baseName + ".pdf");
            string dvhPath = Path.Combine(desktop, baseName + ".dvh.png");

            // ================= DICOM =================
            string planPath = FindDicom(dicomFolder, "RTPLAN");
            var dose = new DicomDoseVolume(FindDicom(dicomFolder, "RTDOSE"));
            var structs = new DicomStructureSet(FindDicom(dicomFolder, "RTSTRUCT"));
            var beams = DicomPlanReader.ReadBeams(planPath);

            // 🔴 Rx ТОЛЬКО ИЗ RTPLAN (для шапки)
            var planRx = DicomPrescriptionExtractor.ExtractAllPTVRx(planPath)
                                                   .Values
                                                   .FirstOrDefault();

            // ================= JSON =================
            var oarCriteriaDefs = LoadOarCriteria(jsonPath);

            // ================= DVH =================
            var dvhTasks = structs.Structures
                .Where(s => !IsTechnical(s.Name))
                .Select(async s => {
                    try { return await Task.Run(() => DVHCalculator.CalculateStructure(dose, s)); }
                    catch { return null; }
                })
                .ToList();

            var dvhs = (await Task.WhenAll(dvhTasks)).Where(d => d != null).ToList();

            var patientDvh = DVHCalculator.CalculatePatient(dose);

            // ================= PTV =================
            var ptvResults = new Dictionary<string, (PTVMetrics, List<PassFailResult>)>();
            var ptvRxMap = new Dictionary<string, PTVRxInfo>();

            foreach (var ptv in dvhs.Where(d => d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase)))
            {
                // 1️⃣ ищем PTV в JSON
                var jsonKey = oarCriteriaDefs.Keys
                    .FirstOrDefault(k => SameStructure(k, ptv.Structure));

                if (jsonKey == null)
                    continue;

                // 2️⃣ ищем D50% >= Rx Gy
                var d50 = oarCriteriaDefs[jsonKey]
                    .FirstOrDefault(c =>
                        c.Metric.StartsWith("D50", StringComparison.OrdinalIgnoreCase) &&
                        c.Operator == ">=" &&
                        c.Unit.Equals("Gy", StringComparison.OrdinalIgnoreCase));

                if (d50 == null)
                    continue; // ❗ правило: без D50%>=Rx — PTV не показываем

                double rxGy = d50.Value;

                var rx = new PTVRxInfo
                {
                    StructureName = ptv.Structure,
                    TotalDoseGy = rxGy,
                    NumberOfFractions = planRx?.NumberOfFractions ?? 0
                };

                var metrics = PTVMetricsCalculator.Calculate(ptv, patientDvh, rxGy);
                var criteria = ClinicalRules.EvaluatePTV(metrics, rx);

                ptvRxMap[ptv.Structure] = rx;
                ptvResults[ptv.Structure] = (metrics, criteria);
            }

            string mainPtv = ptvResults.Keys.FirstOrDefault();

            // ================= OAR =================
            var oarResults = new Dictionary<string, List<PassFailResult>>();

            foreach (var oar in dvhs.Where(d => !d.Structure.Contains("PTV")))
            {
                var key = oarCriteriaDefs.Keys.FirstOrDefault(k => SameStructure(k, oar.Structure));
                if (key == null) continue;

                oarResults[oar.Structure] =
                    OarClinicalRules.Evaluate(oar, oarCriteriaDefs[key]);
            }

            // ================= DVH PLOT =================
            // Создать dictionary один раз
            var dvhDict = dvhs.ToDictionary(d => d.Structure, StringComparer.OrdinalIgnoreCase);

            // Использовать с проверкой
            DVHPlotter.SaveDVHPlot(
                ptvResults.Keys
                    .Where(k => dvhDict.ContainsKey(k))
                    .Select(k => dvhDict[k])
                .Concat(
                    oarResults.Keys
                        .Where(k => dvhDict.ContainsKey(k))
                        .Select(k => dvhDict[k])
                )
                .Append(patientDvh)
                .ToList(),
                dvhPath,
                ptvRxMap.ToDictionary(kv => kv.Key, kv => kv.Value.TotalDoseGy));

            // ================= PDF =================
            var report = new MonacoLikeReport(
                ptvRxMap,
                ptvResults,
                mainPtv,
                beams,
                dvhs.Where(d => oarResults.ContainsKey(d.Structure)).ToList(),
                dvhPath,
                patientName,
                patientId,
                performedBy,
                checkedBy,
                radiationOncologist,
                oarResults
            );

            report.GeneratePdf(pdfPath);
            Console.WriteLine("ГОТОВО");
            Console.WriteLine($"✅ Загружено {dvhs.Count} структур");
            Console.WriteLine($"✅ Обработано {ptvResults.Count} PTV");
            Console.WriteLine($"✅ Обработано {oarResults.Count} OAR");
            Console.WriteLine($"✅ PDF сохранён: {pdfPath}");
        }

        // ================= HELPERS =================
        static bool IsTechnical(string n)
        {
            n = n.ToLowerInvariant();
            return n.Contains("marker") || n.Contains("poi") ||
                   n.Contains("carbon") || n.Contains("foam") ||
                   n.Contains("metal") || n.Contains("body") ||
                   n.Contains("gtv") || n.Contains("ctv");
        }


        static bool SameStructure(string a, string b)
            => Normalize(a) == Normalize(b);

        static string Normalize(string s)
            => s.ToUpperInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "");

        static string FindDicom(string folder, string modality)
        {
            foreach (var f in Directory.GetFiles(folder, "*.dcm"))
            {
                try
                {
                    var d = DicomFile.Open(f);
                    if (d.Dataset.GetSingleValue<string>(DicomTag.Modality) == modality)
                        return f;
                }
                catch { }
            }
            throw new FileNotFoundException(modality);
        }

        // JSON helpers — без изменений
        static Dictionary<string, List<OarCriterion>> LoadOarCriteria(string path)
        {
            var root = JsonSerializer.Deserialize<MonacoCriteriaRoot>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return root?.prescriptions?
                .Where(p => p.prescription?.doseGoals != null)
                .ToDictionary(
                    p => p.prescription.structureName,
                    p => p.prescription.doseGoals
                        .Select(g => ParseDoseGoal(g.doseGoal))
                        .Where(c => c != null)
                        .ToList()
                )
                ?? new();
        }

        private static readonly Regex DoseGoalRegex = new Regex(
            @"(?<m>DMEAN|DMAX|D\d+(CM\?|CM3|CM\^3|CM³|%)?|V\d+GY)\s*(?<op><=|>=|<|>)\s*(?<v>\d+(\.\d+)?)\s*(?<u>%|GY|CM3|CM\?|CM\^3|CM³)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        static OarCriterion? ParseDoseGoal(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return null;

            string text = Regex.Replace(rawText, @"\([^)]*\)", "")
                .Replace(",", ".")
                .Trim();

            var match = DoseGoalRegex.Match(text);  // Используем compiled

            if (!match.Success)
                return null;


            return new OarCriterion
            {
                Metric = match.Groups["m"].Value.ToUpperInvariant(),
                Operator = match.Groups["op"].Value,
                Value = double.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture),
                Unit = match.Groups["u"].Value
               .ToUpperInvariant()
               .Replace("CM^3", "cm³")
               .Replace("CM3", "cm³")
               .Replace("CM³", "cm³")
               .Replace("CM?", "cm³")
               .Replace("GY", "Gy"),
                Source = "Monaco"
            };
        }
    }
}
