# NEXA - reparacion desde PowerShell
# Uso:
#   cd "C:\Users\Maria Lara\source\repos\VentaCursos"
#   Set-ExecutionPolicy -Scope Process Bypass
#   .\reparar.ps1

$ErrorActionPreference = "Continue"
Set-Location -LiteralPath $PSScriptRoot

Write-Host "================================" -ForegroundColor Cyan
Write-Host " NEXA - Reparacion PowerShell" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
  Write-Host "ERROR: Node.js no esta en el PATH. Instala https://nodejs.org" -ForegroundColor Red
  exit 1
}

Write-Host "Node:" (node -v)
Write-Host "npm :" (npm -v)
Write-Host ""

Write-Host "[1/5] Cerrando procesos node..." -ForegroundColor Yellow
Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "[2/5] Borrando node_modules..." -ForegroundColor Yellow
if (Test-Path "node_modules") {
  cmd /c "rmdir /s /q node_modules" 2>$null
  if (Test-Path "node_modules") {
    Remove-Item -LiteralPath "node_modules" -Recurse -Force -ErrorAction SilentlyContinue
  }
}
Get-ChildItem -Force -Directory -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -like ".*" -and $_.Name -notin @(".git", ".next", ".vscode", ".vs", ".cursor") } |
  ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
  }

if (Test-Path "node_modules") {
  Write-Host "ERROR: no pude borrar node_modules. Reinicia la PC y corre de nuevo." -ForegroundColor Red
  exit 1
}
Write-Host "OK: node_modules eliminado"

Write-Host "[3/5] Limpiando cache npm..." -ForegroundColor Yellow
npm cache clean --force | Out-Null

Write-Host "[4/5] Instalando dependencias (puede tardar varios minutos)..." -ForegroundColor Yellow
npm install --legacy-peer-deps --prefer-online --no-audit --no-fund
if ($LASTEXITCODE -ne 0) {
  Write-Host "Primer intento fallo. Borrando package-lock y reintentando..." -ForegroundColor Yellow
  Remove-Item -Force "package-lock.json" -ErrorAction SilentlyContinue
  if (Test-Path "node_modules") { cmd /c "rmdir /s /q node_modules" 2>$null }
  npm install --legacy-peer-deps --prefer-online --no-audit --no-fund
  if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: npm install fallo." -ForegroundColor Red
    Write-Host "Ejecuta como Admin y excluye la carpeta del antivirus:" -ForegroundColor Yellow
    Write-Host "  Add-MpPreference -ExclusionPath `"$PWD`""
    exit 1
  }
}

Write-Host "[5/5] Verificando next..." -ForegroundColor Yellow
$nextJs = Join-Path $PWD "node_modules\next\dist\bin\next"
$nextCmd = Join-Path $PWD "node_modules\.bin\next.cmd"
if (-not (Test-Path $nextJs)) {
  Write-Host "ERROR: next no quedo instalado en node_modules." -ForegroundColor Red
  exit 1
}
if (-not (Test-Path $nextCmd)) {
  Write-Host "AVISO: falta node_modules\.bin\next.cmd - npm run dev usara node directo." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Instalacion OK." -ForegroundColor Green
Write-Host "Ahora corre:" -ForegroundColor Green
Write-Host "  npm run dev"
Write-Host ""
$run = Read-Host "Queres iniciar ahora? (S/N)"
if ($run -match '^[sS]') {
  npm run dev
}
