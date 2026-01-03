using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PDF_Report_Final;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ReportTestts;

namespace ReportTestts
{
    internal class Program
    {
        static void Main()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Console.WriteLine("=== MONACO-LIKE PLAN REPORT ===");

            Console.Write("RTDOSE (.dcm): ");
            string dosePath = Console.ReadLine();

            Console.Write("RTSTRUCT (.dcm): ");
            string structPath = Console.ReadLine();

            Console.Write("RTPLAN (.dcm): ");
            string planPath = Console.ReadLine();

            Console.Write("PDF путь: ");
            string pdfPath = Console.ReadLine();

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

            string dvhImagePath = Path.ChangeExtension(pdfPath, ".dvh.png");

            // =====================================================
            // LOAD DICOM
            // =====================================================
            Console.WriteLine("Загрузка DICOM...");

            var dose = new DicomDoseVolume(dosePath);
            var structureSet = new DicomStructureSet(structPath);
            var rx = DicomPrescriptionExtractor.Extract(planPath);
            var beams = DicomPlanReader.ReadBeams(planPath);

            // =====================================================
            // DVH CALCULATION
            // =====================================================
            Console.WriteLine("Расчёт DVH...");

            var allDvhs = new List<DVHResult>();

            foreach (var s in structureSet.Structures)
            {
                if (IsTechnicalStructure(s.Name))
                {
                    Console.WriteLine($"[SKIP] {s.Name}");
                    continue;
                }

                var dvh = DVHCalculator.CalculateStructure(dose, s);
                allDvhs.Add(dvh);
            }

            var patientDvh = DVHCalculator.CalculatePatient(dose);
            allDvhs.Add(patientDvh);

            // =====================================================
            // SPLIT: PTV / OAR
            // =====================================================
            var ptvDvh = allDvhs.First(d =>
                d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase));

            var oarDvhs = allDvhs
                .Where(d =>
                    !d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase) &&
                    !d.Structure.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"PTV: {ptvDvh.Structure}");
            Console.WriteLine($"OAR count: {oarDvhs.Count}");

            // =====================================================
            // DVH PLOT
            // =====================================================
            Console.WriteLine("Построение DVH-графика...");
            DVHPlotter.SaveDVHPlot(allDvhs, dvhImagePath);

            // =====================================================
            // PTV METRICS
            // =====================================================
            var ptvMetrics = PTVMetricsCalculator.Calculate(
                ptvDvh,
                patientDvh,
                rx.TotalDoseGy);

            // =====================================================
            // CLINICAL RULES
            // =====================================================
            var ptvCriteria = ClinicalRules.EvaluatePTV(ptvMetrics, rx);

            // =====================================================
            // PDF REPORT
            // =====================================================
            Console.WriteLine("Генерация PDF отчёта...");

            var report = new MonacoLikeReport(
                rx,
                ptvMetrics,
                ptvCriteria,          // <<< ВАЖНО
                beams,
                oarDvhs,
                dvhImagePath,
                patientName,
                patientId,
                performedBy,
                checkedBy,
                radiationOncologist
            );

            report.GeneratePdf(pdfPath);


            Console.WriteLine("ГОТОВО");
        }

        // =====================================================
        // TECHNICAL STRUCTURE FILTER
        // =====================================================
        static bool IsTechnicalStructure(string name)
        {
            name = name.ToLower();

            return
                name.Contains("drp") ||
                name.Contains("marker") ||
                name.Contains("poi") ||
                name.Contains("carbon") ||
                name.Contains("foam") ||
                name.Contains("metal") ||
                name.Contains("body") ||
                name.Contains("patient");
        }
    }
}
