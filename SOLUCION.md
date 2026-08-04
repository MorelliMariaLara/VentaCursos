# Solución SANTICAZA Capacitaciones (ASP.NET MVC + API)

Una sola solución Visual Studio con arquitectura MVC y APIs REST.

```text
NEXA.sln
└── Nexa.Web          → ASP.NET Core 8 (MVC + Web API)
    ├── Controllers   → páginas (Home, Courses, Account, Learn, …)
    ├── Controllers/Api → /api/auth, /api/courses, /api/payments, …
    ├── Models        → entidades y ViewModels
    ├── Services      → persistencia JSON, Mercado Pago, stream cifrado
    └── Views         → Razor + layout compartido
```

## Arranque

```bash
# 1) Ejecutar database/01_CreateTables.sql en LARA-NB\SQLEXPRESS02 / CursoVentas
dotnet run --project Nexa.Web
# → http://localhost:5000
```

## Flujo

1. **MVC** renderiza catálogo, login, aula y admin.
2. **APIs** atienden checkout (preferencia/pago), sesión de video y progreso.
3. Los datos viven en **SQL Server** `CursoVentas` (`LARA-NB\SQLEXPRESS02`).
