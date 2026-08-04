# Reescribe MP_PUBLIC_KEY / MP_ACCESS_TOKEN con el par de Pruebas correcto.
# Uso (PowerShell, desde la raíz del repo):
#   .\scripts\aplicar-credenciales-mp-prueba.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pk = "TEST-de2c8c3d-972c-4a5b-a05c-22745894b73a"
$tk = "TEST-2564533232408086-080413-6bb40d3c790d8550063469c4e6000620-706865166"

function Set-MpKeys([string]$path) {
    if (-not (Test-Path $path)) {
        Write-Host "No existe: $path (se omite)"
        return
    }
    $lines = Get-Content $path | ForEach-Object {
        if ($_ -match '^\s*MP_PUBLIC_KEY\s*=') { "MP_PUBLIC_KEY=$pk" }
        elseif ($_ -match '^\s*MP_ACCESS_TOKEN\s*=') { "MP_ACCESS_TOKEN=$tk" }
        else { $_ }
    }
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Host "OK → $path"
}

Set-MpKeys (Join-Path $root ".env")
Set-MpKeys (Join-Path $root "Nexa.Web\.env")

# Limpia variables de entorno de la sesión actual si estaban mal
$env:MP_PUBLIC_KEY = $pk
$env:MP_ACCESS_TOKEN = $tk

Write-Host ""
Write-Host "Listo. Cerrá la app y ejecutá:"
Write-Host "  dotnet run --project Nexa.Web"
Write-Host "En consola debe verse: PK=TEST-de2c8c… TK=TEST-2564…"
