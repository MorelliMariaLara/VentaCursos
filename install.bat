@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo NEXA - instalacion limpia
taskkill /F /IM node.exe >nul 2>nul
if exist "node_modules" rmdir /s /q "node_modules"
call npm cache clean --force
call npm install
if errorlevel 1 (
  echo ERROR: npm install fallo.
  pause
  exit /b 1
)
echo OK. Ahora: npm run dev
pause
