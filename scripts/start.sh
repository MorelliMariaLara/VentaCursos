#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

MODE="${1:-dev}"

case "$MODE" in
  setup)
    bash scripts/setup.sh
    ;;
  dev)
    bash scripts/setup.sh
    echo "==> Iniciando NEXA en modo desarrollo"
    npm run dev
    ;;
  build)
    bash scripts/setup.sh
    npm run build
    ;;
  start)
    if [[ ! -d .next ]]; then
      npm run build
    fi
    echo "==> Iniciando NEXA (producción Node)"
    npm run start
    ;;
  docker|up)
    if [[ ! -f .env ]]; then
      cp .env.example .env
    fi
    echo "==> Levantando solución NEXA con Docker Compose"
    docker compose up --build -d
    echo "    App: http://localhost:${PORT:-3000}"
    ;;
  down)
    docker compose down
    ;;
  logs)
    docker compose logs -f nexa
    ;;
  *)
    echo "Uso: ./scripts/start.sh [setup|dev|build|start|docker|down|logs]"
    exit 1
    ;;
esac
