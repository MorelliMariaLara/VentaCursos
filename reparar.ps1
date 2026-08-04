# NEXA - reparacion simple
Set-Location -LiteralPath $PSScriptRoot
Write-Host "NEXA - instalacion"
Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
if (Test-Path "node_modules") { Remove-Item -Recurse -Force "node_modules" }
if (-not (Test-Path ".env") -and (Test-Path ".env.example")) { Copy-Item ".env.example" ".env" }
npm cache clean --force | Out-Null
npm install
if ($LASTEXITCODE -ne 0) {
  Write-Host "ERROR: npm install fallo. Reinstala Node.js LTS desde https://nodejs.org" -ForegroundColor Red
  exit 1
}
Write-Host "OK. Corre: npm run dev" -ForegroundColor Green
$run = Read-Host "Iniciar ahora? (S/N)"
if ($run -match '^[sS]') { npm run dev }
