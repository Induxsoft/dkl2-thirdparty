// ---------------------------------------------------------------------------------
//  Devkron.Ext.Csv - lectura y escritura de CSV para Devkron 2 (usa CsvHelper)
//  Copyright 2019-2026 Induxsoft Data Services S de RL de CV
//  Licencia: Apache 2.0 (ver LICENSE en la raíz del repositorio)
//  Basado en devkron.serialize.csv (csvHelper.cs) de dkl-linux-fixes.
// ---------------------------------------------------------------------------------

using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Devkron.Ext.Csv
{
    public static class Csv
    {
        // ------------------------------------------------------------------ lectura

        /// <summary>csv.read(archivo, columnas_numericas): separador detectado (, ; tabulador).</summary>
        public static List<object> Read(string file, string numcols)
        {
            string text = ReadFile(file);
            return Parse(text, numcols, null);
        }

        /// <summary>csv.read_sep(archivo, columnas_numericas, separador).</summary>
        public static List<object> ReadSep(string file, string numcols, string separator)
        {
            string text = ReadFile(file);
            return Parse(text, numcols, NormalizeSeparator(separator));
        }

        /// <summary>csv.from_text(texto, columnas_numericas).</summary>
        public static List<object> FromText(string text, string numcols)
        {
            return Parse(text ?? "", numcols, null);
        }

        /// <summary>csv.from_text_sep(texto, columnas_numericas, separador).</summary>
        public static List<object> FromTextSep(string text, string numcols, string separator)
        {
            return Parse(text ?? "", numcols, NormalizeSeparator(separator));
        }

        /// <summary>csv.detect(texto): separador que usaría csv.read (",", ";" o "\t").</summary>
        public static string Detect(string text)
        {
            string t = text ?? "";
            string sep = SepLine(ref t);
            return sep ?? DetectSeparator(t);
        }

        // ------------------------------------------------------------------ escritura

        /// <summary>csv.write(archivo, lista): UTF-8 con BOM, separador coma.</summary>
        public static void Write(string file, object data)
        {
            WriteSep(file, data, ",");
        }

        /// <summary>csv.write_sep(archivo, lista, separador).</summary>
        public static void WriteSep(string file, object data, string separator)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("csv: falta la ruta del archivo de destino.");
            string text = Build(data, NormalizeSeparator(separator));
            File.WriteAllText(file, text, new UTF8Encoding(true));
        }

        /// <summary>csv.to_text(lista): cadena CSV con coma.</summary>
        public static string ToText(object data)
        {
            return Build(data, ",");
        }

        /// <summary>csv.to_text_sep(lista, separador).</summary>
        public static string ToTextSep(object data, string separator)
        {
            return Build(data, NormalizeSeparator(separator));
        }

        // ------------------------------------------------------------------ interno

        private static string ReadFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("csv: falta la ruta del archivo.");
            if (!File.Exists(file))
                throw new Exception("csv: no existe el archivo '" + file + "'.");
            // UTF-8 por omisión; respeta la marca BOM (UTF-8/UTF-16) si viene
            using (var sr = new StreamReader(file, Encoding.UTF8, true))
                return sr.ReadToEnd();
        }

        internal static string NormalizeSeparator(string separator)
        {
            if (separator == null || separator.Length == 0)
                throw new Exception("csv: el separador no puede estar vacío.");
            switch (separator.Trim().ToLowerInvariant())
            {
                case "\\t":
                case "tab":
                case "tabulador":
                    return "\t";
            }
            if (separator == "\t") return "\t";
            string s = separator.Trim();
            if (s.Length != 1)
                throw new Exception("csv: el separador debe ser un solo carácter (se recibió '" + separator + "').");
            if (s == "\"" || s == "\r" || s == "\n")
                throw new Exception("csv: el separador no puede ser comillas ni salto de línea.");
            return s;
        }

        /// <summary>Primera línea "sep=X" (convención de Excel): devuelve el separador y la quita del texto.</summary>
        private static string SepLine(ref string text)
        {
            string t = (text.Length > 0 && text[0] == '\uFEFF') ? text.Substring(1) : text;
            if (t.StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
            {
                int nl = t.IndexOf('\n');
                string line = (nl < 0 ? t : t.Substring(0, nl)).TrimEnd('\r');
                if (line.Length == 5)
                {
                    text = nl < 0 ? "" : t.Substring(nl + 1);
                    return line.Substring(4, 1);
                }
            }
            return null;
        }

        /// <summary>Cuenta , ; y tabulador fuera de comillas en la primera línea lógica (encabezados).</summary>
        internal static string DetectSeparator(string text)
        {
            int comma = 0, semi = 0, tab = 0;
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"') { quoted = !quoted; continue; }
                if (quoted) continue;
                if (c == '\n' || c == '\r') break;
                if (c == ',') comma++;
                else if (c == ';') semi++;
                else if (c == '\t') tab++;
            }
            if (tab > comma && tab >= semi) return "\t";
            if (semi > comma) return ";";
            return ",";
        }

        private static HashSet<string> NumCols(string numcols)
        {
            var h = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(numcols))
                foreach (string c in numcols.Split(','))
                    if (c.Trim() != "") h.Add(c.Trim());
            return h;
        }

        private static List<object> Parse(string text, string numcols, string separator)
        {
            string sepl = SepLine(ref text);
            string sep = separator ?? sepl ?? DetectSeparator(text);
            var nums = NumCols(numcols);
            var result = new List<object>();

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = sep,
                Mode = CsvMode.RFC4180,
                // Un campo sin comillas que contiene " se toma tal cual (antes salía nulo)
                BadDataFound = null,
                MissingFieldFound = null,
                DetectColumnCountChanges = false,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.None,
            };

            try
            {
                using (var reader = new StringReader(text))
                using (var csv = new CsvReader(reader, cfg))
                {
                    if (!csv.Read())
                        return result;
                    csv.ReadHeader();
                    string[] header = csv.HeaderRecord ?? new string[0];
                    string[] names = UniqueNames(header);

                    while (csv.Read())
                    {
                        var rec = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < names.Length; i++)
                        {
                            string v;
                            if (!csv.TryGetField<string>(i, out v)) v = null;
                            if (i >= csv.Parser.Count) v = null; // la fila trae menos columnas
                            rec[names[i]] = v;
                        }
                        if (nums.Count > 0)
                            foreach (string n in names)
                                if (nums.Contains(n))
                                    rec[n] = ToNumber(rec[n] as string, sep);
                        result.Add(rec);
                    }
                }
            }
            catch (CsvHelperException ex)
            {
                int row = ex.Context?.Parser?.Row ?? 0;
                throw new Exception("csv: no se pudo leer el texto en la fila " + row.ToString(CultureInfo.InvariantCulture) + ": " + ex.Message, ex);
            }
            return result;
        }

        /// <summary>Encabezados repetidos o vacíos: "col", "col_2"... ; vacío → "columna_N".</summary>
        private static string[] UniqueNames(string[] header)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var r = new string[header.Length];
            for (int i = 0; i < header.Length; i++)
            {
                string n = header[i] ?? "";
                if (n == "") n = "columna_" + (i + 1).ToString(CultureInfo.InvariantCulture);
                string b = n; int k = 2;
                while (seen.Contains(n)) n = b + "_" + (k++).ToString(CultureInfo.InvariantCulture);
                seen.Add(n);
                r[i] = n;
            }
            return r;
        }

        /// <summary>
        /// Conversión de columnas numéricas (cultura invariante, igual que el original: NumberStyles.Any,
        /// nulo → 0, lo que no es número se queda como texto). En archivos con ";" un valor con coma y sin
        /// punto se lee con coma decimal ("120,50" → 120.50).
        /// </summary>
        private static object ToNumber(string s, string sep)
        {
            string t = s ?? "0";
            if (sep == ";" && t.IndexOf(',') >= 0 && t.IndexOf('.') < 0)
                t = t.Replace(',', '.');
            decimal v;
            if (decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                return v;
            return s;
        }

        private static string Build(object data, string sep)
        {
            if (data == null)
                throw new Exception("csv: la lista de registros es nula.");
            IList list = data as IList;
            if (list == null)
                throw new Exception("csv: se esperaba una lista de registros.");

            // Columnas: unión de los campos de todos los registros, en orden de aparición
            var cols = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object o in list)
            {
                var d = o as IDictionary<string, object>;
                if (d == null)
                    throw new Exception("csv: cada elemento de la lista debe ser un registro.");
                foreach (string k in d.Keys)
                    if (seen.Add(k)) cols.Add(k);
            }

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = sep, NewLine = "\r\n" };
            using (var sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var csv = new CsvWriter(sw, cfg))
                {
                    if (cols.Count > 0)
                    {
                        foreach (string c in cols) csv.WriteField(c);
                        csv.NextRecord();
                    }
                    foreach (object o in list)
                    {
                        var d = (IDictionary<string, object>)o;
                        foreach (string c in cols)
                        {
                            object v;
                            d.TryGetValue(c, out v);
                            csv.WriteField(Format(v));
                        }
                        csv.NextRecord();
                    }
                }
                return sw.ToString();
            }
        }

        private static string Format(object v)
        {
            if (v == null) return "";
            if (v is string) return (string)v;
            if (v is IFormattable) return ((IFormattable)v).ToString(null, CultureInfo.InvariantCulture);
            return v.ToString();
        }
    }
}
