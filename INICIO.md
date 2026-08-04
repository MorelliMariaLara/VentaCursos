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

En Windows suele fallar por `node_modules` a medias o archivos bloqueados (Visual Studio / antivirus).

1. Cerrá **Visual Studio** por completo  
2. Doble clic en **`install.bat`**  
   o en CMD:

```bat
cd /d "C:\Users\Maria Lara\source\repos\VentaCursos"
git pull origin main
install.bat
```

Eso borra `node_modules`, limpia caché y reinstala todo.

Si aún falla:
1. Pausá el antivirus un momento  
2. Ejecutá `install.bat` como Administrador  
3. Desactivá VPN si usás  

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
