# image.dkh

Encabezado: `image.dkh` · DLL: `Devkron.Ext.Image.dll` (clase `Devkron.Ext.Image.Image`) · usa
Magick.NET 15.0.0 Q16-HDRI (ImageMagick 7.1.2-33). OpenCL desactivado, como el original.

El nativo (`Magick.Native-Q16-HDRI-x64.dll.so` en Linux, `.dll` en Windows) se busca en la misma
carpeta que `Devkron.Ext.Image.dll`.

## image.size(archivo) → registro o nulo

`{width, height, size_kb}`: dimensiones en píxeles (se leen sin decodificar la imagen completa) y
tamaño del archivo en KB con dos decimales. Nulo si el archivo no existe o no es una imagen; nunca
lanza error (compatibilidad).

## image.resize(archivo, ancho, alto) → 1/0

Redimensiona y sobrescribe el archivo en su formato.

- `ancho` y `alto` distintos de 0: tamaño exacto (deforma si la proporción no coincide).
- uno de los dos en 0: ese lado se calcula para conservar la proporción.
- Devuelve `0` (sin error) si no se pudo, como el original.

## image.compress(archivo, optimo, con_perdida) → 1/0

Optimiza el archivo en su lugar con `ImageOptimizer` de Magick.NET (JPEG, PNG, GIF, ICO; los demás
formatos se ignoran y devuelven `0`). `optimo` = compresión más lenta y mejor; `con_perdida` =
permite reducir calidad (JPEG). `1` si el archivo quedó más chico. Error solo si el archivo no
existe o está dañado.

## image.thumbnail(origen, destino, lado_mayor) → 1

Miniatura proporcional cuyo lado mayor no pasa de `lado_mayor` píxeles; aplica la orientación EXIF
y quita metadatos. Si la imagen ya es más chica, no se agranda (solo se convierte al formato del
destino). El formato sale de la extensión del destino.

## image.convert(origen, destino) → 1

Cambia de formato según la extensión del destino: `jpg`/`jpeg`, `png`, `gif`, `webp`, `bmp`,
`tif`/`tiff`, `ico`, `avif`, `heic`, `pdf`. Al pasar a JPEG o BMP, la transparencia se rellena de
blanco. La carpeta de destino debe existir.

## image.newer(archivo1, archivo2) → número

`1` si `archivo1` es más reciente, `2` si lo es `archivo2`, `0` si tienen la misma fecha, `-1` si no
existe `archivo1`, `-2` si no existe `archivo2`.

## Errores

Prefijo `image:` en `thumbnail`, `convert` y `compress`: archivo inexistente, destino sin extensión
o con formato no admitido, carpeta de destino inexistente, lado de miniatura menor que 1, y el
motivo que da ImageMagick si no puede decodificar.
