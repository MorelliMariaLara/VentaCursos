#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ ! -f .env && -f .env.example ]]; then cp .env.example .env; fi
if [[ ! -d node_modules/express ]]; then npm install; fi
npm run dev
