#!/bin/bash
# Ejecuta los kits de prueba de csv contra una distribución de Devkron 2 que ya tenga
# instalados Devkron.Ext.Csv.dll, CsvHelper.dll y csv.dkh junto a dkl.
#   DKL=/ruta/dkl csv/tests/run.sh          (ACTUALIZAR=1 regenera las salidas esperadas)
cd "$(dirname "$0")" || exit 2
DKL=${DKL:-dkl}
command -v "$DKL" >/dev/null 2>&1 || [ -x "$DKL" ] || { echo "No se encontró dkl ($DKL); use DKL=/ruta/dkl"; exit 2; }
rm -rf out && mkdir -p out
fallas=0; total=0
for kit in *.dkl; do
    n=${kit%.dkl}; total=$((total+1))
    # Quita la ruta absoluta de los mensajes de error para comparar
    "$DKL" "$kit" -hide-log 2>&1 | sed "s#$PWD/##g" > "out/$n.txt"
    if [ -n "$ACTUALIZAR" ]; then cp "out/$n.txt" "$n.esperado"; echo "actualizado $n"; continue; fi
    if diff -u "$n.esperado" "out/$n.txt" > "out/$n.diff"; then echo "ok    $n"; else echo "FALLA $n (ver tests/out/$n.diff)"; fallas=$((fallas+1)); fi
done
echo "csv: $((total-fallas))/$total kits correctos"
[ $fallas -eq 0 ]
