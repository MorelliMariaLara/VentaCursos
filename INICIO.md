# Cómo abrir NEXA como proyecto de inicio

| Archivo | Para qué |
| --- | --- |
| **`NEXA.sln`** | Abrir en **Visual Studio 2022** |
| **`Nexa.Web.esproj`** | Proyecto web → *Set as Startup Project* |
| **`iniciar.bat`** | Arranque rápido en Windows |
| **`INICIO.md`** | Esta guía |

---

## Requisito obligatorio

- **Node.js 20 o superior (LTS)** → https://nodejs.org  
- En el instalador, marcá la opción de agregar Node al **PATH**  
- Cerrá y reabrí Visual Studio / la terminal después de instalarlo  

Comprobá en CMD o PowerShell:

```powershell
node -v
npm -v
```

Tiene que mostrar algo como `v20.x` o `v22.x`. Si dice que no reconoce `node`, el PATH no quedó bien.

---

## Si `npm install` sale con código 1

Tu log muestra que npm **renombró todo `node_modules`** y falló al instalar los binarios de Windows:

- `@next/swc-win32-x64-msvc` (obligatorio para Next en Windows)
- `lightningcss-win32-x64-msvc`
- `@tailwindcss/oxide-win32-x64-msvc`

Eso pasa cuando Visual Studio, antivirus o un `npm` anterior dejan archivos bloqueados.

### Reparación (en este orden)

1. Cerrá **Visual Studio** por completo  
2. En PowerShell (como Administrador), excluí la carpeta del antivirus:

```powershell
Add-MpPreference -ExclusionPath "C:\Users\Maria Lara\source\repos\VentaCursos"
```

3. Actualizá el repo y ejecutá el instalador:

```powershell
cd "C:\Users\Maria Lara\source\repos\VentaCursos"
git pull origin main
Set-ExecutionPolicy -Scope Process Bypass
.\reparar.ps1
```

O doble clic en **`install.bat`**.

**No corras `npm run dev` hasta que la instalación diga OK.**  
Si ves `"next" no se reconoce`, es porque todavía falta instalar: corré `.\reparar.ps1`.

Si aún falla: clic derecho en `install.bat` → **Ejecutar como administrador**, o reiniciá la PC y volvé a correrlo.  

---

## Visual Studio 2022

1. Workload **Node.js development** (Visual Studio Installer)  
2. Abrí **`NEXA.sln`**  
3. Clic derecho en **`Nexa.Web`** → **Set as Startup Project**  
4. **F5**  
5. Abrí **http://localhost:3000**

El proyecto ahora usa `npm run dev` (sin bash), compatible con Windows.

---

## Alternativa sin Visual Studio

Doble clic en **`iniciar.bat`**, o:

```powershell
npm install --legacy-peer-deps
npm run dev
```

---

## Cuentas

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |
