using FellowOakDicom;
using PDF_Report_Final;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReportTestts;

public sealed class PlanAnalysisService
{
    private static readonly Regex DoseGoalRegex = new(
        @"(?<m>DMEAN|DMAX|D\d+(CM\?|CM3|CM\^3|CM³|%)?|V\d+GY)\s*(?<op><=|>=|<|>)\s*(?<v>\d+(\.\d+)?)\s*(?<u>%|GY|CM3|CM\?|CM\^3|CM³)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<PlanAnalysisResult> AnalyzeAsync(PlanAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.DicomFolder) || !Directory.Exists(request.DicomFolder))
            throw new DirectoryNotFoundException($"DICOM folder not found: {request.DicomFolder}");

        if (string.IsNullOrWhiteSpace(request.JsonCriteriaFolder) || !Directory.Exists(request.JsonCriteriaFolder))
            throw new DirectoryNotFoundException($"JSON criteria folder not found: {request.JsonCriteriaFolder}");

        if (request.ContourPlaneToleranceMm <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.ContourPlaneToleranceMm),
                "Contour plane tolerance must be greater than zero.");

        string jsonPath = Directory.GetFiles(request.JsonCriteriaFolder, "*.json").FirstOrDefault()
            ?? throw new FileNotFoundException("JSON criteria file not found.");

        string planPath = FindDicom(request.DicomFolder, "RTPLAN");
        string dosePath = FindDicom(request.DicomFolder, "RTDOSE");
        string structPath = FindDicom(request.DicomFolder, "RTSTRUCT");

        var dose = new DicomDoseVolume(dosePath);
        var structs = new DicomStructureSet(structPath);
        var beams = DicomPlanReader.ReadBeams(planPath);

        var planRx = DicomPrescriptionExtractor.ExtractAllPTVRx(planPath)
            .Values
            .FirstOrDefault();

        var oarCriteriaDefs = LoadOarCriteria(jsonPath);

        var dvhTasks = structs.Structures
            .Where(s => !IsTechnical(s.Name))
            .Select(s => Task.Run(() => CalculateStructure(
                dose,
                s,
                request.ContourPlaneToleranceMm)))
            .ToList();

        var calculations = await Task.WhenAll(dvhTasks);

        var diagnostics = calculations
            .Where(x => !string.IsNullOrWhiteSpace(x.Diagnostic))
            .Select(x => x.Diagnostic!)
            .ToList();

        var dvhs = calculations
            .Where(x => x.Dvh != null)
            .Select(x => x.Dvh!)
            .ToList();

        var patientDvh = DVHCalculator.CalculatePatient(dose);

        var ptvResults = new Dictionary<string, (PTVMetrics Metrics, List<PassFailResult> Criteria)>();
        var ptvRxMap = new Dictionary<string, PTVRxInfo>();

        foreach (var ptv in dvhs.Where(d => d.Structure.Contains("PTV", StringComparison.OrdinalIgnoreCase)))
        {
            var jsonKey = oarCriteriaDefs.Keys
                .FirstOrDefault(k => SameStructure(k, ptv.Structure));

            if (jsonKey == null)
                continue;

            var d50 = oarCriteriaDefs[jsonKey]
                .FirstOrDefault(c =>
                    c.Metric.StartsWith("D50", StringComparison.OrdinalIgnoreCase) &&
                    c.Operator == ">=" &&
                    c.Unit.Equals("Gy", StringComparison.OrdinalIgnoreCase));

            if (d50 == null)
                continue;

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

        string? mainPtv = ptvResults.Keys.FirstOrDefault();

        var oarResults = new Dictionary<string, List<PassFailResult>>();

        // Intentionally preserves the current filtering behaviour during this refactor.
        foreach (var oar in dvhs.Where(d => !d.Structure.Contains("PTV")))
        {
            var key = oarCriteriaDefs.Keys
                .FirstOrDefault(k => SameStructure(k, oar.Structure));

            if (key == null)
                continue;

            oarResults[oar.Structure] =
                OarClinicalRules.Evaluate(oar, oarCriteriaDefs[key]);
        }

        var oarDvhs = dvhs
            .Where(d => oarResults.ContainsKey(d.Structure))
            .ToList();

        return new PlanAnalysisResult
        {
            Beams = beams,
            Dvhs = dvhs,
            PatientDvh = patientDvh,
            PtvResults = ptvResults,
            PtvRxMap = ptvRxMap,
            MainPtv = mainPtv,
            OarResults = oarResults,
            OarDvhs = oarDvhs,
            Diagnostics = diagnostics,
            PlanPath = planPath,
            DosePath = dosePath,
            StructPath = structPath,
            JsonPath = jsonPath,
            ContourPlaneToleranceMm = request.ContourPlaneToleranceMm
        };
    }

    private static StructureDvhCalculation CalculateStructure(
        DicomDoseVolume dose,
        DicomStructureSet.Structure structure,
        double contourPlaneToleranceMm)
    {
        try
        {
            var dvh = DVHCalculator.CalculateStructure(
                dose,
                structure,
                contourPlaneToleranceMm,
                out var geometry);

            string geometryText =
                $"matched planes={geometry.MatchedDosePlanes}, " +
                $"mean ΔZ={geometry.MeanPlaneDistanceMm:F3} mm, " +
                $"max ΔZ={geometry.MaxPlaneDistanceMm:F3} mm, " +
                $"tolerance={contourPlaneToleranceMm:F3} mm";

            return dvh == null
                ? new StructureDvhCalculation(
                    null,
                    $"[DVH WARNING] {structure.Name}: no dose voxels; {geometryText}.")
                : new StructureDvhCalculation(
                    dvh,
                    $"[DVH GEOMETRY] {structure.Name}: {geometryText}.");
        }
        catch (Exception ex)
        {
            // Keep the current behaviour of skipping failed structures,
            // but surface the exact reason in diagnostics.
            return new StructureDvhCalculation(
                null,
                $"[DVH ERROR] {structure.Name}: {ex.Message}");
        }
    }

    private static bool IsTechnical(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("marker") || n.Contains("poi") ||
               n.Contains("carbon") || n.Contains("foam") ||
               n.Contains("metal") || n.Contains("body") ||
               n.Contains("gtv") || n.Contains("ctv");
    }

    private static bool SameStructure(string a, string b)
        => Normalize(a) == Normalize(b);

    private static string Normalize(string value)
        => value.ToUpperInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace(".", "");

    private static string FindDicom(string folder, string modality)
    {
        foreach (var filePath in Directory.GetFiles(folder, "*.dcm"))
        {
            try
            {
                var dicom = DicomFile.Open(filePath);
                if (dicom.Dataset.GetSingleValue<string>(DicomTag.Modality) == modality)
                    return filePath;
            }
            catch
            {
                // Preserve the current discovery behaviour: unreadable files are ignored.
            }
        }

        throw new FileNotFoundException(modality);
    }

    private static Dictionary<string, List<OarCriterion>> LoadOarCriteria(string path)
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
                    .Select(c => c!)
                    .ToList())
            ?? new Dictionary<string, List<OarCriterion>>();
    }

    private static OarCriterion? ParseDoseGoal(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return null;

        string text = Regex.Replace(rawText, @"\([^)]*\)", "")
            .Replace(",", ".")
            .Trim();

        var match = DoseGoalRegex.Match(text);

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

    private sealed record StructureDvhCalculation(DVHResult? Dvh, string? Diagnostic);
}
