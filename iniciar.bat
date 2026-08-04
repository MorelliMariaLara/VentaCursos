@echo off
setlocal
cd /d "%~dp0"

echo ================================
echo  NEXA Web - Proyecto de inicio
echo ================================
echo.

where node >nul 2>nul
if errorlevel 1 (
  echo ERROR: Necesitas instalar Node.js LTS desde https://nodejs.org
  pause
  exit /b 1
)

if not exist ".env.local" (
  copy /Y ".env.example" ".env.local" >nul
  echo Creado .env.local
)

if not exist "node_modules" (
  echo Instalando dependencias...
  call npm install
  if errorlevel 1 (
    echo Fallo npm install
    pause
    exit /b 1
  )
)

echo.
echo Abriendo http://localhost:3000
echo Usuario alumno: demo@nexa.academy / demo1234
echo Usuario admin:  admin@nexa.academy / admin1234
echo.
start "" "http://localhost:3000"
call npm run dev
