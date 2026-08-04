@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  SANTICAZA Capacitaciones
echo ================================

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: Instala .NET 8 SDK desde
  echo https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

if not exist ".env" if exist ".env.example" copy /Y ".env.example" ".env" >nul

echo Abriendo http://localhost:5000
start "" "http://localhost:5000"
dotnet run --project Nexa.Web
pause
