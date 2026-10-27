# html.dkh

Encabezado: `html.dkh` · DLL: `Devkron.Ext.Html.dll` (clase `Devkron.Ext.Html.Html`) · usa HtmlAgilityPack 1.13.1.

## html.parse(html, url_base) → registro

| Campo | Contenido |
|---|---|
| `url` | `url_base` tal como se recibió |
| `title` | texto de `<title>`, con entidades decodificadas |
| `description`, `keywords` | `content` de `<meta name="description">` / `keywords` (`""` si no hay) |
| `meta*` | registro con todas las `<meta>`: llave = `name`, `property` o `http-equiv` en minúsculas; `charset` si viene; la primera gana |
| `links*` | lista de URL absolutas únicas de los `<a href>`, en orden; resueltas contra `<base href>` o `url_base`; sin anclas `#`, `javascript:` ni `data:` |
| `text` | texto visible (ver `html.text`) |
| `content` | igual a `text` (nombre del original) |

Si `url_base` está vacía o no es absoluta, los enlaces relativos se omiten.

## html.from_url(url, encabezados) → registro

GET a `url` (solo http/https absolutas) y `html.parse` del resultado, con dos campos más: `status`
(código HTTP) y `final_url` (después de redirecciones, también usada como base de los enlaces).

- `encabezados`: registro o `@null`. Se envían solo en esta llamada. `timeout` (segundos) no se
  envía: fija el tiempo de espera (30 por omisión).
- Sin `User-Agent` se envía el de Chrome 131 que usaba el original; sin `Accept`, `text/html`.
- Sigue hasta 10 redirecciones; descomprime gzip/deflate/brotli; no guarda cookies.
- Juego de caracteres: el de `Content-Type`; si no viene, el de `<meta charset>`; si no, UTF-8.
- Respuesta que no es 2xx → error con el código; página de más de 20 MB → error.

## html.get(url, encabezados) → cadena

Lo mismo, pero devuelve el HTML sin interpretar.

## html.text(html) → cadena

Texto visible: omite `script`, `style`, `noscript`, `template`, `head`, `svg`, `iframe`, `object`
y comentarios; decodifica entidades. Cada bloque (`p`, `div`, `li`, `h1`…`h6`, `tr`, `br`…) va en
su línea; las celdas de una fila se separan con tabulador; espacios repetidos se juntan; sin
líneas vacías.

## html.select(html, xpath) → lista

Texto (como `html.text`) de cada nodo que coincide con la expresión XPath 1.0. Con `text()` o un
atributo (`//a/@href`) devuelve su valor. Sin coincidencias → lista vacía.

```dkl
ref nombres = html.select(h, "//li[@class='producto']/a")
```

## html.select_attr(html, xpath, atributo) → lista

Valor del atributo en cada nodo (`""` si no lo tiene): `html.select_attr(h, "//img", "src")`.

## html.select_html(html, xpath) → lista

HTML externo de cada nodo.

## Errores

Prefijo `html:`: URL no absoluta o con esquema distinto de http/https, encabezados que no son
registro o que no se pueden enviar, respuesta no 2xx, tiempo agotado, error de conexión, página
demasiado grande, XPath vacío o inválido.
