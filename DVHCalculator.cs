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

                DicomStructureSet.ContourSlice? nearestSlice = null;
                double nearestDistanceMm = double.MaxValue;

                // Manual nearest-slice search preserves the previous stable tie behaviour
                // while avoiding LINQ allocations for every dose frame.
                foreach (var candidate in structure.Slices)
                {
                    double distanceMm = Math.Abs(candidate.Z - z);
                    if (distanceMm < nearestDistanceMm)
                    {
                        nearestDistanceMm = distanceMm;
                        nearestSlice = candidate;
                    }
                }

                if (nearestSlice == null || nearestDistanceMm >= contourPlaneToleranceMm)
                    continue;

                matchedDosePlanes++;
                sumPlaneDistanceMm += nearestDistanceMm;
                maxPlaneDistanceMm = Math.Max(maxPlaneDistanceMm, nearestDistanceMm);

                var bounds = GetPolygonBounds(nearestSlice.Polygon);

                for (int i = 0; i < dose.Rows; i++)
                {
                    for (int j = 0; j < dose.Columns; j++)
                    {
                        // Same patient-coordinate expression as GetVoxelCenter(),
                        // but without allocating a new double[] for every voxel.
                        double x = dose.Origin[0]
                            + j * dose.Dx * dose.RowDir[0]
                            + i * dose.Dy * dose.ColDir[0]
                            + dose.ZOffsets[k] * dose.SliceDir[0];

                        double y = dose.Origin[1]
                            + j * dose.Dx * dose.RowDir[1]
                            + i * dose.Dy * dose.ColDir[1]
                            + dose.ZOffsets[k] * dose.SliceDir[1];

                        // A polygon can contain a point only inside its axis-aligned
                        // patient-coordinate bounding box. This skips expensive
                        // PointInPolygon calls without changing which voxels are accepted.
                        if (x < bounds.MinX || x > bounds.MaxX ||
                            y < bounds.MinY || y > bounds.MaxY)
                        {
                            continue;
                        }

                        if (PointInPolygon(x, y, nearestSlice.Polygon))
                            doses.Add(dose.DoseGy[k, i, j]);
                    }
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

        private static PolygonBounds GetPolygonBounds(
            List<(double X, double Y)> polygon)
        {
            double minX = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;

            foreach (var point in polygon)
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }

            return new PolygonBounds(minX, maxX, minY, maxY);
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

        private readonly record struct PolygonBounds(
            double MinX,
            double MaxX,
            double MinY,
            double MaxY);
    }
}
