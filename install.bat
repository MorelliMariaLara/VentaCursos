@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ================================
echo  NEXA - Reparacion de instalacion
echo ================================
echo.
echo IMPORTANTE:
echo  1^) Cerra Visual Studio por completo
echo  2^) Cerra Cursor / VS Code si tenes el repo abierto
echo  3^) Pausá Windows Defender / antivirus 2 minutos
echo.
pause

echo.
echo [1/6] Verificando Node...
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

echo [2/6] Cerrando procesos que bloquean archivos...
taskkill /F /IM node.exe >nul 2>nul
taskkill /F /IM npm.cmd >nul 2>nul
taskkill /F /IM esbuild.exe >nul 2>nul
timeout /t 3 /nobreak >nul

echo [3/6] Borrando node_modules corruptos (incluye carpetas .xxxxx)...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "if (Test-Path 'node_modules') { Write-Host 'Borrando node_modules...'; Remove-Item -LiteralPath 'node_modules' -Recurse -Force -ErrorAction SilentlyContinue }; ^
   Get-ChildItem -Force -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like '.*' -and $_.Name -ne '.git' -and $_.Name -ne '.next' -and $_.Name -ne '.vscode' -and $_.Name -ne '.vs' } | ForEach-Object { try { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue } catch {} }; ^
   if (Test-Path 'node_modules') { cmd /c 'rmdir /s /q node_modules' 2>$null }; ^
   if (Test-Path 'node_modules') { Write-Host 'AVISO: no se pudo borrar todo node_modules. Reinicia la PC y volve a ejecutar install.bat' -ForegroundColor Yellow; exit 2 } else { Write-Host 'node_modules eliminado OK' }"
if errorlevel 2 (
  pause
  exit /b 1
)

echo.
echo [4/6] Limpiando cache de npm...
call npm cache clean --force

echo.
echo [5/6] Instalando dependencias (incluye binarios Windows)...
call npm install --legacy-peer-deps --include=optional --prefer-online --no-audit --no-fund
if errorlevel 1 (
  echo.
  echo Primer intento fallo. Reintentando SIN package-lock...
  if exist "package-lock.json" del /f /q "package-lock.json"
  if exist "node_modules" (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-Item -LiteralPath 'node_modules' -Recurse -Force -ErrorAction SilentlyContinue"
  )
  call npm install --legacy-peer-deps --include=optional --prefer-online --no-audit --no-fund
  if errorlevel 1 (
    echo.
    echo ERROR: npm install volvio a fallar.
    echo.
    echo Segun tu log, fallaron binarios Windows:
    echo   @next/swc-win32-x64-msvc
    echo   lightningcss-win32-x64-msvc
    echo   @tailwindcss/oxide-win32-x64-msvc
    echo.
    echo Hace esto y reintenta:
    echo  1^) Clic derecho en install.bat - Ejecutar como administrador
    echo  2^) Exclui esta carpeta del antivirus:
    echo     %CD%
    echo  3^) En PowerShell ^(Admin^):
    echo     Add-MpPreference -ExclusionPath "%CD%"
    echo  4^) Reinicia la PC y corre install.bat de nuevo
    pause
    exit /b 1
  )
)

echo.
echo [6/6] Verificando Next + SWC Windows...
if not exist "node_modules\next\dist\bin\next" (
  echo ERROR: next no quedo instalado.
  pause
  exit /b 1
)
if not exist "node_modules\@next\swc-win32-x64-msvc" (
  echo.
  echo AVISO: falta @next\swc-win32-x64-msvc
  echo Intentando instalarlo aparte...
  call npm install @next/swc-win32-x64-msvc --save-optional --legacy-peer-deps --prefer-online
  if not exist "node_modules\@next\swc-win32-x64-msvc" (
    echo ERROR: Next no tiene el binario de Windows. El antivirus suele bloquearlo.
    echo Exclui la carpeta del proyecto y reejecuta install.bat
    pause
    exit /b 1
  )
)

echo.
echo ================================
echo  Instalacion OK
echo ================================
echo.
echo Para iniciar:
echo   npm run dev
echo Luego abri: http://localhost:3000
echo.
set /p RUNDEV=Queres iniciar ahora? (S/N): 
if /I "%RUNDEV%"=="S" (
  start "" "http://localhost:3000"
  call npm run dev
)
pause
