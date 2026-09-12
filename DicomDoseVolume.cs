using System;
using System.Linq;
using FellowOakDicom;

namespace PDF_Report_Final
{
    /// <summary>
    /// Represents RTDOSE 3D dose grid in patient coordinates (Gy)
    /// Monaco-compatible
    /// </summary>
    public class DicomDoseVolume
    {
        public int Rows { get; }
        public int Columns { get; }
        public int Frames { get; }

        public double Dx { get; }
        public double Dy { get; }

        public double[] ZOffsets { get; }

        public double[] Origin { get; }
        public double[] RowDir { get; }
        public double[] ColDir { get; }
        public double[] SliceDir { get; }

        public double[,,] DoseGy { get; }

        public DicomDoseVolume(string rtdosePath)
        {
            var file = DicomFile.Open(rtdosePath);
            var ds = file.Dataset;

            // ---------- DIMENSIONS ----------
            Rows = ds.GetSingleValue<int>(DicomTag.Rows);
            Columns = ds.GetSingleValue<int>(DicomTag.Columns);
            Frames = ds.GetSingleValue<int>(DicomTag.NumberOfFrames);

            // ---------- GRID SPACING ----------
            var pixelSpacing = ds.GetValues<double>(DicomTag.PixelSpacing);
            Dx = pixelSpacing[0];
            Dy = pixelSpacing[1];

            ZOffsets = ds.GetValues<double>(DicomTag.GridFrameOffsetVector);

            // ---------- ORIENTATION ----------
            Origin = ds.GetValues<double>(DicomTag.ImagePositionPatient);
            var orientation = ds.GetValues<double>(DicomTag.ImageOrientationPatient);

            RowDir = orientation.Take(3).ToArray();
            ColDir = orientation.Skip(3).Take(3).ToArray();

            SliceDir = Cross(RowDir, ColDir);

            // ---------- DOSE SCALING ----------
            double scaling = ds.GetSingleValue<double>(DicomTag.DoseGridScaling);

            // ---------- PIXEL DATA ----------
            ushort[] raw = ds.GetValues<ushort>(DicomTag.PixelData);

            DoseGy = new double[Frames, Rows, Columns];

            int idx = 0;
            for (int k = 0; k < Frames; k++)
                for (int i = 0; i < Rows; i++)
                    for (int j = 0; j < Columns; j++)
                        DoseGy[k, i, j] = raw[idx++] * scaling;
        }

        /// <summary>
        /// Returns patient coordinates of voxel center (mm)
        /// </summary>
        public double[] GetVoxelCenter(int k, int i, int j)
        {
            return new[]
            {
                Origin[0]
                    + j * Dx * RowDir[0]
                    + i * Dy * ColDir[0]
                    + ZOffsets[k] * SliceDir[0],

                Origin[1]
                    + j * Dx * RowDir[1]
                    + i * Dy * ColDir[1]
                    + ZOffsets[k] * SliceDir[1],

                Origin[2]
                    + j * Dx * RowDir[2]
                    + i * Dy * ColDir[2]
                    + ZOffsets[k] * SliceDir[2]
            };
        }

        private static double[] Cross(double[] a, double[] b)
        {
            return new[]
            {
                a[1]*b[2] - a[2]*b[1],
                a[2]*b[0] - a[0]*b[2],
                a[0]*b[1] - a[1]*b[0]
            };
        }
    }
}
