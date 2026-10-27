# dkl2-thirdparty

Bibliotecas estándar de **Devkron 2** que dependen de código de terceros. Cada una vive en su
carpeta, tiene su versión y se instala aparte del motor copiando su zip junto a `dkl`.

| Carpeta | Paquete | Encabezados | DLL propia | Terceros incluidos |
|---|---|---|---|---|
| [`csv/`](csv/) | `dkl2-csv-1.0.0.zip` | `csv.dkh` | `Devkron.Ext.Csv.dll` | CsvHelper 33.1.0 |
| [`office/`](office/) | `dkl2-office-1.0.0.zip` | `xlsx.dkh`, `doc.dkh` | `Devkron.Ext.Office.dll` | ClosedXML 0.105.1, Open XML SDK 3.5.1, PdfPig 0.1.16 |
| [`image/`](image/) | `dkl2-image-1.0.0.zip` | `image.dkh` | `Devkron.Ext.Image.dll` | Magick.NET 15.0.0 (ImageMagick 7.1.2-33) |
| [`html/`](html/) | `dkl2-html-1.0.0.zip` | `html.dkh` | `Devkron.Ext.Html.dll` | HtmlAgilityPack 1.13.1 |
| [`markdown/`](markdown/) | `dkl2-markdown-1.0.0.zip` | `markdown.dkh` | — | Markdig 1.5.0 |

Las bibliotecas **sin** código de terceros (threads, http, url, zip, time, text con
`html.encode`/`html.decode`/`html.attrs`, hash, fs, log) se publican con el motor, en el repo
`devkron2`.

## Instalación

1. Descargue el zip de la biblioteca (sección *Releases* o `build/pack.sh`).
2. Descomprímalo **en la carpeta donde está `dkl`** (la misma de `functions.dkh`):

   ```
   unzip -o dkl2-csv-1.0.0.zip -d /opt/devkron
   ```

   En Windows, extraiga el contenido en la carpeta de `dkl.exe`.
3. En su programa, incluya el encabezado dentro del nodo raíz:

   ```dkl
   #include "dkli.dkh"
   #!
   program "ejemplo"
   {
       #include "functions.dkh"
       #include "csv.dkh"

       ref filas = csv.read("clientes.csv", "saldo")
   }
   ```

`_import_library` sin ruta carga la DLL desde la carpeta de `dkl`, por eso todo va ahí. Los
nombres de archivo llevan el prefijo de la biblioteca (`dkl2-image.LICENSE.txt`, …) para que
varias convivan en la misma carpeta. `image` trae los nativos de ImageMagick para Linux x64
(glibc) y Windows x64; otras plataformas (ARM, musl/Alpine, macOS) no están en el zip.

## Compilar, empaquetar y probar

Requiere el SDK de .NET 10 y `zip`.

```
build/pack.sh                    # todas → dist/dkl2-<biblioteca>-<versión>.zip
build/pack.sh csv image          # solo algunas
DKL_DIST=/ruta/de/dkl build/test-all.sh   # instala los zip en una copia de esa distribución y corre todos los kits
DKL=/ruta/dkl csv/tests/run.sh   # kits de una biblioteca contra una distribución que ya la tiene instalada
```

`test-all.sh` no toca la distribución que recibe: la copia a `build/out/dkl`. Cada kit es un
programa `.dkl` con su salida esperada (`.esperado`); `ACTUALIZAR=1 …/run.sh` la regenera.
Las pruebas de `html.from_url` usan un servidor HTTP local en `127.0.0.1` (python3), no internet.

## Convenciones

- Errores en español con el prefijo de la biblioteca (`csv: no existe el archivo '…'`); el motor
  los entrega como `Error en 'csv.read. (…)'` y se atrapan con `exception { }` y `last_error()`.
- Números y fechas con cultura invariante. Las fechas que salen de un libro XLSX son números DKL
  `AAAAMMDD.HHMMSS`.
- Los registros que devuelven no distinguen mayúsculas y minúsculas en los nombres de campo.

## Licencias

El código de este repositorio (C# y `.dkh`) está bajo **Apache 2.0** ([LICENSE](LICENSE)). Cada
biblioteca redistribuye componentes de terceros bajo sus propias licencias; están en el
`LICENSE-THIRDPARTY` de su carpeta (y dentro de su zip): MIT, BSD-2-Clause, Apache-2.0,
MS-PL/Apache-2.0 y la ImageMagick License, más las de las bibliotecas que trae el nativo de
ImageMagick (`image/NOTICE-Magick.NET.txt`).

## Hallazgos de seguridad

Cada carpeta documenta los suyos en su README (tema, ruta y línea, verificado o inferido).
