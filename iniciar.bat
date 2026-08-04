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

if not exist "node_modules\next\dist\bin\next" (
  echo Falta Next.js. Usando instalacion limpia...
  call "%~dp0install.bat"
  if not exist "node_modules\next\dist\bin\next" (
    echo ERROR: despues de install.bat sigue faltando Next.
    pause
    exit /b 1
  )
) else (
  echo Dependencias ya presentes. Si npm falla, corre install.bat
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
