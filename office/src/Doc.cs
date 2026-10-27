// ---------------------------------------------------------------------------------
//  Devkron.Ext.Office - extracción de texto de PDF, DOCX y XLSX para Devkron 2
//  (PdfPig, DocumentFormat.OpenXml, ClosedXML). Basado en induxsoft.txtract.
//  Copyright 2019-2026 Induxsoft Data Services S de RL de CV
//  Licencia: Apache 2.0 (ver LICENSE en la raíz del repositorio)
// ---------------------------------------------------------------------------------

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;

namespace Devkron.Ext.Office
{
    public static class Doc
    {
        /// <summary>doc.text(archivo): texto de PDF, DOCX o XLSX según la extensión (PDF sin OCR).</summary>
        public static string Text(string file)
        {
            Check(file);
            switch (Path.GetExtension(file).ToLowerInvariant())
            {
                case ".pdf": return string.Join("\n", PdfPages(file));
                case ".docx": return Docx(file);
                case ".xlsx":
                case ".xlsm": return XlsxText(file);
                default:
                    throw new Exception("doc: extensión no admitida en '" + Path.GetFileName(file) + "' (se admiten .pdf, .docx, .xlsx).");
            }
        }

        /// <summary>doc.pages(archivo): lista con el texto de cada página (solo PDF).</summary>
        public static List<object> Pages(string file)
        {
            Check(file);
            if (Path.GetExtension(file).ToLowerInvariant() != ".pdf")
                throw new Exception("doc: doc.pages solo admite archivos .pdf ('" + Path.GetFileName(file) + "').");
            return PdfPages(file).Cast<object>().ToList();
        }

        private static void Check(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("doc: falta la ruta del archivo.");
            if (!File.Exists(file))
                throw new Exception("doc: no existe el archivo '" + file + "'.");
        }

        private static List<string> PdfPages(string file)
        {
            var l = new List<string>();
            try
            {
                using (var pdf = PdfDocument.Open(file))
                    foreach (var page in pdf.GetPages())
                        l.Add(PageText(page));
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                throw new Exception("doc: no se pudo leer el PDF '" + Path.GetFileName(file) + "': " + ex.Message, ex);
            }
            return l;
        }

        // Une las palabras por línea (page.Text del original pega las palabras sin espacios en muchos PDF)
        private static string PageText(UglyToad.PdfPig.Content.Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) return page.Text ?? "";
            var sb = new StringBuilder();
            double lastY = double.NaN;
            foreach (var w in words.OrderByDescending(w => Math.Round(w.BoundingBox.Bottom, 0)).ThenBy(w => w.BoundingBox.Left))
            {
                double y = Math.Round(w.BoundingBox.Bottom, 0);
                if (!double.IsNaN(lastY))
                    sb.Append(Math.Abs(y - lastY) > 1 ? "\n" : " ");
                sb.Append(w.Text);
                lastY = y;
            }
            return sb.ToString();
        }

        private static string Docx(string file)
        {
            try
            {
                using (var doc = WordprocessingDocument.Open(file, false))
                {
                    var body = doc.MainDocumentPart?.Document?.Body;
                    if (body == null) return "";
                    // Un párrafo por línea (el original usaba body.InnerText y juntaba todo el texto)
                    var sb = new StringBuilder();
                    foreach (var p in body.Descendants<W.Paragraph>())
                    {
                        var line = new StringBuilder();
                        foreach (var el in p.Descendants())
                        {
                            if (el is W.Text t) line.Append(t.Text);
                            else if (el is W.TabChar) line.Append('\t');
                            else if (el is W.Break || el is W.CarriageReturn) line.Append('\n');
                        }
                        sb.Append(line).Append('\n');
                    }
                    return sb.ToString().TrimEnd('\n');
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                throw new Exception("doc: no se pudo leer el DOCX '" + Path.GetFileName(file) + "': " + ex.Message, ex);
            }
        }

        // Mismo formato que induxsoft.txtract: "--- Hoja: X ---" y celdas separadas por tabulador
        private static string XlsxText(string file)
        {
            var sb = new StringBuilder();
            using (var wb = Xlsx.Open(file))
            {
                foreach (var ws in wb.Worksheets)
                {
                    sb.Append("--- Hoja: ").Append(ws.Name).Append(" ---\n");
                    foreach (var row in ws.RowsUsed())
                    {
                        foreach (var cell in row.Cells())
                        {
                            XLCellValue v;
                            try { v = cell.Value; } catch (Exception) { v = cell.CachedValue; }
                            sb.Append(Xlsx.Text(v)).Append('\t');
                        }
                        sb.Append('\n');
                    }
                }
            }
            return sb.ToString();
        }
    }
}
