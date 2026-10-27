# dkl2-markdown 1.0.0

Markdown a HTML para Devkron 2: `markdown.dkh`, envoltura directa de
[Markdig](https://github.com/xoofx/markdig) 1.5.0 (BSD-2-Clause). No lleva DLL propia: el
`Markdig.dll` (lib/net10.0 del paquete NuGet) se incluye en `lib/` y en el zip.

Sustituye a `markdig.dkl` de `dkl-web` (Markdig 0.38.0) y conserva su función `md2html`.

## Instalación

Descomprima `dkl2-markdown-1.0.0.zip` en la carpeta de `dkl` (trae `Markdig.dll`, `markdown.dkh`
y las licencias) e incluya `markdown.dkh`. Si la carpeta ya tiene el `Markdig.dll` 0.38.0 de
`dkl-web`, se reemplaza: `markdig.dkl` sigue funcionando con la 1.5.0 (mismas firmas; verificado).
No incluya `markdig.dkl` y `markdown.dkh` en el mismo programa (los dos definen `md2html`).

## Funciones

| Función | Devuelve |
|---|---|
| `md.html(texto)` | HTML con las extensiones avanzadas de Markdig; el HTML crudo del texto pasa tal cual |
| `md.html_safe(texto)` | igual, pero el HTML crudo se escapa como texto (`DisableHtml`) |
| `md2html(texto)` | igual que `md.html` (compatibilidad con `markdig.dkl`) |

Extensiones avanzadas (`UseAdvancedExtensions`): tablas, listas de tareas, notas al pie,
abreviaturas, listas de definición, citas y figuras, énfasis extra (`~~tachado~~`), enlaces
automáticos, identificadores en los encabezados, atributos genéricos `{.clase}`, matemáticas y
diagramas.

```dkl
#include "functions.dkh"
#include "markdown.dkh"

cuerpo = file.rtext("articulo.md")
@http_context<"response/text">: md.html(cuerpo)

// Comentarios de usuarios: sin HTML crudo
html_comentario = md.html_safe(comentario)
```

Detalle en [docs/markdown.md](docs/markdown.md).

## Comparación con `markdig.dkl` (Markdig 0.38.0)

`md2html` se ejecutó con el `markdig.dkl` + `Markdig.dll` 0.38.0 originales de `dkl-web` y con
`markdown.dkh` + Markdig 1.5.0 sobre un documento con encabezados con acentos, negritas, enlaces,
tabla alineada, lista de tareas, HTML crudo, nota al pie, cita, bloque de código, lista numerada,
tachado y enlace automático: salida idéntica (42 líneas).

## Pruebas

```
DKL=/ruta/dkl tests/run.sh
```

Kit `markdown.dkl`: `md.html`, `md.html_safe` (escapa `<div>` y `<script>`), `md2html` igual a
`md.html`, texto vacío. Resultado actual: 1/1.

## Hallazgos de seguridad

- **`md.html_safe` no filtra URL peligrosas** (verificado): `markdown.dkh` línea 23 solo desactiva
  el HTML crudo; los enlaces e imágenes de Markdown conservan su URL, así que
  `[clic](javascript:alert(1))` produce `<a href="javascript:alert(1)">` (también `data:`). Con
  texto de usuarios, `md.html_safe` evita etiquetas pero no XSS por enlace; queda registrado en el
  kit (`tests/markdown.esperado`, última línea).
- **`md.html` deja pasar HTML crudo** (verificado, por diseño): incluido `<script>`; igual que
  `md2html` del original (`dkl-web/src/markdig.dkl` línea 20). Úselo solo con texto confiable.
