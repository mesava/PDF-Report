using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;

namespace PDF_Report_Final
{
    /// <summary>
    /// Represents RTSTRUCT geometry (ROI contours in patient coordinates)
    /// </summary>
    public class DicomStructureSet
    {
        public class ContourSlice
        {
            public double Z { get; set; }
            public List<(double X, double Y)> Polygon { get; set; } = new();
        }

        public class Structure
        {
            public int RoiNumber { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<ContourSlice> Slices { get; set; } = new();
        }

        public List<Structure> Structures { get; } = new();

        public DicomStructureSet(string rtstructPath)
        {
            var file = DicomFile.Open(rtstructPath);
            var ds = file.Dataset;

            // --- ROI definitions ---
            var roiInfo = ds.GetSequence(DicomTag.StructureSetROISequence)
                .Select(seq => new
                {
                    Number = seq.GetSingleValue<int>(DicomTag.ROINumber),
                    Name = seq.GetSingleValue<string>(DicomTag.ROIName)
                })
                .ToDictionary(r => r.Number, r => r.Name);

            // --- ROI contours ---
            var roiContours = ds.GetSequence(DicomTag.ROIContourSequence);

            foreach (var roiContour in roiContours)
            {
                int roiNumber = roiContour.GetSingleValue<int>(DicomTag.ReferencedROINumber);

                if (!roiInfo.ContainsKey(roiNumber))
                    continue;

                Structure structure = new Structure
                {
                    RoiNumber = roiNumber,
                    Name = roiInfo[roiNumber]
                };

                if (!roiContour.Contains(DicomTag.ContourSequence))
                    continue;

                foreach (var contour in roiContour.GetSequence(DicomTag.ContourSequence))
                {
                    double[] data = contour.GetValues<double>(DicomTag.ContourData);

                    // ContourData: x1,y1,z1, x2,y2,z2, ...
                    List<(double X, double Y)> polygon = new();
                    double z = data[2];

                    for (int i = 0; i < data.Length; i += 3)
                        polygon.Add((data[i], data[i + 1]));

                    structure.Slices.Add(new ContourSlice
                    {
                        Z = z,
                        Polygon = polygon
                    });
                }

                Structures.Add(structure);
            }
        }
    }
}
