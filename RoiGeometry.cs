using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Report_Final
{
    /// <summary>
    /// Geometry utilities for ROI operations (Path B)
    /// </summary>
    public static class RoiGeometry
    {
        /// <summary>
        /// Point-in-polygon test (ray casting)
        /// </summary>
        public static bool IsPointInsidePolygon(
            double x,
            double y,
            List<(double X, double Y)> polygon)
        {
            bool inside = false;
            int n = polygon.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];

                bool intersect =
                    ((pi.Y > y) != (pj.Y > y)) &&
                    (x < (pj.X - pi.X) * (y - pi.Y) / (pj.Y - pi.Y) + pi.X);

                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Checks if voxel center is inside ROI (by Z-slice matching)
        /// </summary>
        public static bool IsVoxelInsideStructure(
            double x,
            double y,
            double z,
            DicomStructureSet.Structure structure,
            double zTolerance = 0.5)
        {
            // find nearest contour slice(s) by Z
            var slicesAtZ = structure.Slices
                .Where(s => Math.Abs(s.Z - z) <= zTolerance)
                .ToList();

            if (!slicesAtZ.Any())
                return false;

            // If multiple contours at same Z, inside any = inside
            foreach (var slice in slicesAtZ)
            {
                if (IsPointInsidePolygon(x, y, slice.Polygon))
                    return true;
            }

            return false;
        }
    }
}
