namespace ReportTestts
{
    /// <summary>
    /// Результат проверки одного клинического критерия
    /// (PTV или OAR)
    /// </summary>
    public class PassFailResult
    {
        /// <summary>
        /// Метрика (Dmax, Dmean, D95, V20Gy и т.п.)
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Строка критерия, например:
        /// "V25Gy ≤ 200 cm³"
        /// </summary>
        public string Criterion { get; set; }

        /// <summary>
        /// Актуальное рассчитанное значение
        /// (например 243)
        /// </summary>
        public double ActualValue { get; set; }

        /// <summary>
        /// Единицы измерения (Gy, %, cm³)
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Прошел / не прошел
        /// </summary>
        public bool IsPass { get; set; }

        /// <summary>
        /// Предупреждение / Без предупреждения
        /// </summary>
        public bool IsWarning { get; set; }

        /// <summary>
        /// Источник (Monaco)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Комментарий (опционально)
        /// </summary>
        public string Comment { get; set; }
    }
}