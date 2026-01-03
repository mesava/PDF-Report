namespace ReportTestts
{
    /// <summary>
    /// Один dosimetric criterion (PASS / FAIL)
    /// </summary>
    public class PassFailResult
    {
        public string Metric { get; set; }   // D95, D2 и т.п.
        public double Value { get; set; }    // численное значение
        public string Unit { get; set; }     // Гр, %, см3
        public bool IsPass { get; set; }     // PASS / FAIL
        public string Criterion { get; set; }    // "D95 ≥ 95% Rx"
        public string Source { get; set; }       // ICRU / RTOG / Local
        public string Comment { get; set; }  // пояснение
    }
}
