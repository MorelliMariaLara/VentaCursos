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

## Cuentas demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@santicaza.com` | `demo1234` |
| Admin | `admin@santicaza.com` | `admin1234` |
