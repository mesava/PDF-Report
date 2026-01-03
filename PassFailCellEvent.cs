using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ReportTestts
{
    public class PassFailCellEvent : IPdfPCellEvent
    {
        private readonly bool _pass;

        public PassFailCellEvent(bool pass)
        {
            _pass = pass;
        }

        public void CellLayout(
            PdfPCell cell,
            Rectangle rect,
            PdfContentByte[] canvases)
        {
            PdfContentByte cb = canvases[PdfPTable.TEXTCANVAS];

            float cx = (rect.Left + rect.Right) / 2;
            float cy = (rect.Top + rect.Bottom) / 2;

            if (_pass)
            {
                // green check mark
                cb.SetColorStroke(BaseColor.GREEN);
                cb.SetLineWidth(1.5f);
                cb.MoveTo(cx - 4, cy);
                cb.LineTo(cx - 1, cy - 3);
                cb.LineTo(cx + 4, cy + 4);
                cb.Stroke();
            }
            else
            {
                // red circle
                cb.SetColorStroke(BaseColor.RED);
                cb.SetLineWidth(1.5f);
                cb.Circle(cx, cy, 4);
                cb.Stroke();
            }
        }
    }
}


namespace PDF_Report_Final
{
    internal class PassFailCellEvent
    {
    }
}
