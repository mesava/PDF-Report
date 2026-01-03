using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace ReportTestts
{
    public class FinalPdfReport
    {
        private readonly PrescriptionInfo _rx;
        private readonly PTVMetrics _ptv;
        private readonly List<BeamInfo> _beams;
        private readonly string _dvhImagePath;

        private readonly string _patientName;
        private readonly string _patientId;
        private readonly string _performedBy;
        private readonly string _checkedBy;
        private readonly string _radiationOncologist;

        public FinalPdfReport(
            PrescriptionInfo rx,
            PTVMetrics ptvMetrics,
            List<BeamInfo> beams,
            string dvhImagePath,
            string patientName,
            string patientId,
            string performedBy,
            string checkedBy,
            string radiationOncologist)
        {
            _rx = rx;
            _ptv = ptvMetrics;
            _beams = beams;
            _dvhImagePath = dvhImagePath;

            _patientName = patientName;
            _patientId = patientId;
            _performedBy = performedBy;
            _checkedBy = checkedBy;
            _radiationOncologist = radiationOncologist;
        }

        public void Generate(string pdfPath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Content().Column(col =>
                    {
                        // ---------- HEADER ----------
                        col.Item().Text("Отчёт по плану лучевой терапии")
                            .FontSize(18).Bold().AlignCenter();

                        col.Item().PaddingTop(10).Text(
                            "Клиника: ММЦ Белоостров, Онкоцентр,\n" +
                            "Отделение радиотерапии № 1");

                        col.Item().PaddingTop(10).Text(
                            $"Имя пациента: {_patientName}\n" +
                            $"ID пациента: {_patientId}\n" +
                            $"Дата отчёта: {DateTime.Now:dd.MM.yyyy}");

                        col.Item().LineHorizontal(1);

                        // ---------- PRESCRIPTION ----------
                        col.Item().Text(
                            $"Предписание: {_rx.Target} = {_rx.TotalDoseGy:F2} Гр");

                        // ---------- PTV TABLE ----------
                        col.Item().PaddingTop(10).Text("PTV – дозиметрические критерии")
                            .Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            HeaderCell("Параметр");
                            HeaderCell("Значение");
                            HeaderCell("Ед.");

                            Row("D2", _ptv.D2, "Гр");
                            Row("D5", _ptv.D5, "Гр");
                            Row("D50", _ptv.D50, "Гр");
                            Row("D95", _ptv.D95, "Гр");
                            Row("D98", _ptv.D98, "Гр");
                            Row("HI (ICRU)", _ptv.HI_ICRU, "");
                            Row("HI (D5/D95)", _ptv.HI_D5_D95, "");
                            Row("CI", _ptv.CI, "");
                            Row("GI", _ptv.GI, "");

                            void HeaderCell(string text) =>
                                table.Cell().Border(1).Padding(4)
                                    .Text(text).Bold();

                            void Row(string name, double value, string unit)
                            {
                                table.Cell().Border(1).Padding(4).Text(name);
                                table.Cell().Border(1).Padding(4)
                                    .Text(value.ToString("F3"));
                                table.Cell().Border(1).Padding(4).Text(unit);
                            }
                        });

                        // ---------- DVH ----------
                        col.Item().PaddingTop(10).Text("DVH").Bold();
                        col.Item().Image(_dvhImagePath);

                        // ---------- SIGNATURES ----------
                        col.Item().PaddingTop(20).LineHorizontal(1);

                        col.Item().PaddingTop(10).Text(
                            $"Выполнено кем: {_performedBy}\n" +
                            $"Проверено кем: {_checkedBy}\n" +
                            $"Радиотерапевт: {_radiationOncologist}\n" +
                            $"Заведующий отделения радиотерапии: " +
                            $"Гоголин Даниил Вячеславович\n" +
                            $"Дата: {DateTime.Now:dd.MM.yyyy}");
                    });
                });
            })
            .GeneratePdf(pdfPath);
        }
    }
}
