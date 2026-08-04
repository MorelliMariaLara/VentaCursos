@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  NEXA Web - Proyecto de inicio
echo ================================
echo.

where node >nul 2>nul
if errorlevel 1 (
  echo ERROR: No se encontro Node.js.
  echo Instala Node.js LTS 20 o superior desde:
  echo https://nodejs.org
  echo.
  echo Despues cierra y vuelve a abrir esta ventana.
  pause
  exit /b 1
)

for /f "tokens=1 delims=v" %%v in ('node -v') do set NODEVER=%%v
echo Node.js detectado: 
node -v
npm -v
echo.

if not exist ".env.local" (
  copy /Y ".env.example" ".env.local" >nul
  echo Creado .env.local
)

echo Ejecutando npm install...
call npm install --legacy-peer-deps
if errorlevel 1 (
  echo.
  echo npm install fallo. Reintentando limpio...
  if exist "node_modules" rmdir /s /q "node_modules"
  if exist "package-lock.json" del /f /q "package-lock.json"
  call npm cache clean --force
  call npm install --legacy-peer-deps
  if errorlevel 1 (
    echo.
    echo ERROR: npm install salio con codigo 1.
    echo Revisá:
    echo  1^) Node.js sea version 20 o superior
    echo  2^) Que la carpeta no este bloqueada por antivirus
    echo  3^) Ejecutar PowerShell/CMD como usuario normal ^(no admin obligatorio^)
    echo  4^) Pegame el error completo que aparece arriba
    pause
    exit /b 1
  )
)

echo.
echo Abriendo http://localhost:3000
echo Alumno: demo@nexa.academy / demo1234
echo Admin:  admin@nexa.academy / admin1234
echo.
start "" "http://localhost:3000"
call npm run dev
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
  echo El servidor termino con codigo %EXITCODE%.
  pause
)
exit /b %EXITCODE%
