# Cómo abrir NEXA como proyecto de inicio

Este repositorio es la **solución web NEXA**.

| Archivo | Para qué |
| --- | --- |
| **`NEXA.sln`** | Abrir en **Visual Studio 2022** |
| **`Nexa.Web.esproj`** | Proyecto web (marcá como *Startup Project*) |
| **`NEXA.code-workspace`** | Abrir en Cursor / VS Code |
| **`iniciar.bat`** | Arranque rápido en Windows |

---

## Visual Studio 2022 (recomendado si usás VS)

1. Instalá la workload **Node.js development** (Visual Studio Installer)  
2. Abrí **`NEXA.sln`**  
3. En el Explorador de soluciones, clic derecho en **`Nexa.Web`** → **Set as Startup Project**  
4. Pulsá **F5** (o el botón Start)  
5. Se abre **http://localhost:3000**

Si es la primera vez, VS ejecuta `npm install` y luego `npm run solution:start`.

---

## Cursor / VS Code

1. Abrí **`NEXA.code-workspace`**  
2. Terminal:

```bash
npm install
npm run solution:start
```

3. O Run and Debug → **NEXA Web (proyecto de inicio)**

---

## Doble clic (Windows)

Ejecutá **`iniciar.bat`**.

---

## Cuentas de prueba

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |

---

## URLs principales

| URL | Qué es |
| --- | --- |
| `/` | Landing |
| `/cursos` | Catálogo |
| `/checkout/[slug]` | Mercado Pago |
| `/aprender/[slug]` | Aula cifrada |
| `/admin` | Panel admin |

---

## Requisito

- **Node.js 20+**: https://nodejs.org  
- En Visual Studio: workload **Node.js development**
