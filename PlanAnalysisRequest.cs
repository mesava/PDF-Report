namespace ReportTestts;

public sealed class PlanAnalysisRequest
{
    public required string DicomFolder { get; init; }
    public required string JsonCriteriaFolder { get; init; }
}
