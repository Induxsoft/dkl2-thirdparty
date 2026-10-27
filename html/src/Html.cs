// ---------------------------------------------------------------------------------
//  Devkron.Ext.Html - lectura de páginas HTML para Devkron 2 (usa HtmlAgilityPack)
//  Copyright 2019-2026 Induxsoft Data Services S de RL de CV
//  Licencia: Apache 2.0 (ver LICENSE en la raíz del repositorio)
//  Basado en HtmlScraper.Document (spiderbot/src/cs/spider-bot/htmlscraper/Document.cs).
// ---------------------------------------------------------------------------------

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Devkron.Ext.Html
{
    public static class Html
    {
        private const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
        private const int MaxBytes = 20 * 1024 * 1024;

        // Un solo HttpClient por proceso (sin estado de encabezados: van en cada petición)
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                UseCookies = false,
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        }

        /// <summary>html.parse(html, url_base) → registro {url, title, description, keywords, meta, links, text}.</summary>
        public static IDictionary<string, object> Parse(string html, string urlBase)
        {
            var doc = Load(html);
            var rec = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            rec["url"] = urlBase ?? "";
            rec["title"] = Title(doc);
            var meta = Meta(doc);
            rec["meta"] = meta;
            rec["description"] = meta.TryGetValue("description", out object de) ? de : "";
            rec["keywords"] = meta.TryGetValue("keywords", out object kw) ? kw : "";
            rec["links"] = Links(doc, urlBase);
            string text = TextOf(doc.DocumentNode);
            rec["text"] = text;
            rec["content"] = text; // nombre del original (GetDataFromHtml)
            return rec;
        }

        /// <summary>html.from_url(url, encabezados) → descarga con GET y aplica html.parse; agrega status y final_url.</summary>
        public static IDictionary<string, object> FromUrl(string url, object headers)
        {
            string finalUrl;
            int status;
            string html = Download(url, headers, out finalUrl, out status);
            var rec = Parse(html, finalUrl);
            rec["status"] = status;
            rec["final_url"] = finalUrl;
            return rec;
        }

        /// <summary>html.get(url, encabezados) → solo el HTML descargado (cadena).</summary>
        public static string Get(string url, object headers)
        {
            string finalUrl; int status;
            return Download(url, headers, out finalUrl, out status);
        }

        /// <summary>html.text(html) → solo el texto visible (sin script, style ni comentarios), una línea por bloque.</summary>
        public static string Text(string html)
        {
            return TextOf(Load(html).DocumentNode);
        }

        /// <summary>html.select(html, xpath) → lista con el texto de cada nodo que coincide (vacía si no hay).</summary>
        public static List<object> Select(string html, string xpath)
        {
            var l = new List<object>();
            var nodes = SelectNodes(Load(html), xpath);
            if (nodes != null)
                foreach (var n in nodes)
                    l.Add(n.NodeType == HtmlNodeType.Element || n.NodeType == HtmlNodeType.Document
                        ? TextOf(n) : HtmlEntity.DeEntitize(n.InnerText ?? "").Trim());
            return l;
        }

        /// <summary>html.select_attr(html, xpath, atributo) → lista con el valor del atributo en cada nodo.</summary>
        public static List<object> SelectAttr(string html, string xpath, string attribute)
        {
            var l = new List<object>();
            var nodes = SelectNodes(Load(html), xpath);
            if (nodes != null)
                foreach (var n in nodes)
                    l.Add(HtmlEntity.DeEntitize(n.GetAttributeValue(attribute ?? "", "")));
            return l;
        }

        /// <summary>html.select_html(html, xpath) → lista con el HTML externo de cada nodo.</summary>
        public static List<object> SelectHtml(string html, string xpath)
        {
            var l = new List<object>();
            var nodes = SelectNodes(Load(html), xpath);
            if (nodes != null)
                foreach (var n in nodes)
                    l.Add(n.OuterHtml);
            return l;
        }

        // ------------------------------------------------------------------ interno

        private static HtmlDocument Load(string html)
        {
            var doc = new HtmlDocument();
            doc.OptionMaxNestedChildNodes = 2000; // evita desbordar la pila con HTML malicioso muy anidado
            doc.LoadHtml(html ?? "");
            return doc;
        }

        private static HtmlNodeCollection SelectNodes(HtmlDocument doc, string xpath)
        {
            if (string.IsNullOrWhiteSpace(xpath))
                throw new Exception("html: falta la expresión XPath.");
            try { return doc.DocumentNode.SelectNodes(xpath); }
            catch (Exception ex) { throw new Exception("html: la expresión XPath '" + xpath + "' no es válida: " + ex.Message, ex); }
        }

        private static string Download(string url, object headers, out string finalUrl, out int status)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
                throw new Exception("html: la URL '" + url + "' no es absoluta.");
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new Exception("html: solo se admiten URL http y https ('" + url + "').");

            var h = headers as IDictionary<string, object>;
            if (headers != null && h == null)
                throw new Exception("html: los encabezados deben ser un registro.");

            using (var req = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                bool ua = false, accept = false;
                int timeout = 30;
                if (h != null)
                {
                    foreach (var kv in h)
                    {
                        string name = (kv.Key ?? "").Trim();
                        string value = kv.Value == null ? "" : Convert.ToString(kv.Value, System.Globalization.CultureInfo.InvariantCulture);
                        if (name == "") continue;
                        if (name.Equals("timeout", StringComparison.OrdinalIgnoreCase))
                        {
                            int t; if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out t) && t > 0) timeout = t;
                            continue;
                        }
                        if (name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)) ua = true;
                        if (name.Equals("Accept", StringComparison.OrdinalIgnoreCase)) accept = true;
                        if (!req.Headers.TryAddWithoutValidation(name, value))
                            throw new Exception("html: no se puede enviar el encabezado '" + name + "' en una petición GET.");
                    }
                }
                if (!ua) req.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
                if (!accept) req.Headers.TryAddWithoutValidation("Accept", "text/html");

                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
                    using (var resp = Client.Send(req, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                    {
                        status = (int)resp.StatusCode;
                        finalUrl = (resp.RequestMessage?.RequestUri ?? uri).AbsoluteUri;
                        if (!resp.IsSuccessStatusCode)
                            throw new Exception("html: el servidor respondió " + status.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + resp.ReasonPhrase + " para '" + url + "'.");
                        using (var s = resp.Content.ReadAsStream(cts.Token))
                        {
                            byte[] body = ReadLimited(s);
                            return Decode(body, resp.Content.Headers.ContentType?.CharSet);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    throw new Exception("html: se agotó el tiempo de espera (" + timeout.ToString(System.Globalization.CultureInfo.InvariantCulture) + " s) al descargar '" + url + "'.", ex);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception("html: no se pudo descargar '" + url + "': " + ex.Message, ex);
                }
            }
        }

        private static byte[] ReadLimited(Stream s)
        {
            using (var ms = new MemoryStream())
            {
                byte[] buf = new byte[81920];
                int n;
                while ((n = s.Read(buf, 0, buf.Length)) > 0)
                {
                    if (ms.Length + n > MaxBytes)
                        throw new Exception("html: la página pasa de " + (MaxBytes / 1024 / 1024).ToString(System.Globalization.CultureInfo.InvariantCulture) + " MB.");
                    ms.Write(buf, 0, n);
                }
                return ms.ToArray();
            }
        }

        // Juego de caracteres: el de Content-Type; si no viene, <meta charset>; si no, UTF-8
        private static string Decode(byte[] body, string charset)
        {
            Encoding enc = null;
            if (!string.IsNullOrWhiteSpace(charset))
                enc = EncodingFor(charset.Trim('"', ' '));
            if (enc == null)
            {
                string head = Encoding.ASCII.GetString(body, 0, Math.Min(body.Length, 2048));
                var m = System.Text.RegularExpressions.Regex.Match(head, "<meta[^>]+charset\\s*=\\s*[\"']?([A-Za-z0-9_\\-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) enc = EncodingFor(m.Groups[1].Value);
            }
            enc = enc ?? new UTF8Encoding(false);
            string text = enc.GetString(body);
            return text.Length > 0 && text[0] == '\uFEFF' ? text.Substring(1) : text;
        }

        private static Encoding EncodingFor(string name)
        {
            try
            {
                if (name.Equals("iso-8859-1", StringComparison.OrdinalIgnoreCase) || name.Equals("latin1", StringComparison.OrdinalIgnoreCase))
                    return Encoding.Latin1;
                return Encoding.GetEncoding(name);
            }
            catch (Exception) { return null; }
        }

        private static string Title(HtmlDocument doc)
        {
            var n = doc.DocumentNode.SelectSingleNode("//title");
            return n != null ? HtmlEntity.DeEntitize(n.InnerText).Trim() : "";
        }

        /// <summary>Registro con las etiquetas meta: name o property (og:...) → content; nombres en minúsculas.</summary>
        private static Dictionary<string, object> Meta(HtmlDocument doc)
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var ns = doc.DocumentNode.SelectNodes("//meta");
            if (ns == null) return d;
            foreach (var e in ns)
            {
                string key = e.GetAttributeValue("name", "");
                if (key == "") key = e.GetAttributeValue("property", "");
                if (key == "") key = e.GetAttributeValue("http-equiv", "");
                if (key == "")
                {
                    string cs = e.GetAttributeValue("charset", "");
                    if (cs != "" && !d.ContainsKey("charset")) d["charset"] = cs;
                    continue;
                }
                key = key.Trim().ToLowerInvariant();
                if (!d.ContainsKey(key)) // el primero gana, como GetMetaTagContent
                    d[key] = HtmlEntity.DeEntitize(e.GetAttributeValue("content", "")).Trim();
            }
            return d;
        }

        /// <summary>Enlaces absolutos únicos de los a[href], resueltos contra la URL base (o &lt;base href&gt;).</summary>
        private static List<object> Links(HtmlDocument doc, string urlBase)
        {
            Uri baseUri = null;
            Uri.TryCreate(urlBase ?? "", UriKind.Absolute, out baseUri);
            var bn = doc.DocumentNode.SelectSingleNode("//base[@href]");
            if (bn != null)
            {
                Uri b2;
                string bh = HtmlEntity.DeEntitize(bn.GetAttributeValue("href", "")).Trim();
                if (baseUri != null ? Uri.TryCreate(baseUri, bh, out b2) : Uri.TryCreate(bh, UriKind.Absolute, out b2))
                    baseUri = b2;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var l = new List<object>();
            var ns = doc.DocumentNode.SelectNodes("//a[@href]");
            if (ns == null) return l;
            foreach (var n in ns)
            {
                string u = HtmlEntity.DeEntitize(n.GetAttributeValue("href", "")).Trim();
                if (u == "" || u.StartsWith("#", StringComparison.Ordinal)) continue;
                Uri abs;
                if (Uri.TryCreate(u, UriKind.Absolute, out abs) && (abs.Scheme == "http" || abs.Scheme == "https" || abs.Scheme == "mailto" || abs.Scheme == "tel" || abs.Scheme == "ftp"))
                { }
                else if (baseUri != null && Uri.TryCreate(baseUri, u, out abs))
                { }
                else continue; // javascript:, data:, relativos sin base
                if (abs.Scheme == "javascript" || abs.Scheme == "data") continue;
                string s = abs.AbsoluteUri;
                if (seen.Add(s)) l.Add(s);
            }
            return l;
        }

        private static readonly HashSet<string> Skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "script", "style", "noscript", "template", "head", "svg", "iframe", "object" };

        private static readonly HashSet<string> Blocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "p", "div", "br", "li", "ul", "ol", "tr", "table", "h1", "h2", "h3", "h4", "h5", "h6", "section", "article",
              "header", "footer", "nav", "aside", "main", "blockquote", "pre", "dd", "dt", "dl", "form", "hr", "figure", "figcaption", "address", "title", "option" };

        /// <summary>Texto visible: como GetTextContent del original (una línea por nodo de texto, sin vacías), pero
        /// sin el contenido de head/noscript/template y uniendo el texto en línea de un mismo bloque.</summary>
        internal static string TextOf(HtmlNode root)
        {
            var sb = new StringBuilder();
            Walk(root, sb);
            var lines = sb.ToString().Split('\n');
            var r = new StringBuilder();
            foreach (string raw in lines)
            {
                string s = System.Text.RegularExpressions.Regex.Replace(raw, "[ \\r\\u00A0]+", " ");
                s = System.Text.RegularExpressions.Regex.Replace(s, " *\\t *", "\t").Trim(' ', '\t');
                if (s == "") continue;
                if (r.Length > 0) r.Append('\n');
                r.Append(s);
            }
            return r.ToString();
        }

        private static void Walk(HtmlNode node, StringBuilder sb)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    return;
                case HtmlNodeType.Text:
                    string p = node.ParentNode?.Name ?? "";
                    if (Skip.Contains(p)) return;
                    sb.Append(HtmlEntity.DeEntitize(((HtmlTextNode)node).Text).Replace('\n', ' '));
                    return;
                case HtmlNodeType.Element:
                    if (Skip.Contains(node.Name)) return;
                    bool block = Blocks.Contains(node.Name);
                    if (block) sb.Append('\n');
                    foreach (var c in node.ChildNodes) Walk(c, sb);
                    if (node.Name == "td" || node.Name == "th") sb.Append('\t');
                    else if (block) sb.Append('\n');
                    return;
                default:
                    foreach (var c in node.ChildNodes) Walk(c, sb);
                    return;
            }
        }
    }
}
