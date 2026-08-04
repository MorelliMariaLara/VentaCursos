# SANTICAZA Capacitaciones

Plataforma web de venta de cursos (ASP.NET Core MVC + APIs):

- Catálogo y checkout
- Pago simulado (o Mercado Pago)
- Aula con video cifrado
- Certificados y panel admin
- Estética militar RealTree (verde / marrón)

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server Express: `LARA-NB\SQLEXPRESS02`
- Base de datos: `CursoVentas`

## SQL (manual)

1. Crear la base `CursoVentas` si no existe.
2. Ejecutar en SSMS: [`database/01_CreateTables.sql`](./database/01_CreateTables.sql)
3. (Opcional) Seed cursos: [`database/02_SeedDemo.sql`](./database/02_SeedDemo.sql)

Guía: [`database/README.md`](./database/README.md)

## Arranque

```powershell
dotnet run --project Nexa.Web
```

http://localhost:5000

O abrí **`NEXA.sln`** → `Nexa.Web` → **F5** / **`iniciar.bat`**.

Connection string en `Nexa.Web/appsettings.json` → `ConnectionStrings:CursoVentas`.

## Mercado Pago (Checkout Bricks + QR)

Configurá en `.env` o `appsettings.json`:

```env
MP_PUBLIC_KEY=TEST-xxxx
MP_ACCESS_TOKEN=TEST-xxxx
APP_URL=https://tu-dominio
MP_ALLOW_SIMULATE=false
```

Guía completa: [`docs/MERCADOPAGO.md`](./docs/MERCADOPAGO.md)

El aula/video **solo se habilita con pago acreditado** (`approved`).

## Cuentas demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@santicaza.com` | `demo1234` |
| Admin | `admin@santicaza.com` | `admin1234` |

## Admin: videos, YouTube y preguntas

1. Entrar como admin → **Admin** → **Contenido / videos** en el curso.
2. Crear un **módulo**, luego una **lección** con YouTube o MP4.
3. En cada lección, agregar **preguntas** (opciones A–D, una correcta).
4. El alumno, tras pagar:
   - debe **ver el video completo** para desbloquear el cuestionario;
   - aprueba el curso con **≥ 60%** de respuestas correctas;
   - si el promedio final es menor, **reinicia todo** (videos + preguntas).
