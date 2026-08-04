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

En la carpeta del proyecto (`VentaCursos`) ejecutá:

```powershell
cd "C:\Users\Maria Lara\source\repos\VentaCursos"
git pull origin main
node -v
npm -v
npm cache clean --force
Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
npm install --legacy-peer-deps
```

Si sigue fallando, ejecutá **`iniciar.bat`** (hace el mismo proceso y muestra el error).

Causas frecuentes:
1. Node menor a 20  
2. No se hizo `git pull` y faltan archivos  
3. Antivirus bloqueando `node_modules`  
4. Instalación de Node sin reiniciar Visual Studio  

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
