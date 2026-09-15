using System.Collections.Generic;

namespace ReportTestts;

public sealed class PlanAnalysisResult
{
    public required List<BeamInfo> Beams { get; init; }
    public required List<DVHResult> Dvhs { get; init; }
    public required DVHResult PatientDvh { get; init; }
    public required Dictionary<string, (PTVMetrics Metrics, List<PassFailResult> Criteria)> PtvResults { get; init; }
    public required Dictionary<string, PTVRxInfo> PtvRxMap { get; init; }
    public required Dictionary<string, List<PassFailResult>> OarResults { get; init; }
    public required List<DVHResult> OarDvhs { get; init; }
    public required List<string> Diagnostics { get; init; }
    public string? MainPtv { get; init; }
    public required string PlanPath { get; init; }
    public required string DosePath { get; init; }
    public required string StructPath { get; init; }
    public required string JsonPath { get; init; }
    public double ContourPlaneToleranceMm { get; init; }
}
