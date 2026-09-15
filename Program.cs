using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ReportTestts;

internal class Program
{
    static async Task Main()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Console.WriteLine("=== MONACO RUS PLAN REPORT ===");

        Console.Write("Папка с DICOM (RTPLAN / RTDOSE / RTSTRUCT): ");
        string? dicomFolder = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(dicomFolder) || !Directory.Exists(dicomFolder))
            return;

        Console.Write("Папка с JSON OAR-критериями: ");
        string? jsonFolder = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(jsonFolder) || !Directory.Exists(jsonFolder))
            return;

        Console.Write("Имя пациента: ");
        string patientName = Console.ReadLine() ?? string.Empty;

        Console.Write("ID пациента: ");
        string patientId = Console.ReadLine() ?? string.Empty;

        Console.Write("Выполнено кем: ");
        string performedBy = Console.ReadLine() ?? string.Empty;

        Console.Write("Проверено кем: ");
        string checkedBy = Console.ReadLine() ?? string.Empty;

        Console.Write("Радиотерапевт: ");
        string radiationOncologist = Console.ReadLine() ?? string.Empty;

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string baseName = $"{patientName}_{date}";
        string pdfPath = Path.Combine(desktop, baseName + ".pdf");
        string dvhPath = Path.Combine(desktop, baseName + ".dvh.png");

        PlanAnalysisResult analysis;

        try
        {
            var service = new PlanAnalysisService();
            analysis = await service.AnalyzeAsync(new PlanAnalysisRequest
            {
                DicomFolder = dicomFolder,
                JsonCriteriaFolder = jsonFolder
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка анализа: {ex.Message}");
            return;
        }

        foreach (string diagnostic in analysis.Diagnostics)
            Console.WriteLine(diagnostic);

        var dvhDict = analysis.Dvhs
            .ToDictionary(d => d.Structure, StringComparer.OrdinalIgnoreCase);

        DVHPlotter.SaveDVHPlot(
            analysis.PtvResults.Keys
                .Where(k => dvhDict.ContainsKey(k))
                .Select(k => dvhDict[k])
                .Concat(
                    analysis.OarResults.Keys
                        .Where(k => dvhDict.ContainsKey(k))
                        .Select(k => dvhDict[k]))
                .Append(analysis.PatientDvh)
                .ToList(),
            dvhPath,
            analysis.PtvRxMap.ToDictionary(kv => kv.Key, kv => kv.Value.TotalDoseGy));

        var report = new MonacoLikeReport(
            analysis.PtvRxMap,
            analysis.PtvResults,
            analysis.MainPtv ?? string.Empty,
            analysis.Beams,
            analysis.OarDvhs,
            dvhPath,
            patientName,
            patientId,
            performedBy,
            checkedBy,
            radiationOncologist,
            analysis.OarResults);

        report.GeneratePdf(pdfPath);

        Console.WriteLine("ГОТОВО");
        Console.WriteLine($"✅ Загружено {analysis.Dvhs.Count} структур");
        Console.WriteLine($"✅ Обработано {analysis.PtvResults.Count} PTV");
        Console.WriteLine($"✅ Обработано {analysis.OarResults.Count} OAR");
        Console.WriteLine($"✅ PDF сохранён: {pdfPath}");
    }
}
