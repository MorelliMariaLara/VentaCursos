@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  NEXA - inicio local
echo ================================

where node >nul 2>nul
if errorlevel 1 (
  echo ERROR: Instala Node.js LTS desde https://nodejs.org
  echo No hace falta npm install.
  pause
  exit /b 1
)

if not exist ".env" if exist ".env.example" copy /Y ".env.example" ".env" >nul

echo Abriendo http://localhost:3000
start "" "http://localhost:3000"
node server\index.js
pause
