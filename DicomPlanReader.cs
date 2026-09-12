using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using ReportTestts;

namespace ReportTestts
{
    public class BeamInfo
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public double Energy { get; set; }
        public string Gantry { get; set; } = "";
        public double Collimator { get; set; }
        public double Couch { get; set; }
        public double MU { get; set; }
    }

    public static class DicomPlanReader
    {
        public static List<BeamInfo> ReadBeams(string rtPlanPath)
        {
            var beams = new List<BeamInfo>();

            var file = DicomFile.Open(rtPlanPath);
            var ds = file.Dataset;

            // =========================
            // 1. READ MU FROM FRACTION GROUP
            // =========================
            var muByBeamNumber = new Dictionary<int, double>();

            if (ds.Contains(DicomTag.FractionGroupSequence))
            {
                var fg = ds.GetSequence(DicomTag.FractionGroupSequence).First();

                if (fg.Contains(DicomTag.ReferencedBeamSequence))
                {
                    foreach (var rb in fg.GetSequence(DicomTag.ReferencedBeamSequence))
                    {
                        int beamNumber = rb.GetSingleValueOrDefault(
                            DicomTag.ReferencedBeamNumber, -1);

                        double mu = rb.GetSingleValueOrDefault(
                            DicomTag.BeamMeterset, 0.0);

                        if (beamNumber >= 0)
                            muByBeamNumber[beamNumber] = mu;
                    }
                }
            }

            // =========================
            // 2. READ BEAM GEOMETRY
            // =========================
            if (!ds.Contains(DicomTag.BeamSequence))
                return beams;

            foreach (var beam in ds.GetSequence(DicomTag.BeamSequence))
            {
                int number = beam.GetSingleValueOrDefault(
                    DicomTag.BeamNumber, 0);

                string beamType = beam.GetSingleValueOrDefault(
                    DicomTag.BeamType, string.Empty);

                var info = new BeamInfo
                {
                    Number = number,
                    Name = beam.GetSingleValueOrDefault(
                        DicomTag.BeamName, string.Empty),

                    Type = beamType.Equals("DYNAMIC",
                        StringComparison.OrdinalIgnoreCase)
                        ? "VMAT"
                        : "IMRT",

                    MU = muByBeamNumber.ContainsKey(number)
                        ? muByBeamNumber[number]
                        : 0.0
                };

                // ================= VMAT =================
                if (info.Type == "VMAT" &&
                    beam.Contains(DicomTag.ControlPointSequence))
                {
                    var cps = beam.GetSequence(
                        DicomTag.ControlPointSequence);

                    var first = cps.First();
                    var last = cps.Last();

                    info.Energy = first.GetSingleValueOrDefault(
                        DicomTag.NominalBeamEnergy, 0.0);

                    double gStart = first.GetSingleValueOrDefault(
                        DicomTag.GantryAngle, 0.0);

                    double gEnd = last.GetSingleValueOrDefault(
                        DicomTag.GantryAngle, 0.0);

                    info.Gantry = $"{gStart:F0}° → {gEnd:F0}°";

                    info.Collimator = first.GetSingleValueOrDefault(
                        DicomTag.BeamLimitingDeviceAngle, 0.0);

                    info.Couch = first.GetSingleValueOrDefault(
                        DicomTag.PatientSupportAngle, 0.0);
                }
                // ================= IMRT =================
                else
                {
                    info.Energy = beam.GetSingleValueOrDefault(
                        DicomTag.NominalBeamEnergy, 0.0);

                    info.Gantry = beam.GetSingleValueOrDefault(
                        DicomTag.GantryAngle, 0.0).ToString("F0") + "°";

                    info.Collimator = beam.GetSingleValueOrDefault(
                        DicomTag.BeamLimitingDeviceAngle, 0.0);

                    info.Couch = beam.GetSingleValueOrDefault(
                        DicomTag.PatientSupportAngle, 0.0);
                }

                beams.Add(info);
            }

            return beams;
        }
    }
}

