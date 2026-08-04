@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  NEXA - inicio simple
echo ================================

where node >nul 2>nul
if errorlevel 1 (
  echo ERROR: Instala Node.js LTS desde https://nodejs.org
  pause
  exit /b 1
)

if not exist ".env" if exist ".env.example" copy /Y ".env.example" ".env" >nul

if not exist "node_modules\express" (
  echo Instalando dependencias...
  call npm install
  if errorlevel 1 (
    echo ERROR: npm install fallo.
    pause
    exit /b 1
  )
)

echo Abriendo http://localhost:3000
start "" "http://localhost:3000"
call npm run dev
pause
