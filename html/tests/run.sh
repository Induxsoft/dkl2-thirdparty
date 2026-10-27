#!/bin/bash
# Ejecuta los kits de prueba de html contra una distribución de Devkron 2 que ya tenga
# instalados Devkron.Ext.Html.dll, HtmlAgilityPack.dll y html.dkh junto a dkl.
# Levanta un servidor HTTP local (python3, 127.0.0.1) para html.from_url; no usa internet.
#   DKL=/ruta/dkl html/tests/run.sh          (ACTUALIZAR=1 regenera las salidas esperadas)
cd "$(dirname "$0")" || exit 2
DKL=${DKL:-dkl}
command -v "$DKL" >/dev/null 2>&1 || [ -x "$DKL" ] || { echo "No se encontró dkl ($DKL); use DKL=/ruta/dkl"; exit 2; }
command -v python3 >/dev/null || { echo "Se necesita python3 para el servidor de prueba"; exit 2; }
PUERTO=${PUERTO:-$(python3 -c 'import socket;s=socket.socket();s.bind(("127.0.0.1",0));print(s.getsockname()[1])')}
python3 -I servidor.py "$PUERTO" & SRV=$!
trap 'kill $SRV 2>/dev/null' EXIT
for i in $(seq 1 50); do python3 -c "import socket;socket.create_connection(('127.0.0.1',$PUERTO),1)" 2>/dev/null && break; sleep 0.1; done
export HTML_TEST_BASE="http://127.0.0.1:$PUERTO"
rm -rf out && mkdir -p out
fallas=0; total=0
for kit in *.dkl; do
    n=${kit%.dkl}; total=$((total+1))
    "$DKL" "$kit" -hide-log 2>&1 | sed -e "s#$PWD/##g" -e "s#$HTML_TEST_BASE#http://SERVIDOR#g" > "out/$n.txt"
    if [ -n "$ACTUALIZAR" ]; then cp "out/$n.txt" "$n.esperado"; echo "actualizado $n"; continue; fi
    if diff -u "$n.esperado" "out/$n.txt" > "out/$n.diff"; then echo "ok    $n"; else echo "FALLA $n (ver tests/out/$n.diff)"; fallas=$((fallas+1)); fi
done
echo "html: $((total-fallas))/$total kits correctos"
[ $fallas -eq 0 ]
