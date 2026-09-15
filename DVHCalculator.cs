using System;
using System.Collections.Generic;
using System.Linq;
using PDF_Report_Final;

namespace ReportTestts
{
    public readonly record struct DvhPlaneMatchDiagnostics(
        int MatchedDosePlanes,
        double MeanPlaneDistanceMm,
        double MaxPlaneDistanceMm);

    /// <summary>
    /// DVH calculation (voxel-based, Monaco-like)
    /// </summary>
    public static class DVHCalculator
    {
        public const double DefaultContourPlaneToleranceMm = 1.0;

        // ============================================================
        // PATIENT (Unspecified Tissue)
        // ============================================================
        public static DVHResult CalculatePatient(DicomDoseVolume dose)
        {
            var doses = dose.DoseGy.Cast<double>().ToList();

            double dz = GetSliceThickness(dose.ZOffsets);
            double voxelVolumeCm3 = dose.Dx * dose.Dy * dz / 1000.0;

            return BuildDVH("Patient", doses, voxelVolumeCm3);
        }

        // ============================================================
        // STRUCTURE DVH (REAL RTSTRUCT)
        // ============================================================
        public static DVHResult? CalculateStructure(
            DicomDoseVolume dose,
            DicomStructureSet.Structure structure)
        {
            return CalculateStructure(
                dose,
                structure,
                DefaultContourPlaneToleranceMm,
                out _);
        }

        public static DVHResult? CalculateStructure(
            DicomDoseVolume dose,
            DicomStructureSet.Structure structure,
            double contourPlaneToleranceMm,
            out DvhPlaneMatchDiagnostics geometryDiagnostics)
        {
            if (contourPlaneToleranceMm <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(contourPlaneToleranceMm),
                    "Contour plane tolerance must be greater than zero.");

            Console.WriteLine($"[DVH] {structure.Name}");

            var doses = new List<double>();

            double dz = GetSliceThickness(dose.ZOffsets);
            double voxelVolumeCm3 = dose.Dx * dose.Dy * dz / 1000.0;

            int matchedDosePlanes = 0;
            double sumPlaneDistanceMm = 0;
            double maxPlaneDistanceMm = 0;

            for (int k = 0; k < dose.Frames; k++)
            {
                double z = dose.Origin[2] + dose.ZOffsets[k];

                var nearest = structure.Slices
                    .Select(s => new
                    {
                        Slice = s,
                        DistanceMm = Math.Abs(s.Z - z)
                    })
                    .OrderBy(x => x.DistanceMm)
                    .FirstOrDefault();

                if (nearest == null || nearest.DistanceMm >= contourPlaneToleranceMm)
                    continue;

                matchedDosePlanes++;
                sumPlaneDistanceMm += nearest.DistanceMm;
                maxPlaneDistanceMm = Math.Max(maxPlaneDistanceMm, nearest.DistanceMm);

                var slice = nearest.Slice;

                for (int i = 0; i < dose.Rows; i++)
                    for (int j = 0; j < dose.Columns; j++)
                    {
                        var p = dose.GetVoxelCenter(k, i, j);

                        if (PointInPolygon(p[0], p[1], slice.Polygon))
                            doses.Add(dose.DoseGy[k, i, j]);
                    }
            }

            geometryDiagnostics = new DvhPlaneMatchDiagnostics(
                matchedDosePlanes,
                matchedDosePlanes > 0 ? sumPlaneDistanceMm / matchedDosePlanes : 0,
                maxPlaneDistanceMm);

            if (!doses.Any())
            {
                Console.WriteLine(
                    $"[DVH WARNING] No voxels found for structure {structure.Name}");

                return null;
            }

            return BuildDVH(structure.Name, doses, voxelVolumeCm3);
        }

        // ============================================================
        // CORE DVH BUILDER
        // ============================================================
        private static DVHResult BuildDVH(
            string name,
            List<double> doses,
            double voxelVolumeCm3)
        {
            var dvh = BuildCumulativeDVH(doses);

            double volume = doses.Count * voxelVolumeCm3;

            return new DVHResult
            {
                Structure = name,
                VolumeCm3 = volume,
                Mean = doses.Average(),
                Max = doses.Max(),
                CumulativeDVH = dvh
            };
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private static Dictionary<double, double> BuildCumulativeDVH(
            List<double> doses,
            int bins = 200)
        {
            double max = doses.Max();
            double bin = max / bins;

            var dvh = new Dictionary<double, double>();

            for (int i = 0; i <= bins; i++)
            {
                double d = i * bin;
                double v = 100.0 * doses.Count(x => x >= d) / doses.Count;
                dvh[d] = v;
            }

            return dvh;
        }

        private static bool PointInPolygon(
            double x,
            double y,
            List<(double X, double Y)> poly)
        {
            bool inside = false;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];

                bool intersect =
                    ((pi.Y > y) != (pj.Y > y)) &&
                    (x < (pj.X - pi.X) * (y - pi.Y) /
                     (pj.Y - pi.Y) + pi.X);

                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        private static double GetSliceThickness(double[] z)
        {
            if (z.Length < 2) return 1.0;

            double sum = 0;
            for (int i = 1; i < z.Length; i++)
                sum += Math.Abs(z[i] - z[i - 1]);

            return sum / (z.Length - 1);
        }
    }
}
