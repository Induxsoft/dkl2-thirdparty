# dkl2-html 1.0.0

Lectura de páginas HTML para Devkron 2: `html.dkh` + `Devkron.Ext.Html.dll`, sobre
[HtmlAgilityPack](https://html-agility-pack.net) 1.13.1 (MIT).

Parte de `HtmlScraper.Document` (`internal-devs/spiderbot/src/cs/spider-bot/htmlscraper/Document.cs`).
`html.encode`, `html.decode` y `html.attrs` no están aquí: van con el motor (`text.dkh`).

## Instalación

Descomprima `dkl2-html-1.0.0.zip` en la carpeta de `dkl` (trae `Devkron.Ext.Html.dll`,
`HtmlAgilityPack.dll`, `html.dkh` y las licencias) e incluya `html.dkh`.

## Funciones

| Función | Devuelve |
|---|---|
| `html.parse(html, url_base)` | registro `{url, title, description, keywords, meta, links, text, content}` |
| `html.from_url(url, encabezados)` | descarga con GET y aplica `html.parse`; agrega `status` y `final_url` |
| `html.get(url, encabezados)` | solo el HTML descargado |
| `html.text(html)` | texto visible (sin `script`, `style`, `head`, `noscript`, comentarios) |
| `html.select(html, xpath)` | lista con el texto de cada nodo que coincide |
| `html.select_attr(html, xpath, atributo)` | lista con el valor del atributo en cada nodo |
| `html.select_html(html, xpath)` | lista con el HTML de cada nodo |

`encabezados` es un registro (`@null` o `{}` si no hay) y vale **solo para esa llamada**. El
campo especial `timeout` fija los segundos de espera (30 por omisión). Si no se envían, se agregan
`User-Agent` (el de Chrome que usaba el original) y `Accept: text/html`.

```dkl
#include "functions.dkh"
#include "serialize.dkh"
#include "html.dkh"

new h
{
    @"Authorization": "Bearer " + token
    @"timeout": 10
}
ref p = html.from_url("https://www.ejemplo.com/catalogo/", h)
do print(p.title + " (" + str(p.status) + ")\n")
go foreach.lstr(index:i, value:u) with(@@(p, "&links"))
{
    do print(u + "\n")
}
ref precios = html.select(html.get("https://www.ejemplo.com/catalogo/", @null), "//span[@class='precio']")
```

Detalle en [docs/html.md](docs/html.md).

## Diferencias con `HtmlScraper.Document`

Comparado con `GetDataFromHtml` del original sobre `tests/www/index.html`:

| | Original | Ahora |
|---|---|---|
| encabezados de `from_url` | estáticos (`SetHeaders`): se quedaban para todas las llamadas siguientes del proceso, incluso de otras peticiones FastCGI | por llamada |
| cliente HTTP | `Devkron.WebClient.HTTP.Request` | `HttpClient` propio compartido, con tiempo de espera, redirecciones, descompresión y juego de caracteres del `Content-Type` o `<meta charset>` |
| `title` | `Ferretería &amp;amp; Más` (entidades sin decodificar) | `Ferretería & Más` |
| `links` | incluía `javascript:alert(1)` y dejaba `&amp;amp;` en las URL | sin `javascript:`/`data:` ni anclas `#`, entidades decodificadas, resueltos contra `<base href>` |
| `text` (`content`) | una línea por nodo de texto (`Precios` / `bajos` / `todo el año`), incluía el `<title>` y `<noscript>` | una línea por bloque (`Precios bajos todo el año`), filas de tabla con celdas separadas por tabulador, sin `head` ni `noscript` |
| `meta` | no existía | registro con todas las `meta` (`name`, `property` como `og:title`, `http-equiv`, `charset`) |

`description`, `keywords` y `url` dan lo mismo. Se conserva el campo `content` (igual a `text`).

## Pruebas

```
DKL=/ruta/dkl tests/run.sh
```

Levanta `tests/servidor.py` (python3) en `127.0.0.1` en un puerto libre: sirve `tests/www/`,
`/eco` (devuelve los encabezados recibidos), `/redir` (302) y `/lento` (5 s). No usa internet.
Kits: `parse.dkl`, `from_url.dkl` (encabezados por llamada que no se arrastran a la siguiente,
redirección, Latin-1) y `errores.dkl` (404, URL relativa, esquema `file:`, tiempo agotado,
puerto cerrado, XPath inválido). Resultado actual: 3/3.

## Hallazgos de seguridad

- **Peticiones a cualquier host (SSRF)** (inferido): `html.from_url`/`html.get` aceptan cualquier
  URL http/https (`src/Html.cs` línea 140) y siguen redirecciones (línea 32), incluidas direcciones
  internas (`127.0.0.1`, `169.254.169.254`, la red local). Si la URL la da el usuario, puede
  usarse para consultar servicios internos del servidor. El original tampoco filtraba.
- **Encabezados compartidos entre peticiones** (verificado en el original, corregido aquí):
  `Document.cs` líneas 11 y 23 guardaban los encabezados (p. ej. `Authorization`) en una
  propiedad estática; en FastCGI una petición podía enviar el token de otra.
- **HTML muy anidado** (inferido, mitigado): `src/Html.cs` línea 122 limita el anidamiento a
  2000 niveles para que un HTML malicioso no desborde la pila del recorrido; las páginas descargadas
  se cortan a 20 MB.
- **Enlaces devueltos sin validar** (inferido): `links` incluye `mailto:`, `tel:`, `ftp:` y
  cualquier http/https (`src/Html.cs` línea 296); si se vuelven a mostrar en una página deben
  escaparse con `html.encode`.
