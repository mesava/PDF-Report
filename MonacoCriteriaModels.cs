using System.Collections.Generic;

namespace ReportTestts
{
    public class MonacoCriteriaRoot
    {
        public List<PrescriptionWrapper> prescriptions { get; set; }
    }

    public class PrescriptionWrapper
    {
        public Prescription prescription { get; set; }
    }

    public class Prescription
    {
        public string structureName { get; set; }
        public List<DoseGoalWrapper> doseGoals { get; set; }
    }

    public class DoseGoalWrapper
    {
        public string doseGoal { get; set; }
    }
}
