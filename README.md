# NEXA — solución web ASP.NET MVC + APIs

Plataforma simple de venta de cursos:

- Catálogo y checkout (MVC)
- APIs JSON (`/api/...`) para pagos, progreso y video cifrado
- Pago simulado (o Mercado Pago con `.env`)
- Aula con video cifrado AES-CTR
- Certificados y panel admin

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 / VS Code / Cursor

## Arranque

```powershell
git pull origin main
dotnet run --project Nexa.Web
```

http://localhost:5000

O abrí **`NEXA.sln`** en Visual Studio → proyecto `Nexa.Web` → **F5**.

En Windows también podés usar **`iniciar.bat`**.

## Estructura

```text
NEXA.sln
Nexa.Web/
  Controllers/      → MVC (vistas) + Controllers/Api (JSON)
  Models/           → dominio y ViewModels
  Services/         → store JSON, pagos, stream
  Views/            → Razor
  wwwroot/          → CSS/JS
content/videos/     → videos demo
data/store.json     → base local (se crea sola)
```

## Cuentas demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |

## Mercado Pago (opcional)

Copiá `.env.example` a `.env` y completá `MP_PUBLIC_KEY` / `MP_ACCESS_TOKEN`.  
Sin eso, el checkout ofrece **Simular pago**.
