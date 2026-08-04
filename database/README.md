# SQL Server — CursoVentas

## Conexión

| Campo | Valor |
| --- | --- |
| Servidor | `LARA-NB\SQLEXPRESS02` |
| Base | `CursoVentas` |
| Auth | Windows (Trusted Connection) |

Connection string (ya configurada en `Nexa.Web/appsettings.json`):

```
Server=LARA-NB\SQLEXPRESS02;Database=CursoVentas;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## Scripts (ejecutar a mano en SSMS)

1. Creá la base si no existe:

```sql
IF DB_ID(N'CursoVentas') IS NULL
    CREATE DATABASE [CursoVentas];
```

2. Ejecutá **`01_CreateTables.sql`** (tablas + índices + FKs).
3. (Opcional) Ejecutá **`02_SeedDemo.sql`** para cargar los 3 cursos demo.  
   Si no lo corrés, la app siembra cursos y usuarios al arrancar.
4. (Opcional) **`03_ClearPurchases.sql`** — borra compras/inscripciones (Orders, Enrollments, progreso) para volver a probar el checkout. No borra usuarios ni cursos.
5. (Opcional) **`04_QuizTables.sql`** — preguntas por lección e intentos. La app también intenta crearlas al arrancar.

## Orden recomendado

```text
SSMS → conectar a LARA-NB\SQLEXPRESS02
  → CREATE DATABASE CursoVentas  (si falta)
  → abrir y ejecutar database/01_CreateTables.sql
  → (opcional) database/02_SeedDemo.sql
  → dotnet run --project Nexa.Web
```

## Tablas

- `Users`
- `Courses`
- `CourseLearningOutcomes`
- `CourseModules`
- `Lessons`
- `Orders`
- `Enrollments`
- `EnrollmentProgress`
