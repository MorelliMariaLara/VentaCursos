# Cómo iniciar NEXA (simple)

Solo necesitás **Node.js 20+**.

## PowerShell (Windows)

```powershell
cd "C:\Users\Maria Lara\source\repos\VentaCursos"
git pull origin main
npm install
npm run dev
```

Abrí http://localhost:3000

O doble clic en **`iniciar.bat`**.

## Cuentas demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |

## Visual Studio

1. Abrí `NEXA.sln`
2. Set as Startup Project → `Nexa.Web`
3. F5

## Dependencias

Solo 2 paquetes: `express` y `bcryptjs`.
