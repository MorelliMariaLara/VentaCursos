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

Write-Host "[1/6] Cerrando procesos node..." -ForegroundColor Yellow
Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "[2/6] Borrando node_modules del proyecto..." -ForegroundColor Yellow
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

Write-Host "[3/6] Borrando cache npm corrupta (arregla Yallist)..." -ForegroundColor Yellow
$cacheDirs = @(
  (Join-Path $env:LOCALAPPDATA "npm-cache"),
  (Join-Path $env:APPDATA "npm-cache")
)
foreach ($dir in $cacheDirs) {
  if (Test-Path $dir) {
    Write-Host "  Borrando $dir"
    Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
  }
}
npm cache clean --force 2>$null | Out-Null

Write-Host "[4/6] Reparando npm global (10.9.2)..." -ForegroundColor Yellow
npm install -g npm@10.9.2
if ($LASTEXITCODE -ne 0) {
  Write-Host ""
  Write-Host "No se pudo reparar npm desde aca." -ForegroundColor Red
  Write-Host "Reinstala Node.js LTS desde https://nodejs.org" -ForegroundColor Yellow
  Write-Host "Marca 'Add to PATH', reinicia PowerShell, y corre .\reparar.ps1 de nuevo." -ForegroundColor Yellow
  exit 1
}
Write-Host "npm ahora:" (npm -v)

Write-Host "[5/6] Instalando dependencias del proyecto..." -ForegroundColor Yellow
npm install --legacy-peer-deps --prefer-online --no-audit --no-fund
if ($LASTEXITCODE -ne 0) {
  Write-Host "Primer intento fallo. Reintentando sin package-lock..." -ForegroundColor Yellow
  Remove-Item -Force "package-lock.json" -ErrorAction SilentlyContinue
  if (Test-Path "node_modules") { cmd /c "rmdir /s /q node_modules" 2>$null }
  npm install --legacy-peer-deps --prefer-online --no-audit --no-fund
  if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: npm install sigue fallando." -ForegroundColor Red
    Write-Host "1) Reinstala Node.js LTS: https://nodejs.org" -ForegroundColor Yellow
    Write-Host "2) Abri PowerShell como Administrador y corre:" -ForegroundColor Yellow
    Write-Host "   Add-MpPreference -ExclusionPath `"$PWD`""
    Write-Host "3) Volve a esta carpeta y corre .\reparar.ps1" -ForegroundColor Yellow
    exit 1
  }
}

Write-Host "[6/6] Verificando next..." -ForegroundColor Yellow
$nextJs = Join-Path $PWD "node_modules\next\dist\bin\next"
if (-not (Test-Path $nextJs)) {
  Write-Host "ERROR: next no quedo instalado en node_modules." -ForegroundColor Red
  exit 1
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
