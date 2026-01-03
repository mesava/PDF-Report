using System;
using FellowOakDicom;

namespace ReportTestts
{
    public class PrescriptionInfo
    {
        public string Target { get; set; }
        public double TotalDoseGy { get; set; }
        public int NumberOfFractions { get; set; }

        public double FractionDoseGy =>
            NumberOfFractions > 0
                ? TotalDoseGy / NumberOfFractions
                : 0.0;
    }

    public static class DicomPrescriptionExtractor
    {
        public static PrescriptionInfo Extract(string rtPlanPath)
        {
            var file = DicomFile.Open(rtPlanPath);
            var ds = file.Dataset;

            if (!ds.Contains(DicomTag.DoseReferenceSequence))
                throw new InvalidOperationException(
                    "RTPLAN does not contain DoseReferenceSequence.");

            var doseRefs = ds.GetSequence(DicomTag.DoseReferenceSequence);

            foreach (var dr in doseRefs)
            {
                if (!dr.Contains(DicomTag.TargetPrescriptionDose))
                    continue;

                double rx = dr.GetSingleValue<double>(
                    DicomTag.TargetPrescriptionDose);

                string target = dr.Contains(DicomTag.DoseReferenceDescription)
                    ? dr.GetSingleValue<string>(DicomTag.DoseReferenceDescription)
                    : "PTV";

                int numberOfFractions = 0;

                if (ds.TryGetSequence(DicomTag.FractionGroupSequence, out var fracSeq) &&
                    fracSeq.Items.Count > 0)
                {
                    fracSeq.Items[0].TryGetSingleValue(
                        DicomTag.NumberOfFractionsPlanned,
                        out numberOfFractions);
                }

                return new PrescriptionInfo
                {
                    Target = target,
                    TotalDoseGy = rx,
                    NumberOfFractions = numberOfFractions
                };
            }

            throw new InvalidOperationException(
                "Target Prescription Dose not found in RTPLAN.");
        }
    }
}
