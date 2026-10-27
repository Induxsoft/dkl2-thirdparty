// ---------------------------------------------------------------------------------
//  Devkron.Ext.Image - imágenes para Devkron 2 (usa Magick.NET / ImageMagick)
//  Copyright 2019-2026 Induxsoft Data Services S de RL de CV
//  Licencia: Apache 2.0 (ver LICENSE en la raíz del repositorio)
//  Basado en Devkron.Magick (devkron/API/magick/ImageTools.cs).
// ---------------------------------------------------------------------------------

using ImageMagick;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Devkron.Ext.Image
{
    public static class Image
    {
        static Image()
        {
            // dkl carga esta DLL con Assembly.LoadFile, sin el deps.json de la biblioteca: el nativo
            // (Magick.Native-Q16-HDRI-x64.dll.so / .dll) se busca junto a esta DLL.
            try
            {
                string dir = Path.GetDirectoryName(typeof(Image).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    if (File.Exists(Path.Combine(dir, "Magick.Native-Q16-HDRI-x64.dll.so")) ||
                        File.Exists(Path.Combine(dir, "Magick.Native-Q16-HDRI-x64.dll")) ||
                        File.Exists(Path.Combine(dir, "Magick.Native-Q16-HDRI-arm64.dll.so")) ||
                        File.Exists(Path.Combine(dir, "Magick.Native-Q16-HDRI-arm64.dll")))
                        MagickNET.SetNativeLibraryDirectory(dir);
                }
            }
            catch (Exception) { }
            try { OpenCL.IsEnabled = false; } catch (Exception) { }
        }

        /// <summary>
        /// image.size(archivo) → registro {width, height, size_kb}; nulo si no es una imagen (como el original).
        /// size_kb: tamaño del archivo en KB con 2 decimales (lo agrega la Devkron.Magick.dll publicada).
        /// </summary>
        public static object Size(string file)
        {
            try
            {
                IDictionary<string, object> rec = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                var info = new MagickImageInfo(file);
                rec["height"] = (int)info.Height;
                rec["width"] = (int)info.Width;
                rec["size_kb"] = Math.Round(new FileInfo(file).Length / 1024.0, 2);
                return rec;
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// image.resize(archivo, ancho, alto): sobrescribe el archivo. Si ancho o alto es 0 conserva la
        /// proporción; si los dos son distintos de 0, deforma a ese tamaño exacto (como el original).
        /// Devuelve 1/0 (no lanza error, como el original).
        /// </summary>
        public static bool Resize(string imageFile, decimal w, decimal h)
        {
            try
            {
                using (var img = new MagickImage(imageFile))
                {
                    var g = new MagickGeometry(ToUInt(w), ToUInt(h));
                    g.IgnoreAspectRatio = (w != 0 && h != 0);
                    img.Resize(g);
                    img.Write(imageFile);
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// image.compress(archivo, optimo, con_perdida): optimiza en su lugar con ImageOptimizer (JPEG, PNG,
        /// GIF, ICO). Devuelve 1 si redujo el archivo, 0 si no (formato no admitido o ya optimizado).
        /// </summary>
        public static bool Compress(string imageFile, bool optimal, bool withLoss)
        {
            CheckExists(imageFile, "image.compress");
            try
            {
                var opt = new ImageOptimizer();
                opt.OptimalCompression = optimal;
                opt.IgnoreUnsupportedFormats = true;
                return withLoss ? opt.Compress(imageFile) : opt.LosslessCompress(imageFile);
            }
            catch (Exception ex)
            {
                throw new Exception("image: no se pudo comprimir '" + Path.GetFileName(imageFile) + "': " + ex.Message, ex);
            }
        }

        /// <summary>image.thumbnail(origen, destino, lado_mayor): miniatura proporcional; no agranda imágenes chicas.</summary>
        public static bool Thumbnail(string source, string target, decimal maxSide)
        {
            CheckExists(source, "image.thumbnail");
            CheckTarget(target, "image.thumbnail");
            if (maxSide < 1)
                throw new Exception("image: el lado mayor de la miniatura debe ser de 1 píxel o más.");
            uint side = ToUInt(maxSide);
            try
            {
                using (var img = new MagickImage(source))
                {
                    img.AutoOrient();
                    if (img.Width > side || img.Height > side)
                        img.Thumbnail(new MagickGeometry(side, side)); // proporcional, quita metadatos
                    else
                        img.Strip();
                    WriteAs(img, target);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("image: no se pudo crear la miniatura de '" + Path.GetFileName(source) + "': " + ex.Message, ex);
            }
        }

        /// <summary>image.convert(origen, destino): cambia de formato según la extensión del destino.</summary>
        public static bool Convert(string source, string target)
        {
            CheckExists(source, "image.convert");
            CheckTarget(target, "image.convert");
            try
            {
                using (var img = new MagickImage(source))
                {
                    WriteAs(img, target);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("image: no se pudo convertir '" + Path.GetFileName(source) + "' a '" + Path.GetFileName(target) + "': " + ex.Message, ex);
            }
        }

        /// <summary>image.newer(archivo1, archivo2) (compatibilidad con magick.dkh): 1, 2, 0 si son iguales; -1/-2 si falta uno.</summary>
        public static decimal WichIsNewer(string file1, string file2)
        {
            var f1 = new FileInfo(file1);
            var f2 = new FileInfo(file2);
            if (!f1.Exists) return -1;
            if (!f2.Exists) return -2; // el original revisaba f1 dos veces
            if (f1.LastWriteTimeUtc > f2.LastWriteTimeUtc) return 1;
            if (f2.LastWriteTimeUtc > f1.LastWriteTimeUtc) return 2;
            return 0;
        }

        // ------------------------------------------------------------------ interno

        private static void WriteAs(MagickImage img, string target)
        {
            MagickFormat fmt = FormatFor(target);
            string dir = Path.GetDirectoryName(Path.GetFullPath(target));
            if (!Directory.Exists(dir))
                throw new Exception("no existe la carpeta de destino '" + dir + "'");
            if ((fmt == MagickFormat.Jpeg || fmt == MagickFormat.Bmp) && img.HasAlpha)
            {
                img.BackgroundColor = MagickColors.White; // JPEG/BMP sin transparencia: fondo blanco
                img.Alpha(AlphaAction.Remove);
            }
            img.Write(target, fmt);
        }

        private static MagickFormat FormatFor(string target)
        {
            string ext = Path.GetExtension(target).ToLowerInvariant().TrimStart('.');
            switch (ext)
            {
                case "jpg":
                case "jpeg": return MagickFormat.Jpeg;
                case "png": return MagickFormat.Png;
                case "gif": return MagickFormat.Gif;
                case "webp": return MagickFormat.WebP;
                case "bmp": return MagickFormat.Bmp;
                case "tif":
                case "tiff": return MagickFormat.Tiff;
                case "ico": return MagickFormat.Ico;
                case "avif": return MagickFormat.Avif;
                case "heic": return MagickFormat.Heic;
                case "pdf": return MagickFormat.Pdf;
                case "": throw new Exception("el destino '" + Path.GetFileName(target) + "' no tiene extensión para elegir el formato");
                default:
                    throw new Exception("formato de destino no admitido: ." + ext + " (se admiten jpg, png, gif, webp, bmp, tif, ico, avif, heic, pdf)");
            }
        }

        private static void CheckExists(string file, string fn)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("image: falta la ruta del archivo en " + fn + ".");
            if (!File.Exists(file))
                throw new Exception("image: no existe el archivo '" + file + "'.");
        }

        private static void CheckTarget(string file, string fn)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new Exception("image: falta la ruta del archivo de destino en " + fn + ".");
        }

        private static uint ToUInt(decimal v)
        {
            if (v <= 0) return 0;
            if (v > uint.MaxValue) return uint.MaxValue;
            return (uint)Math.Round(v, MidpointRounding.AwayFromZero);
        }
    }
}
