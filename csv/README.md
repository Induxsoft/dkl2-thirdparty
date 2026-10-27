# dkl2-csv 1.0.0

Lectura y escritura de CSV para Devkron 2: `csv.dkh` + `Devkron.Ext.Csv.dll`, sobre
[CsvHelper](https://joshclose.github.io/CsvHelper/) 33.1.0 (MS-PL o Apache-2.0).

Sustituye a `devkron.serialize.csv.dll` (`CsvRead`/`CsvWrite`, que se mapeaban a mano) con nombres
estándar. Parte de `dkl-linux-fixes/csv/csv/csvHelper.cs`.

## Instalación

Descomprima `dkl2-csv-1.0.0.zip` en la carpeta de `dkl` (trae `Devkron.Ext.Csv.dll`,
`CsvHelper.dll`, `csv.dkh` y las licencias) e incluya `csv.dkh` dentro del nodo raíz.

## Funciones

| Función | Devuelve |
|---|---|
| `csv.read(archivo, columnas_numericas)` | lista de registros; separador detectado (`,` `;` tabulador) |
| `csv.read_sep(archivo, columnas_numericas, separador)` | igual, con separador explícito (`","`, `";"`, `"\\t"` o `"tab"`, `"\|"`…) |
| `csv.from_text(texto, columnas_numericas)` | lista, desde una cadena (CSV que llega en una petición) |
| `csv.from_text_sep(texto, columnas_numericas, separador)` | igual, con separador explícito |
| `csv.detect(texto)` | separador que usaría `csv.read` |
| `csv.write(archivo, lista)` | escribe con coma, UTF-8 con BOM (Excel lo abre con acentos) |
| `csv.write_sep(archivo, lista, separador)` | escribe con el separador indicado |
| `csv.to_text(lista)` / `csv.to_text_sep(lista, separador)` | cadena CSV (para `httpres.text`) |

`columnas_numericas` es una lista separada por comas (`"precio,existencia"`, o `""`): esas
columnas se convierten a número con cultura invariante; un campo vacío queda como cadena vacía `""`
(revise el tipo con `field.typeof` antes de sumar); si a la fila le falta el campo, vale `0`; lo que no
es número se queda como texto.

```dkl
#include "functions.dkh"
#include "serialize.dkh"
#include "csv.dkh"

ref filas = csv.read("existencias.csv", "existencia,costo")
go foreach.lobj(index:i, value:r) with(filas)
{
    do print(r.clave + ": " + str(r.existencia) + "\n")
}

ref salida = list.create()
new r
{
    @"clave": "M-1"
    @"precio": 120.5
}
do list.add(salida, r)
do csv.write("salida.csv", salida)
@http_context<"response/text">: csv.to_text(salida)
```

Detalle completo en [docs/csv.md](docs/csv.md).

## Comparación con `devkron.serialize.csv.dll`

`csv.read` se comparó con `CsvRead(archivo, columnas)` de la DLL original (con su CsvHelper 30.0.1)
sobre los CSV de `tests/data`: el resultado es el mismo (mismos nombres de campo, valores, nulos en
filas cortas, BOM, comillas dobles, saltos de línea entre comillas) salvo los defectos corregidos:

| Caso | Original | Ahora |
|---|---|---|
| campo sin comillas con `"` dentro (`Pinza 8"`) | el campo sale `null` y la columna numérica `0` | `Pinza 8"` y `99.9` |
| archivo con `;` | un solo campo `"id;nombre;precio"` | separador detectado; `120,50` → `120.50` |
| archivo con tabulador | un solo campo | separador detectado |
| encabezados repetidos o vacíos | error de CsvHelper | `a`, `a_2`, `columna_3` |
| línea `sep=;` (Excel) | se tomaba como encabezado | se usa como separador y se quita |

Otras diferencias: el original leía el archivo con la codificación por omisión del `StreamReader`
(UTF-8 con detección de BOM), igual que ahora; `csv.write` agrega la BOM UTF-8 como el original;
el original solo escribía las columnas del primer registro, ahora usa la unión de las columnas de
todos los registros.

## Pruebas

```
DKL=/ruta/dkl tests/run.sh
```

Kits: `read.dkl` (lectura, detección, `read_sep`), `text.dkl` (`from_text`, escritura e ida y
vuelta), `errores.dkl` (mensajes). Resultado actual: 3/3.

## Hallazgos de seguridad

- **Lectura completa a memoria** (inferido): `src/Csv.cs` línea 96 lee el archivo entero con
  `ReadToEnd()` antes de interpretarlo, sin límite de tamaño; un CSV muy grande subido por un
  usuario puede agotar la memoria del proceso FastCGI. El original (`csvHelper.cs`, línea 43) leía
  por flujo pero también acumulaba todo en una lista.
- **Inyección de fórmulas en CSV** (verificado): `csv.write`/`csv.to_text` (`src/Csv.cs` línea 291)
  escriben los valores tal cual; un valor que empieza con `=`, `+`, `-` o `@` se ejecuta como
  fórmula si el archivo se abre en Excel. Igual que el original.
