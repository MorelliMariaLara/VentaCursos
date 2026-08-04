#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

echo "================================"
echo " NEXA Web — Proyecto de inicio"
echo "================================"

if ! command -v node >/dev/null 2>&1; then
  echo "ERROR: instalá Node.js LTS desde https://nodejs.org"
  exit 1
fi

bash scripts/setup.sh
echo ""
echo "Abriendo http://localhost:3000"
echo "Alumno: demo@nexa.academy / demo1234"
echo "Admin:  admin@nexa.academy / admin1234"
echo ""

if command -v xdg-open >/dev/null 2>&1; then
  (sleep 2 && xdg-open "http://localhost:3000") >/dev/null 2>&1 &
elif command -v open >/dev/null 2>&1; then
  (sleep 2 && open "http://localhost:3000") >/dev/null 2>&1 &
fi

npm run dev
