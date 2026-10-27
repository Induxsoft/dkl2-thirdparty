#!/bin/bash
# Genera un zip por biblioteca en dist/: dkl2-<biblioteca>-<versión>.zip
# Cada zip trae la DLL propia, sus dependencias de terceros (con los nativos de Linux x64 y Windows x64
# cuando los hay), los .dkh y los avisos de licencia. Se instala descomprimiéndolo junto a dkl.
#
#   build/pack.sh                 todas las bibliotecas
#   build/pack.sh csv image       solo las indicadas
#
# Compila con un solo nodo de MSBuild y sin servidor de compilación (equipos chicos o compartidos).
set -euo pipefail
RAIZ=$(cd "$(dirname "$0")/.." && pwd)
DIST="$RAIZ/dist"
TMP="$RAIZ/build/out"
LIBS=("$@"); [ ${#LIBS[@]} -eq 0 ] && LIBS=(csv office image html markdown)
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1
mkdir -p "$DIST"

compilar() { # $1 = carpeta de la biblioteca, $2 = proyecto
    dotnet build "$RAIZ/$1/src/$2" -c Release -v q -nologo -m:1 -nodeReuse:false -p:UseSharedCompilation=false >&2
    echo "$RAIZ/$1/src/bin/Release/net10.0"
}

empacar() { # $1 = biblioteca; el contenido ya está en $TMP/$1
    local lib=$1 ver
    ver=$(tr -d ' \r\n' < "$RAIZ/$lib/VERSION")
    cp "$RAIZ/LICENSE" "$TMP/$lib/dkl2-$lib.LICENSE.txt"
    cp "$RAIZ/$lib/LICENSE-THIRDPARTY" "$TMP/$lib/dkl2-$lib.LICENSE-THIRDPARTY.txt"
    local zip="$DIST/dkl2-$lib-$ver.zip"
    rm -f "$zip"
    (cd "$TMP/$lib" && zip -q -X -r "$zip" .)
    printf '%-32s %8s KB\n' "$(basename "$zip")" "$(( $(stat -c %s "$zip") / 1024 ))"
}

for lib in "${LIBS[@]}"; do
    rm -rf "$TMP/$lib"; mkdir -p "$TMP/$lib"; o="$TMP/$lib"
    case "$lib" in
        csv)
            b=$(compilar csv Devkron.Ext.Csv.csproj)
            cp "$b/Devkron.Ext.Csv.dll" "$b/CsvHelper.dll" "$RAIZ/csv/csv.dkh" "$o/" ;;
        office)
            b=$(compilar office Devkron.Ext.Office.csproj)
            cp "$b"/*.dll "$RAIZ/office/xlsx.dkh" "$RAIZ/office/doc.dkh" "$o/" ;;
        image)
            b=$(compilar image Devkron.Ext.Image.csproj)
            cp "$b/Devkron.Ext.Image.dll" "$b/Magick.NET-Q16-HDRI-AnyCPU.dll" "$b/Magick.NET.Core.dll" "$RAIZ/image/image.dkh" "$o/"
            # Nativos de ImageMagick: Linux x64 (glibc) y Windows x64, junto a la DLL (Devkron.Ext.Image los busca ahí)
            cp "$b/runtimes/linux-x64/native/Magick.Native-Q16-HDRI-x64.dll.so" "$o/"
            cp "$b/runtimes/win-x64/native/Magick.Native-Q16-HDRI-x64.dll" "$o/"
            cp "$RAIZ/image/NOTICE-Magick.NET.txt" "$o/dkl2-image.NOTICE-Magick.NET.txt" ;;
        html)
            b=$(compilar html Devkron.Ext.Html.csproj)
            cp "$b/Devkron.Ext.Html.dll" "$b/HtmlAgilityPack.dll" "$RAIZ/html/html.dkh" "$o/" ;;
        markdown)
            esperado=4dab6e49268afc296e955658f7132eb172049d7da0837b170ba3aaedc8b7c8d9
            real=$(sha256sum "$RAIZ/markdown/lib/Markdig.dll" | cut -d' ' -f1)
            [ "$real" = "$esperado" ] || { echo "markdown/lib/Markdig.dll no es el Markdig 1.5.0 esperado (sha256 $real)" >&2; exit 1; }
            cp "$RAIZ/markdown/lib/Markdig.dll" "$RAIZ/markdown/markdown.dkh" "$o/" ;;
        *) echo "Biblioteca desconocida: $lib (csv, office, image, html, markdown)" >&2; exit 2 ;;
    esac
    empacar "$lib"
done
