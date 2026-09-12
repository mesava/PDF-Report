using PDF_Report_Final;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportTestts
{
    public class MonacoLikeReport : IDocument
    {
        // ================= DATA =================
        private readonly Dictionary<string, PTVRxInfo> _ptvRx;
        private readonly Dictionary<string, (PTVMetrics Metrics, List<PassFailResult> Criteria)> _ptvs;
        private readonly string _mainPtv;
        private readonly List<BeamInfo> _beams;
        private readonly List<DVHResult> _oars;
        private readonly Dictionary<string, List<PassFailResult>> _oarCriteria;
        private readonly string _dvhImagePath;
        private readonly string _patientName;
        private readonly string _patientId;
        private readonly string _performedBy;
        private readonly string _checkedBy;
        private readonly string _radiationOncologist;
        private readonly DateTime _reportDate;

        // ================= CTOR =================
        public MonacoLikeReport(
            Dictionary<string, PTVRxInfo> ptvRx,
            Dictionary<string, (PTVMetrics Metrics, List<PassFailResult> Criteria)> ptvs,
            string mainPtv,
            List<BeamInfo> beams,
            List<DVHResult> oars,
            string dvhImagePath,
            string patientName,
            string patientId,
            string performedBy,
            string checkedBy,
            string radiationOncologist,
            Dictionary<string, List<PassFailResult>> oarCriteria)
        {
            _ptvRx = ptvRx;
            _ptvs = ptvs;
            _mainPtv = mainPtv;
            _beams = beams;
            _oars = oars;
            _dvhImagePath = dvhImagePath;
            _patientName = patientName;
            _patientId = patientId;
            _performedBy = performedBy;
            _checkedBy = checkedBy;
            _radiationOncologist = radiationOncologist;
            _reportDate = DateTime.Now;
            _oarCriteria = oarCriteria;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        // ================= COMPOSE =================
        public void Compose(IDocumentContainer container)
        {
            ComposeMainReport(container);
            ComposeSignaturesPage(container);
        }

        // =====================================================
        // MAIN REPORT (header only once as content)
        // =====================================================
        private void ComposeMainReport(IDocumentContainer container) {
            container.Page(page => {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Content().Column(col => {
                    col.Spacing(12);

                    // ---------- HEADER (ONLY FIRST PAGE) ----------
                    col.Item().Row(row => {
                        row.RelativeItem().Column(h => {
                            h.Spacing(4);
                            h.Item().Text("Отчёт по плану лучевой терапии")
                                .FontSize(14).Bold();
                            h.Item().Text("Система планирования: Monaco");
                            h.Item().Text($"Имя пациента: {_patientName}");
                            h.Item().Text($"ID пациента: {_patientId}");
                            h.Item().Text($"Дата отчёта: {_reportDate:dd.MM.yyyy}");

                            if (_ptvRx != null && _ptvRx.Count > 0) {
                                var rx = _ptvRx.Values.First();
                                h.Item().Text(
                                    "Предписание:\n" +
                                    $"СД (суммарная доза) – {rx.TotalDoseGy:F2} Гр / " +
                                    $"{rx.NumberOfFractions} фр / " +
                                    $"РД (разовая доза) - {rx.FractionDoseGy:F2} Гр"
                                );
                            } else {
                                h.Item().Text(t => {
                                    t.Span("Предписание:\n");
                                    t.Span(
                                        "СОД и фракционирование не определены в RTPLAN\n" +
                                        "или отсутствуют применимые PTV-критерии."
                                    )
                                    .Italic()
                                    .FontSize(9);
                                });
                            }
                        });

                        row.ConstantItem(200)
                            .AlignRight()
                            .AlignTop()
                            .Height(100)
                            .Image("logo.jpg", ImageScaling.FitArea);
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    // ---------- MAIN CONTENT ----------
                    ComposeBeams(col);
                    ComposePTV(col);
                    ComposeOAR(col);

                    col.Item()
                        .EnsureSpace(300)
                        .Column(c => ComposeDVH(c));
                });

                page.Footer().AlignRight().Text(t => {
                    t.Span("Стр. ");
                    t.CurrentPageNumber();
                    t.Span(" из ");
                    t.TotalPages();
                });
            });
        }

        // =====================================================
        // BEAMS
        // =====================================================
        private void ComposeBeams(ColumnDescriptor col) {
            Section(col, "Поля");

            col.Item().Table(t => {
                t.ColumnsDefinition(c => {
                    for (int i = 0; i < 7; i++)
                        c.RelativeColumn();
                });

                t.Header(h => {
                    HeaderCell(h.Cell(), "Поле");
                    HeaderCell(h.Cell(), "Тип");
                    HeaderCell(h.Cell(), "Энергия (МэВ)");
                    HeaderCell(h.Cell(), "Гантри (°)");
                    HeaderCell(h.Cell(), "Коллиматор (°)");
                    HeaderCell(h.Cell(), "Стол (°)");
                    HeaderCell(h.Cell(), "MU");
                });

                foreach (var b in _beams) {
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
        // PTV
        // =====================================================
        private void ComposePTV(ColumnDescriptor col) {
            Section(col, "PTV – дозиметрические критерии");

            col.Item()
                .PaddingBottom(4)
                .Text(
                    "(СД для каждого PTV отдельно в RTPLAN.dcm не задан, " +
                    "PTV определяется по наличию критерия D50%>=Rx Гр, " +
                    "критерии оценки применяются в случае его присутствия.)"
                )
                .Italic()
                .FontSize(8);

            foreach (var kv in _ptvs) {
                var metrics = kv.Value.Metrics;
                var criteria = kv.Value.Criteria;

                col.Item().PaddingTop(6)
                    .Text(kv.Key)
                    .Bold();

                col.Item().Table(t => {
                    t.ColumnsDefinition(c => {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                    });

                    t.Header(h => {
                        HeaderCell(h.Cell(), "Параметр");
                        HeaderCell(h.Cell(), "Критерий");
                        HeaderCell(h.Cell(), "Актуальное значение");
                        HeaderCell(h.Cell(), "Статус");
                    });

                    AddPTVRow(t, "D95", "D95% (Гр)", metrics.D95, criteria);
                    AddPTVRow(t, "D50", "D50% (Гр)", metrics.D50, criteria);
                    AddPTVRow(t, "D2", "D2% (Гр)", metrics.D2, criteria);
                    AddPTVRow(t, "HI_ICRU", "HI (ICRU)", metrics.HI_ICRU, criteria);
                    AddPTVRow(t, "HI_D5_D95", "HI (D5/D95)", metrics.HI_D5_D95, criteria);
                    AddPTVRow(t, "CI", "CI", metrics.CI, criteria);
                    AddPTVRow(t, "GI", "GI", metrics.GI, criteria);
                });
            }
        }

        private void AddPTVRow(
            TableDescriptor t,
            string metricKey,
            string label,
            double value,
            List<PassFailResult> rules)
        {
            var rule = rules.Find(r => r.Metric == metricKey);

            BodyCell(t.Cell(), label);
            BodyCell(t.Cell(), rule?.Criterion ?? "–");
            BodyCell(t.Cell(), value.ToString("F3"));

            t.Cell()
                .Border(0.5f)
                .Padding(3)
                .AlignCenter()
                .Text(tx => {
                    if (rule == null)
                        tx.Span("");
                    else if (rule.IsWarning)
                        tx.Span("⚠️").FontColor(Colors.Orange.Darken1);
                    else
                        tx.Span(rule.IsPass ? "✔️" : "✖️")
                            .FontColor(rule.IsPass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });
        }

        // =====================================================
        // OAR
        // =====================================================
        private void ComposeOAR(ColumnDescriptor col) {
            Section(col, "Органы риска");

            col.Item().Table(t => {
                t.ColumnsDefinition(c => {
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(4);
                    c.RelativeColumn(3);
                    c.RelativeColumn(1.5f);
                });

                t.Header(h => {
                    HeaderCell(h.Cell(), "Структура");
                    HeaderCell(h.Cell(), "Объём (см³)");
                    HeaderCell(h.Cell(), "Средняя доза (Гр)");
                    HeaderCell(h.Cell(), "Макс. доза (Гр)");
                    HeaderCell(h.Cell(), "Критерий");
                    HeaderCell(h.Cell(), "Актуальное значение");
                    HeaderCell(h.Cell(), "Статус");
                });

                foreach (var o in _oars) {
                    if (!_oarCriteria.TryGetValue(o.Structure, out var rules))
                        continue;

                    bool first = true;

                    foreach (var r in rules) {
                        if (first) {
                            BodyCell(t.Cell(), o.Structure);
                            BodyCell(t.Cell(), o.VolumeCm3.ToString("F1"));
                            BodyCell(t.Cell(), o.Mean.ToString("F2"));
                            BodyCell(t.Cell(), o.Max.ToString("F2"));
                            first = false;
                        } else {
                            BodyCell(t.Cell(), "");
                            BodyCell(t.Cell(), "");
                            BodyCell(t.Cell(), "");
                            BodyCell(t.Cell(), "");
                        }

                        BodyCell(t.Cell(), r.Criterion);
                        BodyCell(t.Cell(), $"{r.ActualValue:F2} {r.Unit}");

                        t.Cell()
                            .Border(0.5f)
                            .Padding(3)
                            .AlignCenter()
                            .Text(tx => {
                                tx.Span(r.IsPass ? "✔️" : "✖️")
                                    .FontColor(r.IsPass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            });
                    }
                }
            });
        }

        // =====================================================
        // DVH
        // =====================================================
        private void ComposeDVH(ColumnDescriptor col) {
            Section(col, "Гистограмма Доза-Объём (DVH)");

            if (!File.Exists(_dvhImagePath)) {
                col.Item().Text("DVH-график не найден.");
                return;
            }

            col.Item()
                .AlignCenter()
                .Image(_dvhImagePath, ImageScaling.FitWidth);
        }

        // =====================================================
        // SIGNATURES (LAST PAGE)
        // =====================================================
        private void ComposeSignaturesPage(IDocumentContainer container) {
            container.Page(page => {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row => {
                    row.RelativeItem().Column(h => {
                        h.Spacing(4);
                        h.Item().Text("Отчёт по плану лучевой терапии")
                            .FontSize(14).Bold();
                        h.Item().Text("Система планирования: Monaco");
                        h.Item().Text($"Имя пациента: {_patientName}");
                        h.Item().Text($"ID пациента: {_patientId}");
                        h.Item().Text($"Дата отчёта: {_reportDate:dd.MM.yyyy}");

                        if (_ptvRx != null && _ptvRx.Count > 0) {
                            var rx = _ptvRx.Values.First();
                            h.Item().Text(
                                "Предписание:\n" +
                                $"СД (суммарная доза) – {rx.TotalDoseGy:F2} Гр / " +
                                $"{rx.NumberOfFractions} фр / " +
                                $"РД (разовая доза) - {rx.FractionDoseGy:F2} Гр"
                            );
                        } else {
                            h.Item().Text(t => {
                                t.Span("Предписание:\n");
                                t.Span(
                                    "СОД и фракционирование не определены в RTPLAN\n" +
                                    "или отсутствуют применимые PTV-критерии."
                                )
                                .Italic()
                                .FontSize(9);
                            });
                        }
                    });

                    row.ConstantItem(200)
                        .AlignRight()
                        .AlignTop()
                        .Height(100)
                        .Image("logo.jpg", ImageScaling.FitArea);
                });

                page.Content()
                    .Column(col => {
                        // Комментарии в начале страницы
                        col.Item()
                            .PaddingTop(20)
                            .Column(commCol => {
                                commCol.Item()
                                    .PaddingBottom(8)
                                    .Text("Комментарии:")
                                    .Bold()
                                    .FontSize(11);

                                for (int i = 0; i < 10; i++) {
                                    commCol.Item()
                                        .PaddingVertical(8)
                                        .BorderBottom(1)
                                        .Height(25);
                                }
                            });

                        // Подписи внизу страницы
                        col.Item()
                            .ExtendVertical()
                            .AlignBottom()
                            .Column(sigCol => {
                                sigCol.Spacing(8);

                                SignatureLine(sigCol, "Выполнено", _performedBy);
                                SignatureLine(sigCol, "Проверено", _checkedBy);
                                SignatureLine(sigCol, "Радиотерапевт", _radiationOncologist);
                                SignatureLine(sigCol, "Заведующий отделением радиотерапии", "Гоголин Д. В.");

                                sigCol.Item().PaddingTop(10)
                                    .Text($"Дата: {_reportDate:dd.MM.yyyy}");
                            });
                    });

                page.Footer().AlignRight().Text(t => {
                    t.Span("Стр. ");
                    t.CurrentPageNumber();
                    t.Span(" из ");
                    t.TotalPages();
                });
            });
        }

        private void SignatureLine(ColumnDescriptor col, string title, string name) {
            col.Item().PaddingVertical(4).Row(row => {
                row.RelativeItem()
                    .AlignLeft()
                    .Text($"{title}: {name}");

                row.ConstantItem(180)
                    .BorderBottom(1);
            });
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private void Section(ColumnDescriptor col, string title) {
            col.Item().PaddingTop(10)
                .Text(title).FontSize(11).Bold();
        }

        private void HeaderCell(IContainer cell, string text) {
            cell.Background(Colors.Grey.Lighten2)
                .Border(0.8f)
                .Padding(4)
                .AlignCenter()
                .Text(text).Bold();
        }

        private void BodyCell(IContainer cell, string text) {
            cell.Border(0.5f)
                .Padding(3)
                .AlignCenter()
                .Text(text);
        }
    }
}
