#!/bin/bash
# Prueba los zip de dist/ instalados sobre una COPIA de una distribución de Devkron 2.
#   DKL_DIST=/ruta/a/la/carpeta/de/dkl build/test-all.sh     (antes: build/pack.sh)
# No modifica la distribución original: la copia a build/out/dkl, descomprime ahí cada
# dist/dkl2-*.zip y corre <biblioteca>/tests/run.sh con esa copia.
set -u
RAIZ=$(cd "$(dirname "$0")/.." && pwd)
DKL_DIST=${DKL_DIST:-}
[ -n "$DKL_DIST" ] && [ -x "$DKL_DIST/dkl" ] || { echo "Indique la carpeta de la distribución: DKL_DIST=/ruta (debe contener dkl)"; exit 2; }
COPIA="$RAIZ/build/out/dkl"
rm -rf "$COPIA"; mkdir -p "$(dirname "$COPIA")"; cp -r "$DKL_DIST" "$COPIA"
fallas=0
for lib in csv office image html markdown; do
    zip=$(ls "$RAIZ"/dist/dkl2-$lib-*.zip 2>/dev/null | sort -V | tail -1)
    if [ -z "$zip" ]; then echo "falta dist/dkl2-$lib-*.zip (ejecute build/pack.sh)"; fallas=$((fallas+1)); continue; fi
    unzip -q -o "$zip" -d "$COPIA"
    echo "== $lib ($(basename "$zip"))"
    DKL="$COPIA/dkl" "$RAIZ/$lib/tests/run.sh" || fallas=$((fallas+1))
done
echo
[ $fallas -eq 0 ] && echo "Todas las bibliotecas pasaron." || echo "Bibliotecas con fallas: $fallas"
[ $fallas -eq 0 ]
