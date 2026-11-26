using Barcoder;
using Barcoder.Code128;
using Barcoder.Renderers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Barcoder;
using Barcoder.Code128;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
namespace Kenan.Receipt
{
    public static class ReceiptMaker
    {
        private static bool _fontsReady;
        private static readonly object _fontLock = new object();

        /*
         * Brand info
        */
        public static string BrandName { get; set; }
        //public static string BrandLogo { get; set; } // add this later
        public static string BrandEmail { get; set; }
        public static string BrandWebSite { get; set; }
        /*
         * Invoice / Receipt info
        */
        public static string InvoiceNumber { get; set; }
        public static string Date { get; set; }
        public static string Currency { get; set; } // Make this ISO 4217 format later
        public static float TaxRate { get; set; }
        /*
         * Customer info
        */
        public static string CustomerName { get; set; }
        public static List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
        public static string Notes { get; set; } = "Thank you for your purchase.";

        private static void EnsureFonts()
        {
            lock (_fontLock)
            {
                if (_fontsReady) return;
                GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                _fontsReady = true;
            }
        }
        public static byte[] GetReceipt()
        {
            EnsureFonts();
            var document = new Document();
            BuildDocument(document);

            var pdfRenderer = new PdfDocumentRenderer();
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            if (!string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                DrawBarcode(pdfRenderer.PdfDocument);
            }

            using (var stream = new MemoryStream())
            {
                pdfRenderer.PdfDocument.Save(stream, false);
                return stream.ToArray();
            }
        }
        private static void BuildDocument(Document document)
        {
            var section = document.AddSection();
            var ps = section.PageSetup;
            ps.PageWidth = Unit.FromMillimeter(90);
            ps.PageHeight = Unit.FromMillimeter(150);
            ps.LeftMargin = ps.RightMargin = Unit.FromMillimeter(6);
            ps.TopMargin = ps.BottomMargin = Unit.FromMillimeter(6);

            // Green header with brand name and amount
            var header = section.AddTable();
            header.AddColumn(Unit.FromMillimeter(78));

            var hRow = header.AddRow();
            hRow.Shading.Color = Color.FromRgb(15, 118, 110);
            hRow.Format.Alignment = ParagraphAlignment.Center;
            hRow.Format.Font.Color = Colors.White;
            hRow.TopPadding = Unit.FromMillimeter(4);
            hRow.BottomPadding = Unit.FromMillimeter(4);
            hRow.Cells[0].AddParagraph($"{BrandName}").Format.Font.Bold = true;

            decimal totalAmount = 0;
            if (Items.Count > 0)
            {
                foreach (var item in Items)
                {
                    var lineTotal = item.Amount * item.Price;
                    totalAmount += lineTotal;
                }
            }

            var amount = hRow.Cells[0].AddParagraph($"${totalAmount.ToString("F2")}");
            amount.Format.Font.Size = 20;
            amount.Format.Font.Bold = true;
            //add here a status line for paid/unpaid, etc. later
            section.AddParagraph().Format.SpaceAfter = Unit.FromMillimeter(5);

            // info table with receipt number, date, and customer name
            if (!string.IsNullOrWhiteSpace(InvoiceNumber) ||
                !string.IsNullOrWhiteSpace(Date) ||
                !string.IsNullOrWhiteSpace(CustomerName))
            {
                var info = section.AddTable();
                info.AddColumn(Unit.FromMillimeter(39));
                info.AddColumn(Unit.FromMillimeter(39));
                info.TopPadding = Unit.FromMillimeter(1.2);
                info.BottomPadding = Unit.FromMillimeter(1.2);

                Row lastRow = null;
                if (!string.IsNullOrWhiteSpace(InvoiceNumber))
                {

                    lastRow = AddLine(info, "Receipt ", $"{InvoiceNumber}");
                    lastRow.Cells[1].Format.Font.Bold = true;
                }
                if (!string.IsNullOrWhiteSpace(Date))
                {
                    lastRow = AddLine(info, "Date ", $"{Date}");
                }
                if (!string.IsNullOrWhiteSpace(CustomerName))
                {
                    lastRow = AddLine(info, "Customer ", $"{CustomerName}");
                }

                // dotted line under the last visible row
                lastRow.Borders.Bottom.Style = BorderStyle.Dot;
                lastRow.Borders.Bottom.Width = 0.5;
                lastRow.BottomPadding = Unit.FromMillimeter(2);
            }
            // items table with purchased items and their prices
            var items = section.AddTable();
            items.AddColumn(Unit.FromMillimeter(39));
            items.AddColumn(Unit.FromMillimeter(39));
            items.TopPadding = Unit.FromMillimeter(2);
            items.BottomPadding = Unit.FromMillimeter(1.5);

            foreach (var item in Items)
            {
                var lineTotal = item.Amount * item.Price;
                AddLine(items, $"{item.Name} x {item.Amount}", "$" + lineTotal.ToString("F2"));
            }

            // total line with total amount paid
            var total = AddLine(items, "Total paid", $"${totalAmount.ToString("F2")}", true);
            total.Cells[1].Format.Font.Color = Color.FromRgb(15, 118, 110);
            total.Borders.Top.Style = BorderStyle.Dot;
            total.Borders.Top.Width = 0.5;
            total.TopPadding = Unit.FromMillimeter(2);

            var thanks = section.AddParagraph($"{Notes}");
            thanks.Format.Alignment = ParagraphAlignment.Center;
            thanks.Format.Font.Size = 7;
            thanks.Format.Font.Color = Color.FromRgb(120, 120, 120);
            thanks.Format.SpaceBefore = Unit.FromMillimeter(8);
        }

        private static Row AddLine(Table table, string left, string right, bool bold = false)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(left);
            row.Cells[1].AddParagraph(right).Format.Alignment = ParagraphAlignment.Right;
            row.Format.Font.Bold = bold;
            return row;
        }
        private static void DrawBarcode(PdfDocument pdf)
        {
            IBarcode barcode;
            try
            {
                barcode = Code128Encoder.Encode(InvoiceNumber);
            }
            catch (Exception)
            {
                return;
            }

            int modules = barcode.Bounds.X;
            var page = pdf.Pages[pdf.PageCount - 1];

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                double totalWidth = XUnit.FromMillimeter(60).Point;
                double barHeight = XUnit.FromMillimeter(12).Point;
                double moduleWidth = totalWidth / modules;

                double left = (page.Width.Point - totalWidth) / 2;
                double top = page.Height.Point - XUnit.FromMillimeter(10).Point - barHeight;

                int x = 0;
                while (x < modules)
                {
                    if (barcode.At(x, 0))
                    {
                        int start = x;
                        while (x < modules && barcode.At(x, 0)) x++;
                        gfx.DrawRectangle(XBrushes.Black,
                            left + start * moduleWidth, top,
                            (x - start) * moduleWidth, barHeight);
                    }
                    else x++;
                }
            }
        }
    }
}
