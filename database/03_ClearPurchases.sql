/*
  SANTICAZA Capacitaciones — borrar compras / accesos a cursos
  Servidor: LARA-NB\SQLEXPRESS02
  Base:     CursoVentas

  Borra: EnrollmentProgress → Enrollments → Orders
  NO toca: Users, Courses, módulos ni lecciones.

  Ejecutar en SSMS contra CursoVentas.
  Por defecto borra TODAS las compras.
  Para borrar solo un usuario o un curso, comentá el bloque "TODAS"
  y descomentá la opción B o C.
*/

USE [CursoVentas];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @Progress INT, @Enrollments INT, @Orders INT;

/* ========== TODAS las compras (default) ========== */
DELETE FROM dbo.EnrollmentProgress;
SET @Progress = @@ROWCOUNT;

DELETE FROM dbo.Enrollments;
SET @Enrollments = @@ROWCOUNT;

DELETE FROM dbo.Orders;
SET @Orders = @@ROWCOUNT;

/* ========== OPCION B: un usuario (por email) — descomentá y comentá el bloque de arriba ========== */
/*
DECLARE @Email NVARCHAR(256) = N'demo@santicaza.com';

DELETE ep
FROM dbo.EnrollmentProgress ep
INNER JOIN dbo.Enrollments e ON e.Id = ep.EnrollmentId
INNER JOIN dbo.Users u ON u.Id = e.UserId
WHERE u.Email = @Email;
SET @Progress = @@ROWCOUNT;

DELETE e
FROM dbo.Enrollments e
INNER JOIN dbo.Users u ON u.Id = e.UserId
WHERE u.Email = @Email;
SET @Enrollments = @@ROWCOUNT;

DELETE o
FROM dbo.Orders o
INNER JOIN dbo.Users u ON u.Id = o.UserId
WHERE u.Email = @Email;
SET @Orders = @@ROWCOUNT;
*/

/* ========== OPCION C: un curso (por slug) ========== */
/*
DECLARE @Slug NVARCHAR(160) = N'tu-slug-del-curso';

DELETE ep
FROM dbo.EnrollmentProgress ep
INNER JOIN dbo.Enrollments e ON e.Id = ep.EnrollmentId
INNER JOIN dbo.Courses c ON c.Id = e.CourseId
WHERE c.Slug = @Slug;
SET @Progress = @@ROWCOUNT;

DELETE e
FROM dbo.Enrollments e
INNER JOIN dbo.Courses c ON c.Id = e.CourseId
WHERE c.Slug = @Slug;
SET @Enrollments = @@ROWCOUNT;

DELETE o
FROM dbo.Orders o
INNER JOIN dbo.Courses c ON c.Id = o.CourseId
WHERE c.Slug = @Slug;
SET @Orders = @@ROWCOUNT;
*/

PRINT N'Eliminado → Progress=' + CAST(@Progress AS NVARCHAR(20))
    + N' | Enrollments=' + CAST(@Enrollments AS NVARCHAR(20))
    + N' | Orders=' + CAST(@Orders AS NVARCHAR(20));

SELECT
    (SELECT COUNT(*) FROM dbo.Orders) AS OrdersRestantes,
    (SELECT COUNT(*) FROM dbo.Enrollments) AS EnrollmentsRestantes,
    (SELECT COUNT(*) FROM dbo.EnrollmentProgress) AS ProgressRestante;

COMMIT;
GO
