using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PDF_Report_Final;

namespace ReportTestts
{
    public class MonacoLikeReport : IDocument
    {
        // ================= DATA =================
        private readonly PrescriptionInfo _rx;
        private readonly PTVMetrics _ptv;
        private readonly List<PassFailResult> _ptvCriteria;
        private readonly List<BeamInfo> _beams;
        private readonly List<DVHResult> _oars;
        private readonly string _dvhImagePath;

        private readonly string _patientName;
        private readonly string _patientId;
        private readonly string _performedBy;
        private readonly string _checkedBy;
        private readonly string _radiationOncologist;
        private readonly DateTime _reportDate;

        // ================= CTOR =================
        public MonacoLikeReport(
            PrescriptionInfo rx,
            PTVMetrics ptv,
            List<PassFailResult> ptvCriteria,
            List<BeamInfo> beams,
            List<DVHResult> oars,
            string dvhImagePath,
            string patientName,
            string patientId,
            string performedBy,
            string checkedBy,
            string radiationOncologist)
        {
            _rx = rx;
            _ptv = ptv;
            _ptvCriteria = ptvCriteria;
            _beams = beams;
            _oars = oars;
            _dvhImagePath = dvhImagePath;

            _patientName = patientName;
            _patientId = patientId;
            _performedBy = performedBy;
            _checkedBy = checkedBy;
            _radiationOncologist = radiationOncologist;
            _reportDate = DateTime.Now;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        // ================= COMPOSE =================
        public void Compose(IDocumentContainer container)
        {
            ComposeMainReport(container);
            ComposeSignaturesPage(container);
        }

        // =====================================================
        // MAIN REPORT
        // =====================================================
        private void ComposeMainReport(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    ComposeHeader(col);
                    ComposeBeams(col);
                    ComposePTV(col);
                    ComposeOAR(col);

                    col.Item()
                        .EnsureSpace(300)
                        .Column(c => ComposeDVH(c));
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Стр. ");
                    t.CurrentPageNumber();
                    t.Span(" из ");
                    t.TotalPages();
                });
            });
        }

        // =====================================================
        // HEADER (with logo on the right, first page only)
        // =====================================================
        private void ComposeHeader(ColumnDescriptor col)
        {
            col.Item().Row(row =>
            {
                // ===== LEFT: TEXT =====
                row.RelativeItem().Column(h =>
                {
                    h.Spacing(4);

                    h.Item().Text("Отчёт по плану лучевой терапии")
                        .FontSize(14).Bold();

                    h.Item().Text("Система планирования: Monaco");
                    h.Item().Text("Клиника: ММЦ Белоостров, Онкоцентр, Отделение радиотерапии № 1");
                    h.Item().Text($"Имя пациента: {_patientName}");
                    h.Item().Text($"ID пациента: {_patientId}");
                    h.Item().Text($"Дата отчёта: {_reportDate:dd.MM.yyyy}");
                    h.Item().Text(
                        $"Предписание: {_rx.Target} — " +
                        $"СОД {_rx.TotalDoseGy:F1} Гр / " +
                        $"{_rx.NumberOfFractions} фр " +
                        $"(РОД {_rx.FractionDoseGy:F1} Гр)"
                    );
                });

                // ===== RIGHT: LOGO =====
                row.ConstantItem(160)
                    .AlignRight()
                    .AlignTop()
                    .Height(45)
                    .Image("logo_beloostrov.jpg", ImageScaling.FitArea);
            });

            col.Item().PaddingVertical(8).LineHorizontal(1);
        }


        // =====================================================
        // BEAMS
        // =====================================================
        private void ComposeBeams(ColumnDescriptor col)
        {
            Section(col, "Поля");

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    for (int i = 0; i < 7; i++)
                        c.RelativeColumn();
                });

                t.Header(h =>
                {
                    HeaderCell(h.Cell(), "Поле");
                    HeaderCell(h.Cell(), "Тип");
                    HeaderCell(h.Cell(), "Энергия (МэВ)");
                    HeaderCell(h.Cell(), "Гантри (°)");
                    HeaderCell(h.Cell(), "Коллиматор (°)");
                    HeaderCell(h.Cell(), "Стол (°)");
                    HeaderCell(h.Cell(), "MU");
                });

                foreach (var b in _beams)
                {
                    BodyCell(t.Cell(), b.Number.ToString());
                    BodyCell(t.Cell(), b.Type);
                    BodyCell(t.Cell(), b.Energy.ToString("F1"));
                    BodyCell(t.Cell(), b.Gantry);
                    BodyCell(t.Cell(), b.Collimator.ToString("F1"));
                    BodyCell(t.Cell(), b.Couch.ToString("F1"));
                    BodyCell(t.Cell(), b.MU.ToString("F1"));
                }
            });
        }

        // =====================================================
        // PTV (ICRU FIXED)
        // =====================================================
        private void ComposePTV(ColumnDescriptor col)
        {
            Section(col, "PTV – дозиметрические критерии");

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3); // Параметр
                    c.RelativeColumn(2); // Значение
                    c.RelativeColumn(1); // Статус
                    c.RelativeColumn(3); // Критерий
                });

                t.Header(h =>
                {
                    HeaderCell(h.Cell(), "Параметр");
                    HeaderCell(h.Cell(), "Значение");
                    HeaderCell(h.Cell(), "Статус");
                    HeaderCell(h.Cell(), "Критерий");
                });

                AddPTVRow(t, "D2", "D2% (Гр)", _ptv.D2);
                AddPTVRow(t, "D50", "D50% (Гр)", _ptv.D50);
                AddPTVRow(t, "D95", "D95% (Гр)", _ptv.D95);
                AddPTVRow(t, "HI_ICRU", "HI (ICRU)", _ptv.HI_ICRU);
                AddPTVRow(t, "HI_D5_D95", "HI (D5/D95)", _ptv.HI_D5_D95);
                AddPTVRow(t, "CI", "CI", _ptv.CI);
                AddPTVRow(t, "GI", "GI", _ptv.GI);
            });
        }

        private void AddPTVRow(
      TableDescriptor t,
      string key,
      string label,
      double value)
        {
            var rule = _ptvCriteria.Find(r => r.Metric == key);
            bool pass = rule?.IsPass ?? true;

            // Параметр
            BodyCell(t.Cell(), label);

            // Значение
            BodyCell(t.Cell(), value.ToString("F3"));

            // Статус (✔ / ✖) — С РАМКОЙ
            t.Cell()
                .Border(0.5f)
                .Padding(3)
                .AlignCenter()
                .Text(tx =>
                {
                    tx.Span(pass ? "✔" : "✖")
                      .FontColor(pass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });

            // Критерий (ICRU / RTOG)
            BodyCell(
                t.Cell(),
                rule != null
                    ? $"{rule.Criterion} ({rule.Source})"
                    : "-"
            );
        }

        // =====================================================
        // OAR
        // =====================================================
        private void ComposeOAR(ColumnDescriptor col)
        {
            Section(col, "Органы риска");

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });

                t.Header(h =>
                {
                    HeaderCell(h.Cell(), "Структура");
                    HeaderCell(h.Cell(), "Объём (см³)");
                    HeaderCell(h.Cell(), "Средняя доза (Гр)");
                    HeaderCell(h.Cell(), "Макс. доза (Гр)");
                });

                foreach (var o in _oars)
                {
                    BodyCell(t.Cell(), o.Structure);
                    BodyCell(t.Cell(), o.VolumeCm3.ToString("F1"));
                    BodyCell(t.Cell(), o.Mean.ToString("F2"));
                    BodyCell(t.Cell(), o.Max.ToString("F2"));
                }
            });
        }

        // =====================================================
        // DVH
        // =====================================================
        private void ComposeDVH(ColumnDescriptor col)
        {
            Section(col, "Кумулятивная DVH");

            if (!File.Exists(_dvhImagePath))
            {
                col.Item().Text("DVH-график не найден.");
                return;
            }

            col.Item().AlignCenter()
                .Image(_dvhImagePath, ImageScaling.FitWidth);
        }

        // =====================================================
        // SIGNATURE PAGE — CLEAN & STABLE (TABLE-BASED)
        // =====================================================
        private void ComposeSignaturesPage(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content()
                    .ExtendVertical()      // ⬅ занимает ВСЮ высоту страницы
                    .AlignBottom()         // ⬅ якорь к НИЗУ страницы
                    .Column(col =>
                    {
                        col.Spacing(8);

                        SignatureLine(col, "Выполнено кем", _performedBy);
                        SignatureLine(col, "Проверено кем", _checkedBy);
                        SignatureLine(col, "Радиотерапевт", _radiationOncologist);
                        SignatureLine(
                            col,
                            "Заведующий отделения радиотерапии",
                            "Гоголин Дани" +
                            "л Вячеславович");

                        col.Item().PaddingTop(10)
                            .Text($"Дата: {_reportDate:dd.MM.yyyy}");
                    });

                // номер страницы — ОДИН раз, можно оставить
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Стр. ");
                    t.CurrentPageNumber();
                    t.Span(" из ");
                    t.TotalPages();
                });
            });
        }


        // =====================================================
        // SIGNATURE LINE
        // =====================================================
        private void SignatureLine(ColumnDescriptor col, string title, string name)
        {
            col.Item().PaddingVertical(4).Row(row =>
            {
                // Левая часть: "Выполнено кем: ФИО"
                row.RelativeItem()
                    .AlignLeft()
                    .Text($"{title}: {name}")
                    .FontSize(10);

                // Правая часть: линия подписи у правого края
                row.ConstantItem(180)
                    .PaddingBottom(2)
                    .BorderBottom(1);
            });
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private void Section(ColumnDescriptor col, string title)
        {
            col.Item().PaddingTop(10)
                .Text(title).FontSize(11).Bold();
        }

        private void HeaderCell(IContainer cell, string text)
        {
            cell.Background(Colors.Grey.Lighten2)
                .Border(0.8f)
                .Padding(4)
                .AlignCenter()
                .Text(text).Bold();
        }

        private void BodyCell(IContainer cell, string text)
        {
            cell.Border(0.5f)
                .Padding(3)
                .Text(text);
        }
    }
}
