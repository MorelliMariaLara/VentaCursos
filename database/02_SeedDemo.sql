/*
  SANTICAZA Capacitaciones — datos demo (opcional)
  Ejecutar DESPUÉS de 01_CreateTables.sql

  Nota: los PasswordHash se regeneran al iniciar la app si no verifican.
  Este script inserta placeholders; preferí dejar que la app cree usuarios demo
  al primer arranque, o correr solo la parte de cursos.

  Usuarios demo (si la app no los creó aún):
    demo@santicaza.com  / demo1234
    admin@santicaza.com / admin1234
*/

USE [CursoVentas];
GO

/* Cursos seed (idempotente) */
IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE Id = N'course-ciberseguridad')
BEGIN
    INSERT INTO dbo.Courses
    (Id, Slug, Title, Subtitle, Description, Category, Level, Price, Currency, DurationHours,
     IncludesCertificate, CertificateName, ThumbnailGradient, Instructor, Published, UpdatedAt)
    VALUES
    (N'course-ciberseguridad', N'ciberseguridad-aplicada', N'Ciberseguridad aplicada',
     N'Protegé sistemas reales con prácticas de defensa y respuesta.',
     N'Un recorrido práctico por amenazas actuales, hardening, monitoreo y respuesta a incidentes.',
     N'Seguridad', N'Intermedio', 89000, N'ARS', 18, 1,
     N'Certificación SANTICAZA en Ciberseguridad Aplicada',
     N'linear-gradient(135deg, #2A3A22 0%, #3D4F2F 45%, #5C4030 100%)',
     N'Ing. Laura Rivas', 1, SYSUTCDATETIME());

    INSERT INTO dbo.CourseLearningOutcomes (CourseId, SortOrder, Text) VALUES
    (N'course-ciberseguridad', 1, N'Identificar vectores de ataque en aplicaciones web'),
    (N'course-ciberseguridad', 2, N'Implementar controles de acceso y cifrado en tránsito'),
    (N'course-ciberseguridad', 3, N'Diseñar un plan básico de respuesta a incidentes');

    INSERT INTO dbo.CourseModules (Id, CourseId, Title, SortOrder) VALUES
    (N'mod-cs-1', N'course-ciberseguridad', N'Fundamentos de amenaza', 1),
    (N'mod-cs-2', N'course-ciberseguridad', N'Defensa práctica', 2);

    INSERT INTO dbo.Lessons (Id, ModuleId, Title, DurationMinutes, SourceUrl, [Order]) VALUES
    (N'les-cs-1', N'mod-cs-1', N'Mapa de riesgos actuales', 22, N'local:lesson-a.mp4', 1),
    (N'les-cs-2', N'mod-cs-1', N'Superficie de ataque', 28, N'local:lesson-b.mp4', 2),
    (N'les-cs-3', N'mod-cs-2', N'Cifrado y sesiones seguras', 34, N'local:lesson-c.mp4', 3),
    (N'les-cs-4', N'mod-cs-2', N'Simulacro de incidente', 40, N'local:lesson-d.mp4', 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE Id = N'course-datos')
BEGIN
    INSERT INTO dbo.Courses
    (Id, Slug, Title, Subtitle, Description, Category, Level, Price, Currency, DurationHours,
     IncludesCertificate, CertificateName, ThumbnailGradient, Instructor, Published, UpdatedAt)
    VALUES
    (N'course-datos', N'analisis-de-datos-con-python', N'Análisis de datos con Python',
     N'De datasets crudos a decisiones con notebooks y visualización.',
     N'Aprendé a limpiar, transformar y comunicar datos con Python.',
     N'Datos', N'Inicial', 72000, N'ARS', 14, 1,
     N'Certificación SANTICAZA en Análisis de Datos',
     N'linear-gradient(145deg, #3E2A1C 0%, #5A6B3A 50%, #8B5A2B 100%)',
     N'Lic. Martín Escobar', 1, SYSUTCDATETIME());

    INSERT INTO dbo.CourseLearningOutcomes (CourseId, SortOrder, Text) VALUES
    (N'course-datos', 1, N'Manipular datos tabulares con pandas'),
    (N'course-datos', 2, N'Construir visualizaciones claras'),
    (N'course-datos', 3, N'Documentar un caso de análisis end-to-end');

    INSERT INTO dbo.CourseModules (Id, CourseId, Title, SortOrder) VALUES
    (N'mod-dt-1', N'course-datos', N'Bases del análisis', 1),
    (N'mod-dt-2', N'course-datos', N'Comunicación de hallazgos', 2);

    INSERT INTO dbo.Lessons (Id, ModuleId, Title, DurationMinutes, SourceUrl, [Order]) VALUES
    (N'les-dt-1', N'mod-dt-1', N'Entorno y primer notebook', 18, N'local:lesson-b.mp4', 1),
    (N'les-dt-2', N'mod-dt-1', N'Limpieza de datos', 26, N'local:lesson-a.mp4', 2),
    (N'les-dt-3', N'mod-dt-2', N'Visualización efectiva', 24, N'local:lesson-d.mp4', 3),
    (N'les-dt-4', N'mod-dt-2', N'Proyecto final guiado', 36, N'local:lesson-c.mp4', 4);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE Id = N'course-liderazgo')
BEGIN
    INSERT INTO dbo.Courses
    (Id, Slug, Title, Subtitle, Description, Category, Level, Price, Currency, DurationHours,
     IncludesCertificate, CertificateName, ThumbnailGradient, Instructor, Published, UpdatedAt)
    VALUES
    (N'course-liderazgo', N'liderazgo-de-equipos-digitales', N'Liderazgo de equipos digitales',
     N'Gestión, feedback y entrega continua en entornos remotos.',
     N'Herramientas para coordinar equipos híbridos y medir resultados.',
     N'Gestión', N'Avanzado', 98000, N'ARS', 12, 1,
     N'Certificación SANTICAZA en Liderazgo Digital',
     N'linear-gradient(160deg, #1C2618 0%, #4A3A28 48%, #6B5335 100%)',
     N'Mg. Sofía Herrera', 1, SYSUTCDATETIME());

    INSERT INTO dbo.CourseLearningOutcomes (CourseId, SortOrder, Text) VALUES
    (N'course-liderazgo', 1, N'Diseñar rituales de equipo que escalan'),
    (N'course-liderazgo', 2, N'Dar feedback accionable'),
    (N'course-liderazgo', 3, N'Alinear OKRs técnicos con negocio');

    INSERT INTO dbo.CourseModules (Id, CourseId, Title, SortOrder) VALUES
    (N'mod-ld-1', N'course-liderazgo', N'Sistema de liderazgo', 1),
    (N'mod-ld-2', N'course-liderazgo', N'Entrega y cultura', 2);

    INSERT INTO dbo.Lessons (Id, ModuleId, Title, DurationMinutes, SourceUrl, [Order]) VALUES
    (N'les-ld-1', N'mod-ld-1', N'Roles y claridad', 20, N'local:lesson-c.mp4', 1),
    (N'les-ld-2', N'mod-ld-1', N'Conversaciones difíciles', 27, N'local:lesson-a.mp4', 2),
    (N'les-ld-3', N'mod-ld-2', N'Métricas que importan', 23, N'local:lesson-b.mp4', 3),
    (N'les-ld-4', N'mod-ld-2', N'Caso integral de equipo', 32, N'local:lesson-d.mp4', 4);
END
GO

PRINT N'Seed de cursos demo listo. Los usuarios demo se crean al iniciar la app.';
GO
