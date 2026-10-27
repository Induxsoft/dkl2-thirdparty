// ---------------------------------------------------------------------------------
//  Devkron.Ext.Office - hojas de cálculo XLSX para Devkron 2 (usa ClosedXML)
//  Copyright 2019-2026 Induxsoft Data Services S de RL de CV
//  Licencia: Apache 2.0 (ver LICENSE en la raíz del repositorio)
// ---------------------------------------------------------------------------------

using ClosedXML.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Devkron.Ext.Office
{
    public static class Xlsx
    {
        /// <summary>xlsx.write(archivo, lista): una hoja ("Hoja1") con encabezados.</summary>
        public static void Write(string file, object list)
        {
            CheckPath(file);
            using (var wb = new XLWorkbook())
            {
                FillSheet(wb, "Hoja1", list);
                Save(wb, file);
            }
        }

        /// <summary>xlsx.write_sheets(archivo, hojas): registro {nombre_hoja: lista, ...}.</summary>
        public static void WriteSheets(string file, object sheets)
        {
            CheckPath(file);
            var d = sheets as IDictionary<string, object>;
            if (d == null)
                throw new Exception("xlsx: se esperaba un registro {hoja: lista, ...}.");
            if (d.Count == 0)
                throw new Exception("xlsx: el registro de hojas está vacío.");
            using (var wb = new XLWorkbook())
            {
                foreach (var kv in d)
                    FillSheet(wb, SheetName(kv.Key), kv.Value);
                Save(wb, file);
            }
        }

        /// <summary>xlsx.to_bytes(lista): el libro en memoria, para httpres.binary.</summary>
        public static byte[] ToBytes(object list)
        {
            using (var wb = new XLWorkbook())
            using (var ms = new MemoryStream())
            {
                FillSheet(wb, "Hoja1", list);
                wb.SaveAs(ms);
                return ms.ToArray();
            }
        }

        /// <summary>xlsx.sheets(archivo): nombres de las hojas.</summary>
        public static List<object> Sheets(string file)
        {
            var l = new List<object>();
            using (var wb = Open(file))
                foreach (var ws in wb.Worksheets)
                    l.Add(ws.Name);
            return l;
        }

        /// <summary>xlsx.read(archivo, hoja): lista de registros; la primera fila son los nombres. hoja "" = la primera.</summary>
        public static List<object> Read(string file, string sheet)
        {
            var result = new List<object>();
            using (var wb = Open(file))
            {
                IXLWorksheet ws;
                if (string.IsNullOrWhiteSpace(sheet))
                    ws = wb.Worksheet(1);
                else if (!wb.TryGetWorksheet(sheet, out ws))
                    throw new Exception("xlsx: el libro no tiene la hoja '" + sheet + "'.");

                var used = ws.RangeUsed();
                if (used == null) return result;

                // Fórmulas sin valor guardado (libros generados por otros programas): se recalculan
                foreach (var fc in used.CellsUsed(c => c.HasFormula))
                {
                    if (fc.CachedValue.IsBlank)
                    {
                        try { wb.RecalculateAllFormulas(); } catch (Exception) { }
                        break;
                    }
                }

                int r0 = used.FirstRow().RowNumber(), r1 = used.LastRow().RowNumber();
                int c0 = used.FirstColumn().ColumnNumber(), c1 = used.LastColumn().ColumnNumber();

                // Encabezados: vacío → columna_N; repetido → nombre_2, nombre_3...
                var names = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int c = c0; c <= c1; c++)
                {
                    string n = Text(ws.Cell(r0, c).Value).Trim();
                    if (n == "") n = "columna_" + (c - c0 + 1).ToString(CultureInfo.InvariantCulture);
                    string b = n; int k = 2;
                    while (seen.Contains(n)) n = b + "_" + (k++).ToString(CultureInfo.InvariantCulture);
                    seen.Add(n);
                    names.Add(n);
                }

                for (int r = r0 + 1; r <= r1; r++)
                {
                    var row = ws.Row(r);
                    if (row.IsEmpty()) continue;
                    var rec = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (int c = c0; c <= c1; c++)
                        rec[names[c - c0]] = ToDkl(ws.Cell(r, c));
                    result.Add(rec);
                }
            }
            return result;
        }

        // ------------------------------------------------------------------ interno

        internal static XLWorkbook Open(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("xlsx: falta la ruta del archivo.");
            if (!File.Exists(file))
                throw new Exception("xlsx: no existe el archivo '" + file + "'.");
            try { return new XLWorkbook(file); }
            catch (Exception ex) { throw new Exception("xlsx: no se pudo abrir '" + file + "' como libro XLSX: " + ex.Message, ex); }
        }

        private static void CheckPath(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("xlsx: falta la ruta del archivo de destino.");
        }

        private static void Save(XLWorkbook wb, string file)
        {
            try { wb.SaveAs(file); }
            catch (Exception ex) { throw new Exception("xlsx: no se pudo guardar '" + file + "': " + ex.Message, ex); }
        }

        private static string SheetName(string n)
        {
            n = (n ?? "").Trim();
            if (n == "") throw new Exception("xlsx: el nombre de la hoja no puede estar vacío.");
            if (n.Length > 31) throw new Exception("xlsx: el nombre de la hoja '" + n + "' pasa de 31 caracteres.");
            if (n.IndexOfAny(new[] { ':', '\\', '/', '?', '*', '[', ']' }) >= 0)
                throw new Exception("xlsx: el nombre de la hoja '" + n + "' tiene caracteres no permitidos (: \\ / ? * [ ]).");
            return n;
        }

        private static void FillSheet(XLWorkbook wb, string name, object data)
        {
            if (data == null)
                throw new Exception("xlsx: la lista de la hoja '" + name + "' es nula.");
            var list = data as IList;
            if (list == null)
                throw new Exception("xlsx: la hoja '" + name + "' debe recibir una lista de registros.");

            var cols = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object o in list)
            {
                var d = o as IDictionary<string, object>;
                if (d == null)
                    throw new Exception("xlsx: cada elemento de la hoja '" + name + "' debe ser un registro.");
                foreach (string k in d.Keys)
                    if (seen.Add(k)) cols.Add(k);
            }

            var ws = wb.Worksheets.Add(name);
            for (int c = 0; c < cols.Count; c++)
            {
                ws.Cell(1, c + 1).Value = cols[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }
            int r = 2;
            foreach (object o in list)
            {
                var d = (IDictionary<string, object>)o;
                for (int c = 0; c < cols.Count; c++)
                {
                    object v;
                    if (d.TryGetValue(cols[c], out v))
                        SetCell(ws.Cell(r, c + 1), v);
                }
                r++;
            }
            if (cols.Count > 0)
                ws.SheetView.FreezeRows(1);
        }

        private static void SetCell(IXLCell cell, object v)
        {
            switch (v)
            {
                case null: return;
                case string s: cell.Value = s; return;
                case decimal m: cell.Value = (double)m; return;
                case double d: cell.Value = d; return;
                case float f: cell.Value = (double)f; return;
                case int i: cell.Value = i; return;
                case long l: cell.Value = (double)l; return;
                case bool b: cell.Value = b; return;
                case DateTime dt: cell.Value = dt; return;
                case IFormattable fo: cell.Value = fo.ToString(null, CultureInfo.InvariantCulture); return;
                default: cell.Value = v.ToString(); return;
            }
        }

        /// <summary>Valor de celda a DKL: número → decimal; fecha → AAAAMMDD.HHMMSS; lógico → 1/0; error → "#N/A"...</summary>
        private static object ToDkl(IXLCell cell)
        {
            XLCellValue v;
            try { v = cell.Value; } catch (Exception) { v = cell.CachedValue; }
            switch (v.Type)
            {
                case XLDataType.Blank: return "";
                case XLDataType.Number:
                    double d = v.GetNumber();
                    if (double.IsNaN(d) || double.IsInfinity(d)) return "";
                    if (Math.Abs(d) >= 7.9e27) return d.ToString("R", CultureInfo.InvariantCulture); // fuera del rango de decimal
                    return Round((decimal)d);
                case XLDataType.Boolean: return v.GetBoolean() ? 1m : 0m;
                case XLDataType.DateTime: return DklDate(v.GetDateTime());
                case XLDataType.TimeSpan:
                    var ts = v.GetTimeSpan();
                    return decimal.Parse(ts.Hours.ToString("00") + ts.Minutes.ToString("00") + ts.Seconds.ToString("00"), CultureInfo.InvariantCulture) / 1000000m;
                default: return Text(v);
            }
        }

        // Quita el ruido binario de double (0.1 + 0.2 → 0.3) conservando 15 dígitos significativos, como Excel
        private static decimal Round(decimal m)
        {
            double d = (double)m;
            string s = d.ToString("G15", CultureInfo.InvariantCulture);
            decimal r;
            if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out r)) return r;
            return m;
        }

        internal static decimal DklDate(DateTime dt)
        {
            return decimal.Parse(dt.ToString("yyyyMMdd.HHmmss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        internal static string Text(XLCellValue v)
        {
            switch (v.Type)
            {
                case XLDataType.Blank: return "";
                case XLDataType.Number:
                    double n = v.GetNumber();
                    if (double.IsNaN(n) || double.IsInfinity(n) || Math.Abs(n) >= 7.9e27) return n.ToString("R", CultureInfo.InvariantCulture);
                    return Round((decimal)n).ToString(CultureInfo.InvariantCulture);
                case XLDataType.Boolean: return v.GetBoolean() ? "TRUE" : "FALSE";
                case XLDataType.DateTime: return v.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                case XLDataType.TimeSpan: return v.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture);
                case XLDataType.Error: return v.GetError().ToString();
                default: return v.GetText();
            }
        }
    }
}
