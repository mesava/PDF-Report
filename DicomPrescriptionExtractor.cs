using System;
using System.Collections.Generic;
using FellowOakDicom;

namespace ReportTestts
{
    public static partial class DicomPrescriptionExtractor
    {
        public static Dictionary<string, PTVRxInfo> ExtractAllPTVRx(string rtPlanPath)
        {
            var result = new Dictionary<string, PTVRxInfo>(StringComparer.OrdinalIgnoreCase);

            var file = DicomFile.Open(rtPlanPath);
            var ds = file.Dataset;

            if (!ds.TryGetSequence(DicomTag.DoseReferenceSequence, out var doseRefSeq))
                return result;

            int numberOfFractions = 0;

            if (ds.TryGetSequence(DicomTag.FractionGroupSequence, out var fracSeq) &&
                fracSeq.Items.Count > 0)
            {
                fracSeq.Items[0].TryGetSingleValue(
                    DicomTag.NumberOfFractionsPlanned,
                    out numberOfFractions);
            }

            foreach (var dr in doseRefSeq.Items)
            {
                if (!dr.Contains(DicomTag.TargetPrescriptionDose))
                    continue;

                double totalDose = dr.GetSingleValue<double>(
                    DicomTag.TargetPrescriptionDose);

                string structureName =
                    dr.TryGetSingleValue(DicomTag.DoseReferenceDescription, out string desc)
                        ? desc
                        : "PTV";

                if (!result.ContainsKey(structureName))
                {
                    result[structureName] = new PTVRxInfo
                    {
                        StructureName = structureName,
                        TotalDoseGy = totalDose,
                        NumberOfFractions = numberOfFractions
                    };
                }
            }

            return result;
        }
    }
}