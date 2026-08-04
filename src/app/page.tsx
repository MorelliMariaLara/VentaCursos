import Link from "next/link";
import { listCourses, toPublicCourse } from "@/lib/db";
import { formatPrice } from "@/lib/format";

export default async function HomePage() {
  const courses = (await listCourses()).map(toPublicCourse);

  return (
    <>
      <section className="hero">
        <div className="hero-media" aria-hidden />
        <div className="shell hero-content">
          <p className="hero-brand">NEXA</p>
          <h1>Cursos y certificaciones con video cifrado.</h1>
          <p>
            Comprá acceso, mirá lecciones protegidas y obtené certificados
            verificables. El contenido nunca se entrega como archivo descargable.
          </p>
          <div className="hero-actions">
            <Link href="/cursos" className="btn">
              Ver catálogo
            </Link>
            <Link href="/registro" className="btn btn-secondary">
              Empezar gratis
            </Link>
          </div>
        </div>
      </section>

      <section className="section">
        <div className="shell">
          <div className="section-head">
            <p className="eyebrow">Catálogo</p>
            <h2>Formación lista para vender y certificar</h2>
            <p className="muted">
              Cada curso incluye módulos en video, seguimiento de progreso y
              certificación al completar.
            </p>
          </div>
          <div className="course-grid">
            {courses.map((course) => (
              <Link
                key={course.id}
                href={`/cursos/${course.slug}`}
                className="course-tile"
                style={{ background: course.thumbnailGradient }}
              >
                <div className="course-meta">
                  <span>{course.category}</span>
                  <span>{course.level}</span>
                  <span>{course.durationHours} h</span>
                </div>
                <h3>{course.title}</h3>
                <p>{course.subtitle}</p>
                <strong>{formatPrice(course.price, course.currency)}</strong>
              </Link>
            ))}
          </div>
        </div>
      </section>

      <section className="section">
        <div className="shell feature-row">
          <div className="feature-panel">
            <p className="eyebrow">Protección de contenido</p>
            <h2>El video viaja cifrado. Se descifra solo al reproducir.</h2>
            <p className="muted">
              NEXA no expone la URL original del video. El servidor entrega un
              stream AES-256-CTR con sesión de corta duración, marca de agua por
              usuario y bloqueos ante intentos de captura.
            </p>
            <ul className="feature-list">
              <li>Proxy cifrado: el navegador nunca ve el archivo fuente</li>
              <li>Clave de sesión por lección y por usuario</li>
              <li>Detección de compartir/grabar pantalla vía getDisplayMedia</li>
              <li>Certificados emitidos al completar el 100% del curso</li>
            </ul>
          </div>
          <div className="shield-visual">
            <p className="eyebrow" style={{ color: "#9ee0d4" }}>
              Capa de seguridad
            </p>
            <h3 style={{ fontFamily: "var(--font-display)", fontSize: "1.8rem", margin: 0 }}>
              AES-256-CTR + watermark + sesión firmada
            </h3>
            <p style={{ opacity: 0.85, marginBottom: 0 }}>
              Ideal como base. Para producción enterprise se puede sumar DRM
              Widevine/FairPlay.
            </p>
          </div>
        </div>
      </section>
    </>
  );
}
