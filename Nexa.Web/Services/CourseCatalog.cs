using Nexa.Web.Models;

namespace Nexa.Web.Services;

public static class CourseCatalog
{
    private const string A = "local:lesson-a.mp4";
    private const string B = "local:lesson-b.mp4";
    private const string C = "local:lesson-c.mp4";
    private const string D = "local:lesson-d.mp4";

    public static List<Course> SeedCourses() =>
    [
        new Course
        {
            Id = "course-ciberseguridad",
            Slug = "ciberseguridad-aplicada",
            Title = "Ciberseguridad aplicada",
            Subtitle = "Protegé sistemas reales con prácticas de defensa y respuesta.",
            Description = "Un recorrido práctico por amenazas actuales, hardening, monitoreo y respuesta a incidentes.",
            Category = "Seguridad",
            Level = "Intermedio",
            Price = 89000,
            DurationHours = 18,
            CertificateName = "Certificación SANTICAZA en Ciberseguridad Aplicada",
            ThumbnailGradient = "linear-gradient(135deg, #2A3A22 0%, #3D4F2F 45%, #5C4030 100%)",
            Instructor = "Ing. Laura Rivas",
            LearningOutcomes =
            [
                "Identificar vectores de ataque en aplicaciones web",
                "Implementar controles de acceso y cifrado en tránsito",
                "Diseñar un plan básico de respuesta a incidentes",
            ],
            Modules =
            [
                new CourseModule
                {
                    Id = "mod-cs-1",
                    Title = "Fundamentos de amenaza",
                    Lessons =
                    [
                        new Lesson { Id = "les-cs-1", Title = "Mapa de riesgos actuales", DurationMinutes = 22, SourceUrl = A, Order = 1 },
                        new Lesson { Id = "les-cs-2", Title = "Superficie de ataque", DurationMinutes = 28, SourceUrl = B, Order = 2 },
                    ],
                },
                new CourseModule
                {
                    Id = "mod-cs-2",
                    Title = "Defensa práctica",
                    Lessons =
                    [
                        new Lesson { Id = "les-cs-3", Title = "Cifrado y sesiones seguras", DurationMinutes = 34, SourceUrl = C, Order = 3 },
                        new Lesson { Id = "les-cs-4", Title = "Simulacro de incidente", DurationMinutes = 40, SourceUrl = D, Order = 4 },
                    ],
                },
            ],
        },
        new Course
        {
            Id = "course-datos",
            Slug = "analisis-de-datos-con-python",
            Title = "Análisis de datos con Python",
            Subtitle = "De datasets crudos a decisiones con notebooks y visualización.",
            Description = "Aprendé a limpiar, transformar y comunicar datos con Python.",
            Category = "Datos",
            Level = "Inicial",
            Price = 72000,
            DurationHours = 14,
            CertificateName = "Certificación SANTICAZA en Análisis de Datos",
            ThumbnailGradient = "linear-gradient(145deg, #3E2A1C 0%, #5A6B3A 50%, #8B5A2B 100%)",
            Instructor = "Lic. Martín Escobar",
            LearningOutcomes =
            [
                "Manipular datos tabulares con pandas",
                "Construir visualizaciones claras",
                "Documentar un caso de análisis end-to-end",
            ],
            Modules =
            [
                new CourseModule
                {
                    Id = "mod-dt-1",
                    Title = "Bases del análisis",
                    Lessons =
                    [
                        new Lesson { Id = "les-dt-1", Title = "Entorno y primer notebook", DurationMinutes = 18, SourceUrl = B, Order = 1 },
                        new Lesson { Id = "les-dt-2", Title = "Limpieza de datos", DurationMinutes = 26, SourceUrl = A, Order = 2 },
                    ],
                },
                new CourseModule
                {
                    Id = "mod-dt-2",
                    Title = "Comunicación de hallazgos",
                    Lessons =
                    [
                        new Lesson { Id = "les-dt-3", Title = "Visualización efectiva", DurationMinutes = 24, SourceUrl = D, Order = 3 },
                        new Lesson { Id = "les-dt-4", Title = "Proyecto final guiado", DurationMinutes = 36, SourceUrl = C, Order = 4 },
                    ],
                },
            ],
        },
        new Course
        {
            Id = "course-liderazgo",
            Slug = "liderazgo-de-equipos-digitales",
            Title = "Liderazgo de equipos digitales",
            Subtitle = "Gestión, feedback y entrega continua en entornos remotos.",
            Description = "Herramientas para coordinar equipos híbridos y medir resultados.",
            Category = "Gestión",
            Level = "Avanzado",
            Price = 98000,
            DurationHours = 12,
            CertificateName = "Certificación SANTICAZA en Liderazgo Digital",
            ThumbnailGradient = "linear-gradient(160deg, #1C2618 0%, #4A3A28 48%, #6B5335 100%)",
            Instructor = "Mg. Sofía Herrera",
            LearningOutcomes =
            [
                "Diseñar rituales de equipo que escalan",
                "Dar feedback accionable",
                "Alinear OKRs técnicos con negocio",
            ],
            Modules =
            [
                new CourseModule
                {
                    Id = "mod-ld-1",
                    Title = "Sistema de liderazgo",
                    Lessons =
                    [
                        new Lesson { Id = "les-ld-1", Title = "Roles y claridad", DurationMinutes = 20, SourceUrl = C, Order = 1 },
                        new Lesson { Id = "les-ld-2", Title = "Conversaciones difíciles", DurationMinutes = 27, SourceUrl = A, Order = 2 },
                    ],
                },
                new CourseModule
                {
                    Id = "mod-ld-2",
                    Title = "Entrega y cultura",
                    Lessons =
                    [
                        new Lesson { Id = "les-ld-3", Title = "Métricas que importan", DurationMinutes = 23, SourceUrl = B, Order = 3 },
                        new Lesson { Id = "les-ld-4", Title = "Caso integral de equipo", DurationMinutes = 32, SourceUrl = D, Order = 4 },
                    ],
                },
            ],
        },
    ];
}
