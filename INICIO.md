# Cómo iniciar SANTICAZA Capacitaciones

Solución **ASP.NET Core MVC + APIs** + **SQL Server**.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server: `LARA-NB\SQLEXPRESS02` · base `CursoVentas`

## SQL (una vez)

En SSMS, contra `LARA-NB\SQLEXPRESS02`:

1. `CREATE DATABASE CursoVentas;` (si no existe)
2. Ejecutar `database\01_CreateTables.sql`
3. (Opcional) `database\02_SeedDemo.sql`

## PowerShell / CMD

```powershell
cd "C:\Users\Maria Lara\source\repos\VentaCursos"
git pull origin main
dotnet run --project Nexa.Web
```

Abrí http://localhost:5000

O doble clic en **`iniciar.bat`**.

## Visual Studio 2022

1. Abrí `NEXA.sln`
2. Proyecto de inicio: `Nexa.Web`
3. **F5**

## Cuentas

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@santicaza.com` | `demo1234` |
| Admin | `admin@santicaza.com` | `admin1234` |
