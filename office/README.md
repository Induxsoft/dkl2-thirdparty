# dkl2-office 1.0.0

Hojas de cálculo XLSX y texto de documentos para Devkron 2: `xlsx.dkh` y `doc.dkh` sobre
`Devkron.Ext.Office.dll`, que usa [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.105.1 (MIT),
[Open XML SDK](https://github.com/dotnet/Open-XML-SDK) 3.5.1 (MIT) y
[PdfPig](https://github.com/UglyToad/PdfPig) 0.1.16 (Apache-2.0).

`doc.text` parte de `internal-devs/induxsoft.txtract` (`txtract.cs`); `xlsx.*` y `doc.pages` son nuevos.

## Instalación

Descomprima `dkl2-office-1.0.0.zip` en la carpeta de `dkl`. Trae `Devkron.Ext.Office.dll`,
ClosedXML, ClosedXML.Parser, DocumentFormat.OpenXml(.Framework), System.IO.Packaging,
ExcelNumberFormat, RBush, SixLabors.Fonts 1.0.0, los `UglyToad.PdfPig*.dll`, `xlsx.dkh`, `doc.dkh` y
las licencias. Todo es .NET administrado (sin nativos): funciona igual en Linux y Windows.

## Funciones

| Función | Devuelve |
|---|---|
| `xlsx.write(archivo, lista)` | libro con una hoja `Hoja1`: encabezados en negritas y fila 1 fija |
| `xlsx.write_sheets(archivo, hojas)` | libro con varias hojas: registro `{nombre_hoja: lista, …}` (campos `*`) |
| `xlsx.to_bytes(lista)` | bytes del libro, para `httpres.binary` sin archivo temporal |
| `xlsx.read(archivo, hoja)` | lista de registros; la primera fila son los nombres; `hoja` = `""` → la primera |
| `xlsx.sheets(archivo)` | lista con los nombres de las hojas |
| `doc.text(archivo)` | texto de `.pdf`, `.docx`, `.xlsx`/`.xlsm` según la extensión (PDF sin OCR) |
| `doc.pages(archivo)` | lista con el texto de cada página (solo PDF) |

```dkl
#include "functions.dkh"
#include "xlsx.dkh"
#include "doc.dkh"

ref ventas = xlsx.read("ventas.xlsx", "Ventas")
go foreach.lobj(index:i, value:r) with(ventas)
{
    do print(r.folio + " " + str(r.importe) + "\n")
}

new hojas
{
    @"Ventas*": ventas
    @"Clientes*": clientes
}
do xlsx.write_sheets("reporte.xlsx", hojas)

@http_context<"response/headers/content-type">: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
@http_context<"response/output">: "binary"
@http_context<"response/binary*">: xlsx.to_bytes(ventas)

texto = doc.text("contrato.pdf")
ref paginas = doc.pages("contrato.pdf")
```

Detalle en [docs/xlsx.md](docs/xlsx.md) y [docs/doc.md](docs/doc.md).

## Diferencias con `induxsoft.txtract`

- PDF: el original usaba `page.Text`, que en muchos PDF pega las palabras sin espacios ni saltos;
  ahora se arman las líneas con las palabras de la página (`GetWords`) ordenadas por renglón.
- DOCX: el original usaba `Body.InnerText` (todo el texto junto); ahora un párrafo por línea,
  con tabuladores y saltos de línea.
- XLSX: mismo formato (`--- Hoja: X ---` y celdas separadas por tabulador); números, fechas y
  lógicos con cultura invariante en vez de la cultura del sistema.
- Errores en español; archivo inexistente o extensión no admitida se informan antes de abrir.

## Pruebas

```
DKL=/ruta/dkl tests/run.sh
```

Kits: `xlsx.dkl` (lectura de un libro generado con openpyxl con fechas, lógicos, fórmula sin valor
guardado, encabezados vacíos y repetidos, hoja vacía; escritura de una y varias hojas y `to_bytes`
con ida y vuelta), `doc.dkl` (PDF de 2 páginas, DOCX con tabla y tabulador, XLSX) y `errores.dkl`.
Resultado actual: 3/3.

## Hallazgos de seguridad

- **Archivos grandes o maliciosos sin límite** (inferido): `src/Xlsx.cs` línea 130
  (`new XLWorkbook`), `src/Doc.cs` líneas 59 (`PdfDocument.Open`) y 92
  (`WordprocessingDocument.Open`) cargan el documento completo en memoria sin tope de tamaño ni
  de páginas; un XLSX tipo "zip bomb" o un PDF enorme subido por un usuario puede agotar la memoria
  del proceso FastCGI. El original `txtract.cs` hacía lo mismo.
- **Inyección de fórmulas** (verificado que no aplica): `xlsx.write` (`src/Xlsx.cs` línea
  189) guarda las cadenas como texto, no como fórmula, así que `=CMD(...)` no se ejecuta al abrir
  el libro.
- **Fórmulas del libro** (inferido): cuando un libro trae fórmulas sin valor guardado, `xlsx.read`
  las recalcula con el motor de ClosedXML (sin funciones externas ni macros); un libro con fórmulas
  muy pesadas puede tardar.
