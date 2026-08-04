#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "==> NEXA · setup de la solución"

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "    Creado .env desde .env.example"
else
  echo "    .env ya existe (sin cambios)"
fi

if [[ ! -f .env.local ]]; then
  cp .env.example .env.local
  echo "    Creado .env.local para desarrollo"
fi

if [[ ! -d node_modules ]]; then
  echo "    Instalando dependencias…"
  npm install
else
  echo "    Dependencias OK"
fi

mkdir -p data content/videos

if [[ ! -f content/videos/lesson-a.mp4 ]]; then
  if command -v ffmpeg >/dev/null 2>&1; then
    echo "    Generando videos demo…"
    bash scripts/generate-demo-videos.sh
  else
    echo "    Aviso: no hay ffmpeg; los videos demo deben existir en content/videos"
  fi
fi

echo "    Setup listo."
echo ""
echo "Siguiente:"
echo "  npm run dev          # desarrollo"
echo "  npm run solution:up  # Docker (producción local)"
echo ""
echo "Cuentas:"
echo "  Alumno  demo@nexa.academy / demo1234"
echo "  Admin   admin@nexa.academy / admin1234"
