# doc.dkh

Encabezado: `doc.dkh` · DLL: `Devkron.Ext.Office.dll` (clase `Devkron.Ext.Office.Doc`) · usa
PdfPig 0.1.16, DocumentFormat.OpenXml 3.5.1 y ClosedXML 0.105.1.

## doc.text(archivo) → cadena

Texto según la extensión:

- `.pdf`: texto de todas las páginas (separadas por `\n`), armado por renglones con las palabras de
  cada página. Sin OCR: un PDF escaneado (solo imágenes) devuelve `""`. PDF cifrado o dañado → error.
- `.docx`: un párrafo por línea (incluye los párrafos de las tablas, una celda por línea);
  tabuladores y saltos de línea manuales se conservan.
- `.xlsx` / `.xlsm`: por hoja, una línea `--- Hoja: <nombre> ---` y luego una línea por fila con
  las celdas separadas (y terminadas) por tabulador, como `induxsoft.txtract`.

Otras extensiones (`.doc`, `.xls`, `.txt`…) → error `doc: extensión no admitida`.

## doc.pages(archivo) → lista

Solo PDF: una cadena por página, en orden.

```dkl
ref paginas = doc.pages("factura.pdf")
do print("Páginas: " + str(list.count(paginas)) + "\n")
do print(list.str(paginas, 0))
```

## Errores

Prefijo `doc:`: archivo inexistente, extensión no admitida, `doc.pages` con algo que no es PDF, y
documento que no se pudo leer (con el motivo que da la biblioteca).
