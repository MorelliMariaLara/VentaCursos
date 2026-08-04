# NEXA - sin dependencias npm
Set-Location -LiteralPath $PSScriptRoot
Write-Host "NEXA no necesita npm install." -ForegroundColor Green
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
  Write-Host "Instala Node.js LTS: https://nodejs.org" -ForegroundColor Red
  exit 1
}
if (-not (Test-Path ".env") -and (Test-Path ".env.example")) { Copy-Item ".env.example" ".env" }
if (Test-Path "node_modules") {
  Write-Host "Borrando node_modules viejo (ya no se usa)..."
  Remove-Item -Recurse -Force "node_modules" -ErrorAction SilentlyContinue
}
Write-Host "Iniciando..."
node server/index.js
