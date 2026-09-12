namespace ReportTestts
{
    /// <summary>
    /// Клинический критерий для органа риска (OAR),
    /// импортируемый из Monaco (JSON)
    /// </summary>
    public class OarCriterion
    {
        /// <summary>
        /// Метрика: Dmax, Dmean, D2, V20Gy, V25Gy и т.п.
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Оператор сравнения: <=, >=, <, >
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Пороговое значение критерия
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Единицы измерения: Gy, %, cm³
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Источник (обычно "Monaco")
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Комментарий (необязательно)
        /// </summary>
        public string Comment { get; set; }
    }
}