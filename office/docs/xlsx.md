# xlsx.dkh

Encabezado: `xlsx.dkh` · DLL: `Devkron.Ext.Office.dll` (clase `Devkron.Ext.Office.Xlsx`) · usa ClosedXML 0.105.1.

## xlsx.write(archivo, lista)

Crea (o reemplaza) un libro con una sola hoja `Hoja1`. Columnas: la unión de los campos de todos
los registros en el orden en que aparecen; fila 1 con los nombres en negritas y fija al desplazar.
Tipos: número DKL → número de Excel; cadena → texto (nunca fórmula); nulo o campo ausente → celda
vacía.

Nota: DKL no distingue fechas de números; una fecha `AAAAMMDD.HHMMSS` se escribe como número. Si
necesita una fecha de Excel, conviértala a texto (`ndatetime.tostr`) o use el número tal cual.

## xlsx.write_sheets(archivo, hojas)

`hojas` es un registro cuyos campos son listas (`@"Ventas*": lista`); cada campo es una hoja en el
orden del registro. Nombres de hoja: de 1 a 31 caracteres, sin `: \ / ? * [ ]`.

## xlsx.to_bytes(lista) → bytes

El mismo libro de `xlsx.write` en memoria (arreglo de bytes) para `@http_context<"response/binary*">`
o `file.wbytes`.

## xlsx.read(archivo, hoja) → lista

Lee el rango usado de la hoja (`""` = la primera). La primera fila del rango son los nombres de
campo (vacío → `columna_N`, repetido → `nombre_2`…). Las filas totalmente vacías se omiten.

| Celda | Valor DKL |
|---|---|
| número | número (15 dígitos significativos, como Excel: `0.1` y no `0.1000000000000000055`) |
| fecha y hora | número `AAAAMMDD.HHMMSS` |
| hora sola | número `0.HHMMSS` |
| lógico | `1` / `0` |
| texto | cadena |
| vacía | `""` |
| error (`#N/A`, `#DIV/0!`) | cadena con el error |
| fórmula | su valor; si el libro no lo trae guardado (lo generó otro programa), se recalcula |

## xlsx.sheets(archivo) → lista

Nombres de las hojas en orden.

## Errores

Prefijo `xlsx:`: archivo inexistente, archivo que no es un libro XLSX, hoja inexistente, nombre de
hoja inválido, lista nula o con elementos que no son registros.
