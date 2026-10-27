# markdown.dkh

Encabezado: `markdown.dkh` · Biblioteca: `Markdig.dll` 1.5.0 (sin DLL propia).

Al incluirse, `markdown.dkh` carga `Markdig.dll` desde la carpeta de `dkl`, mapea
`Markdig.Markdown.ToHtml`, `MarkdownExtensions.UseAdvancedExtensions`,
`MarkdownExtensions.DisableHtml` y `MarkdownPipelineBuilder.Build`, y arma una sola vez dos canales
(`@dkl2_md_pipeline` y `@dkl2_md_pipeline_safe`) que se reutilizan en cada llamada.

## md.html(texto) → cadena

Convierte Markdown (CommonMark + extensiones avanzadas) a HTML. El HTML crudo del texto
(`<div>`, `<script>`, …) se copia tal cual. Texto vacío → `""`.

## md.html_safe(texto) → cadena

Igual que `md.html` con `DisableHtml`: los bloques y etiquetas HTML del texto salen escapados
(`&lt;script&gt;`). Las URL de enlaces e imágenes **no** se filtran (ver hallazgos en el README):
si el texto viene de usuarios, valide o limpie los `href`/`src` aparte.

## md2html(texto) → cadena

Alias de `md.html` con el nombre de `markdig.dkl` de `dkl-web`.

## Nombres internos

`md._to_html`, `md._use_advanced`, `md._disable_html`, `md._build` son los métodos mapeados; no
forman parte de la interfaz y pueden cambiar.
