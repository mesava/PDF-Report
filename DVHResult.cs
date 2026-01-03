using System.Collections.Generic;
using System.Linq;

namespace ReportTestts
{
    /// <summary>
    /// Результат DVH (кумулятивный), Monaco-like
    /// </summary>
    public class DVHResult
    {
        /// <summary>
        /// Имя структуры (PTV, Rectum, Patient и т.д.)
        /// </summary>
        public string Structure { get; set; } = string.Empty;

        /// <summary>
        /// Кумулятивная DVH: Dose (Gy) -> Volume (%)
        /// </summary>
        public Dictionary<double, double> CumulativeDVH { get; set; }
            = new Dictionary<double, double>();

        /// <summary>
        /// Объём структуры (см³)
        /// </summary>
        public double VolumeCm3 { get; set; }

        /// <summary>
        /// Средняя доза (Gy)
        /// </summary>
        public double Mean { get; set; }

        /// <summary>
        /// Максимальная доза (Gy)
        /// </summary>
        public double Max { get; set; }

        // =====================================================
        // Convenience DVH metrics
        // =====================================================

        /// <summary>
        /// Dose at volume v% (e.g. D95)
        /// </summary>
        public double GetDoseAtVolume(double volumePercent)
        {
            // DVH хранится как Dose -> Volume%
            // Нужно найти минимальную дозу, где Volume <= volumePercent
            return CumulativeDVH
                .OrderBy(kv => kv.Key)
                .First(kv => kv.Value <= volumePercent)
                .Key;
        }

        /// <summary>
        /// Volume (%) receiving at least given dose (e.g. V54Gy)
        /// </summary>
        public double GetVolumeAtDose(double doseGy)
        {
            return CumulativeDVH
                .OrderBy(kv => kv.Key)
                .Last(kv => kv.Key <= doseGy)
                .Value;
        }
    }
}
