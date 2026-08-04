@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  NEXA - Instalacion limpia
echo ================================
echo.
echo Cerra Visual Studio y cualquier "npm run dev" antes de continuar.
echo.
pause

echo.
echo [1/5] Verificando Node...
where node >nul 2>nul
if errorlevel 1 (
  echo ERROR: Node.js no esta instalado o no esta en el PATH.
  echo Instala Node LTS desde https://nodejs.org y reabri esta ventana.
  pause
  exit /b 1
)
node -v
npm -v
echo.

echo [2/5] Cerrando procesos node.exe si hay...
taskkill /F /IM node.exe >nul 2>nul
timeout /t 2 /nobreak >nul

echo [3/5] Borrando node_modules y cache...
if exist "node_modules" (
  echo Borrando node_modules...
  rmdir /s /q "node_modules"
)
if exist "package-lock.json" (
  echo Conservando package-lock.json
)
call npm cache clean --force

echo.
echo [4/5] Instalando dependencias...
call npm install --legacy-peer-deps --fetch-retries=5 --fetch-retry-mintimeout=20000 --fetch-retry-maxtimeout=120000
if errorlevel 1 (
  echo.
  echo Fallo el install. Reintentando SIN package-lock...
  if exist "package-lock.json" del /f /q "package-lock.json"
  if exist "node_modules" rmdir /s /q "node_modules"
  call npm install --legacy-peer-deps --fetch-retries=5
  if errorlevel 1 (
    echo.
    echo ERROR: npm install volvio a fallar.
    echo Probá:
    echo  1^) Ejecutar este .bat como Administrador
    echo  2^) Pausar antivirus temporalmente
    echo  3^) Verificar internet / VPN
    pause
    exit /b 1
  )
)

echo.
echo [5/5] Verificando Next.js...
if not exist "node_modules\next\dist\bin\next" (
  echo ERROR: next no quedo instalado.
  pause
  exit /b 1
)

echo.
echo Instalacion OK.
echo Para iniciar la web:
echo   npm run dev
echo.
set /p RUNDEV=Queres iniciar ahora? (S/N): 
if /I "%RUNDEV%"=="S" (
  start "" "http://localhost:3000"
  call npm run dev
)
pause
