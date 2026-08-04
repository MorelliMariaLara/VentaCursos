import type { Course } from "./types";

/**
 * Local lesson files under /content/videos (never publicly served).
 * The stream API reads and encrypts them before sending to the browser.
 */
const SAMPLE_A = "local:lesson-a.mp4";
const SAMPLE_B = "local:lesson-b.mp4";
const SAMPLE_C = "local:lesson-c.mp4";
const SAMPLE_D = "local:lesson-d.mp4";

export const COURSES: Course[] = [
  {
    id: "course-ciberseguridad",
    slug: "ciberseguridad-aplicada",
    title: "Ciberseguridad aplicada",
    subtitle: "Protegé sistemas reales con prácticas de defensa y respuesta.",
    description:
      "Un recorrido práctico por amenazas actuales, hardening, monitoreo y respuesta a incidentes. Ideal para equipos técnicos que necesitan certificar competencias medibles.",
    category: "Seguridad",
    level: "Intermedio",
    price: 89000,
    currency: "ARS",
    durationHours: 18,
    includesCertificate: true,
    certificateName: "Certificación NEXA en Ciberseguridad Aplicada",
    thumbnailGradient: "linear-gradient(135deg, #0B3D4A 0%, #1A7A6D 55%, #C45C26 100%)",
    instructor: "Ing. Laura Rivas",
    learningOutcomes: [
      "Identificar vectores de ataque en aplicaciones web",
      "Implementar controles de acceso y cifrado en tránsito",
      "Diseñar un plan básico de respuesta a incidentes",
    ],
    modules: [
      {
        id: "mod-cs-1",
        title: "Fundamentos de amenaza",
        lessons: [
          {
            id: "les-cs-1",
            title: "Mapa de riesgos actuales",
            durationMinutes: 22,
            sourceUrl: SAMPLE_A,
            order: 1,
          },
          {
            id: "les-cs-2",
            title: "Superficie de ataque y activos críticos",
            durationMinutes: 28,
            sourceUrl: SAMPLE_B,
            order: 2,
          },
        ],
      },
      {
        id: "mod-cs-2",
        title: "Defensa práctica",
        lessons: [
          {
            id: "les-cs-3",
            title: "Cifrado, tokens y sesiones seguras",
            durationMinutes: 34,
            sourceUrl: SAMPLE_C,
            order: 3,
          },
          {
            id: "les-cs-4",
            title: "Simulacro de incidente",
            durationMinutes: 40,
            sourceUrl: SAMPLE_D,
            order: 4,
          },
        ],
      },
    ],
  },
  {
    id: "course-datos",
    slug: "analisis-de-datos-con-python",
    title: "Análisis de datos con Python",
    subtitle: "De datasets crudos a decisiones con notebooks y visualización.",
    description:
      "Aprendé a limpiar, transformar y comunicar datos con Python. Incluye evaluación final y certificado verificable de NEXA.",
    category: "Datos",
    level: "Inicial",
    price: 72000,
    currency: "ARS",
    durationHours: 14,
    includesCertificate: true,
    certificateName: "Certificación NEXA en Análisis de Datos",
    thumbnailGradient: "linear-gradient(145deg, #123048 0%, #2F6F8F 50%, #E8A15A 100%)",
    instructor: "Lic. Martín Escobar",
    learningOutcomes: [
      "Manipular datos tabulares con pandas",
      "Construir visualizaciones claras para negocio",
      "Documentar un caso de análisis end-to-end",
    ],
    modules: [
      {
        id: "mod-dt-1",
        title: "Bases del análisis",
        lessons: [
          {
            id: "les-dt-1",
            title: "Entorno y primer notebook",
            durationMinutes: 18,
            sourceUrl: SAMPLE_B,
            order: 1,
          },
          {
            id: "les-dt-2",
            title: "Limpieza y tipado de datos",
            durationMinutes: 26,
            sourceUrl: SAMPLE_A,
            order: 2,
          },
        ],
      },
      {
        id: "mod-dt-2",
        title: "Comunicación de hallazgos",
        lessons: [
          {
            id: "les-dt-3",
            title: "Visualización efectiva",
            durationMinutes: 24,
            sourceUrl: SAMPLE_D,
            order: 3,
          },
          {
            id: "les-dt-4",
            title: "Proyecto final guiado",
            durationMinutes: 36,
            sourceUrl: SAMPLE_C,
            order: 4,
          },
        ],
      },
    ],
  },
  {
    id: "course-liderazgo",
    slug: "liderazgo-de-equipos-digitales",
    title: "Liderazgo de equipos digitales",
    subtitle: "Gestión, feedback y entrega continua en entornos remotos.",
    description:
      "Herramientas para coordinar equipos híbridos, medir resultados y sostener cultura de aprendizaje. Certificación orientada a managers y tech leads.",
    category: "Gestión",
    level: "Avanzado",
    price: 98000,
    currency: "ARS",
    durationHours: 12,
    includesCertificate: true,
    certificateName: "Certificación NEXA en Liderazgo Digital",
    thumbnailGradient: "linear-gradient(160deg, #1C2B24 0%, #3E6B52 48%, #B86B3C 100%)",
    instructor: "Mg. Sofía Herrera",
    learningOutcomes: [
      "Diseñar rituales de equipo que escalan",
      "Dar feedback accionable sin fricción",
      "Alinear OKRs técnicos con negocio",
    ],
    modules: [
      {
        id: "mod-ld-1",
        title: "Sistema de liderazgo",
        lessons: [
          {
            id: "les-ld-1",
            title: "Roles, ownership y claridad",
            durationMinutes: 20,
            sourceUrl: SAMPLE_C,
            order: 1,
          },
          {
            id: "les-ld-2",
            title: "Conversaciones difíciles",
            durationMinutes: 27,
            sourceUrl: SAMPLE_A,
            order: 2,
          },
        ],
      },
      {
        id: "mod-ld-2",
        title: "Entrega y cultura",
        lessons: [
          {
            id: "les-ld-3",
            title: "Métricas que importan",
            durationMinutes: 23,
            sourceUrl: SAMPLE_B,
            order: 3,
          },
          {
            id: "les-ld-4",
            title: "Caso integral de equipo",
            durationMinutes: 32,
            sourceUrl: SAMPLE_D,
            order: 4,
          },
        ],
      },
    ],
  },
];
