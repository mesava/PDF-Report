using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportTestts
{
    public class DoseReferenceInfo
    {
        public int DoseReferenceNumber { get; set; }
        public string? StructureName { get; set; }
        public double? TotalDoseGy { get; set; }
        public int? NumberOfFractions { get; set; }

        public override string ToString()
        {
            return $"{StructureName ?? "UNKNOWN"} : {TotalDoseGy} Gy / {NumberOfFractions} fx";
        }
    }

    public static class DoseReferenceReader
    {
        public static List<DoseReferenceInfo> ReadDoseReferences(string rtPlanPath)
        {
            var result = new List<DoseReferenceInfo>();

            var dicom = DicomFile.Open(rtPlanPath);
            var ds = dicom.Dataset;

            // ================= DoseReferenceSequence =================
            if (!ds.TryGetSequence(DicomTag.DoseReferenceSequence, out var doseRefSeq))
                return result;

            foreach (var item in doseRefSeq)
            {
                var info = new DoseReferenceInfo
                {
                    DoseReferenceNumber =
                        item.GetSingleValueOrDefault(DicomTag.DoseReferenceNumber, -1),

                    StructureName =
                        item.GetSingleValueOrDefault<string>(
                            DicomTag.DoseReferenceDescription,
                            null),

                    TotalDoseGy =
                        item.TryGetSingleValue(
                            DicomTag.TargetPrescriptionDose,
                            out double dose)
                            ? dose
                            : (double?)null
                };

                result.Add(info);
            }

            // ================= FractionGroupSequence =================
            if (ds.TryGetSequence(DicomTag.FractionGroupSequence, out var fgSeq))
            {
                foreach (var fg in fgSeq)
                {
                    int fractions =
                        fg.GetSingleValueOrDefault(
                            DicomTag.NumberOfFractionsPlanned, -1);

                    if (fractions <= 0)
                        continue;

                    if (!fg.TryGetSequence(
                            DicomTag.ReferencedDoseReferenceSequence,
                            out var refSeq))
                        continue;

                    foreach (var refItem in refSeq)
                    {
                        int refNum =
                            refItem.GetSingleValueOrDefault(
                                DicomTag.ReferencedDoseReferenceNumber, -1);

                        var match = result.FirstOrDefault(r =>
                            r.DoseReferenceNumber == refNum);

                        if (match != null)
                            match.NumberOfFractions = fractions;
                    }
                }
            }

            return result;
        }
    }
}