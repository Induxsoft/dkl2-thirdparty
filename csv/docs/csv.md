# csv.dkh

Encabezado: `csv.dkh` · DLL: `Devkron.Ext.Csv.dll` (clase `Devkron.Ext.Csv.Csv`) · depende de `CsvHelper.dll` 33.1.0.

## Lectura

### csv.read(archivo, columnas_numericas) → lista

Lee el archivo y devuelve una lista de registros; la primera fila son los nombres de campo.

- **Codificación**: UTF-8 por omisión; si el archivo trae BOM (UTF-8 o UTF-16) se respeta.
- **Separador**: si la primera línea es `sep=X` (convención de Excel) se usa `X` y esa línea se
  quita. Si no, se cuentan `,` `;` y tabuladores fuera de comillas en la línea de encabezados: gana
  el tabulador si hay más que comas y al menos tantos como `;`; después `;` si hay más que comas; si
  no, coma.
- **Comillas**: RFC 4180 (`"a, b"`, `""` dentro de comillas, saltos de línea dentro de comillas). Un
  campo sin comillas que contiene `"` se toma tal cual.
- **Encabezados**: vacío → `columna_N` (N desde 1); repetido → `nombre_2`, `nombre_3`… Los nombres
  no distinguen mayúsculas.
- **Filas**: las líneas en blanco se omiten. Si una fila trae menos columnas, las que faltan valen
  nulo; si trae más, las sobrantes se ignoran.
- **Columnas numéricas**: `columnas_numericas` es una cadena con nombres separados por comas. Esos
  campos se convierten con `NumberStyles.Any` y cultura invariante (`1,500.00` → `1500.00`);
  vacío o nulo → `0`; lo que no es número se queda como texto. Si el separador es `;` y el valor
  trae coma pero no punto, la coma se toma como decimal (`120,50` → `120.50`).
- Archivo solo con encabezados → lista vacía.

### csv.read_sep(archivo, columnas_numericas, separador) → lista

Como `csv.read` pero sin detección. `separador` es un carácter; se aceptan además `"\\t"`, `"tab"`
y `"tabulador"` para el tabulador. Comillas y saltos de línea no son separadores válidos.

### csv.from_text(texto, columnas_numericas) / csv.from_text_sep(texto, columnas_numericas, separador) → lista

Lo mismo sobre una cadena (por ejemplo `httpreq.body()` o el contenido de un archivo subido).

### csv.detect(texto) → cadena

El separador que usaría `csv.read` para ese texto: `","`, `";"` o `"\t"`.

## Escritura

### csv.write(archivo, lista) / csv.write_sep(archivo, lista, separador)

Escribe una lista de registros. Columnas: la unión de los campos de todos los registros en el orden
en que aparecen; un registro sin un campo deja la celda vacía. Números con cultura invariante
(`1234.5`), comillas solo cuando hacen falta, fin de línea `\r\n`, UTF-8 con BOM.

### csv.to_text(lista) / csv.to_text_sep(lista, separador) → cadena

Lo mismo, devuelto como cadena (sin BOM).

## Errores

Mensajes con el prefijo `csv:`: archivo inexistente, ruta vacía, separador inválido, lista nula o
con elementos que no son registros, y errores de interpretación con el número de fila.

```dkl
leer:: ruta
{
    ref l = csv.read(ruta, "")
    return list.count(l)
    exception
    {
        do print("No se pudo leer: " + last_error())
    }
}
```
