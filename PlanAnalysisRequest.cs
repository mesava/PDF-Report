namespace ReportTestts;

public sealed class PlanAnalysisRequest
{
    public required string DicomFolder { get; init; }
    public required string JsonCriteriaFolder { get; init; }

    // Regression baseline confirmed against examples/65.pdf.
    // Do not change without a separate geometry validation against Monaco.
    public double ContourPlaneToleranceMm { get; init; } = 1.0;
}
