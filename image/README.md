# dkl2-image 1.0.0

Imágenes para Devkron 2: `image.dkh` + `Devkron.Ext.Image.dll`, sobre
[Magick.NET](https://github.com/dlemstra/Magick.NET) 15.0.0 (Apache-2.0), variante
`Q16-HDRI-AnyCPU`, con ImageMagick 7.1.2-33 (ImageMagick License).

Sustituye a `magick.dkh` + `Devkron.Magick.dll` (Magick.NET 13.3.0): mismos nombres y
comportamiento de `image.size`, `image.resize`, `image.compress` e `image.newer`, y agrega
`image.thumbnail` e `image.convert`. Parte de `devkron/API/magick/ImageTools.cs`.

## Instalación

Descomprima `dkl2-image-1.0.0.zip` en la carpeta de `dkl`. Trae `Devkron.Ext.Image.dll`,
`Magick.NET-Q16-HDRI-AnyCPU.dll`, `Magick.NET.Core.dll`, los nativos
`Magick.Native-Q16-HDRI-x64.dll.so` (Linux x64, glibc) y `Magick.Native-Q16-HDRI-x64.dll`
(Windows x64), `image.dkh` y las licencias (`dkl2-image.NOTICE-Magick.NET.txt` tiene las de todas
las bibliotecas que incluye el nativo). El zip pesa ~27 MB por los dos nativos.

Si se instala sobre una carpeta que ya tiene la `Devkron.Magick.dll` anterior, `image.dkh`
reemplaza a `magick.dkh`; no incluya los dos en el mismo programa (definen los mismos nombres).
Los `Magick.NET-*.dll` nuevos reemplazan a los de la versión 13.3.0.

## Funciones

| Función | Devuelve |
|---|---|
| `image.size(archivo)` | registro `{width, height, size_kb}`; nulo si no es imagen o no existe |
| `image.resize(archivo, ancho, alto)` | `1`/`0`; sobrescribe; con `0` en un lado conserva la proporción, con los dos distintos de 0 deforma a ese tamaño |
| `image.compress(archivo, optimo, con_perdida)` | `1` si redujo el archivo, `0` si no (formato no admitido o ya óptimo); JPEG, PNG, GIF, ICO |
| `image.thumbnail(origen, destino, lado_mayor)` | `1`; miniatura proporcional con lado mayor ≤ `lado_mayor`, orientación EXIF aplicada, sin metadatos, no agranda |
| `image.convert(origen, destino)` | `1`; formato según la extensión del destino |
| `image.newer(archivo1, archivo2)` | `1`/`2` el más reciente, `0` iguales, `-1`/`-2` si falta el primero/segundo |

```dkl
#include "functions.dkh"
#include "image.dkh"

ref t = image.size("foto.jpg")
if not(isnull(t))
{
    do print(str(t.width) + "x" + str(t.height) + " " + str(t.size_kb) + " KB\n")
}
do image.thumbnail("foto.jpg", "foto_th.webp", 200)
do image.convert("logo.png", "logo.jpg")
if image.resize("copia.jpg", 1024, 0) { do image.compress("copia.jpg", @true, @false) }
```

Detalle en [docs/image.md](docs/image.md).

## Comparación con `Devkron.Magick.dll`

Se corrieron las mismas operaciones con la DLL original publicada (`pub/v12/src/Devkron.Magick.dll`
con Magick.NET 13.3.0) y con la nueva, sobre las imágenes de `tests/data` (generadas con
ImageMagick: JPEG 800×600, PNG con transparencia, GIF 64×64, PNG 1000×250, un `.jpg` falso):

- `image.size`: misma salida. La DLL publicada devuelve además `size_kb` (no está en el
  `ImageTools.cs` del repo); se conserva.
- `image.resize` (400×0, 0×50, 100×100 deformando, archivo falso → 0): mismas dimensiones;
  los píxeles resultantes son idénticos (`compare -metric RMSE` = 0) en JPEG y PNG. El PNG con
  transparencia queda 16 bytes más chico (1346 contra 1362, `size_kb` 1.31 contra 1.33) por el
  codificador de la versión nueva.
- `image.compress`: mismo resultado (`1` en el JPEG, `0` en el GIF ya óptimo) y mismo tamaño final
  del JPEG (3896 bytes).
- `image.newer`: el original devolvía `1` cuando faltaba el segundo archivo (revisaba dos veces el
  primero); ahora devuelve `-2`, como decía su código.
- Errores: `size` y `resize` siguen sin lanzar error (nulo / `0`), igual que antes; `compress` con
  un archivo inexistente ahora da un error en español en vez de la excepción de Magick.NET.

## Pruebas

```
DKL=/ruta/dkl tests/run.sh
```

Kits: `image.dkl` (todas las funciones sobre una copia de `tests/data` en `tests/out/w`) y
`errores.dkl`. Resultado actual: 2/2 en Linux x64. El nativo de Windows x64 se empaqueta pero no
se probó (no hay Windows en el equipo de pruebas).

## Hallazgos de seguridad

- **Decodificadores de ImageMagick con archivos de usuarios** (inferido): `src/Image.cs` líneas 47,
  65, 107 y 131 abren cualquier formato que ImageMagick reconozca por su contenido, no por la
  extensión (SVG, PS/PDF vía Ghostscript si está instalado, MVG, MSL, etc.), sin política de
  recursos (`ResourceLimits`) ni lista de formatos permitidos. Con imágenes subidas por usuarios
  esto expone a fallas conocidas de esos decodificadores (p. ej. lectura de archivos locales vía
  SVG/MSL) y a imágenes "bomba" de dimensiones enormes que agotan memoria. El original
  (`ImageTools.cs` líneas 17 y 73) hacía lo mismo.
- **Escritura en rutas arbitrarias** (inferido): `image.convert` y `image.thumbnail` escriben en el
  destino que reciben (`src/Image.cs` línea 168); si la ruta viene del cliente puede sobrescribir
  archivos del sitio. Igual que `image.resize` del original.
