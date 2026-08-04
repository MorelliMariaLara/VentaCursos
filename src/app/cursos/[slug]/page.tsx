import Link from "next/link";
import { notFound } from "next/navigation";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, getEnrollment, toPublicCourse } from "@/lib/db";
import { formatPrice } from "@/lib/format";
import { BuyButton } from "@/components/BuyButton";

export default async function CourseDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const courseRaw = await getCourseBySlug(slug);
  if (!courseRaw) notFound();
  const course = toPublicCourse(courseRaw);
  const session = await getSession();
  const enrollment = session
    ? await getEnrollment(session.sub, course.id)
    : null;

  return (
    <div className="shell">
      <div className="page-hero">
        <p className="eyebrow">
          {course.category} · {course.level}
        </p>
        <h1 className="page-title">{course.title}</h1>
        <p className="muted">{course.subtitle}</p>
      </div>

      <div className="detail-layout">
        <div>
          <p>{course.description}</p>
          <h2 style={{ fontFamily: "var(--font-display)", marginTop: "2rem" }}>
            Qué vas a lograr
          </h2>
          <ul className="feature-list">
            {course.learningOutcomes.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>

          <h2 style={{ fontFamily: "var(--font-display)", marginTop: "2rem" }}>
            Temario
          </h2>
          <ol className="module-list">
            {course.modules.map((mod) => (
              <li key={mod.id}>
                <h3>{mod.title}</h3>
                <ul>
                  {mod.lessons.map((lesson) => (
                    <li key={lesson.id}>
                      {lesson.title} · {lesson.durationMinutes} min
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ol>
        </div>

        <aside className="detail-aside">
          <p className="eyebrow">Acceso + certificado</p>
          <p className="price">{formatPrice(course.price, course.currency)}</p>
          <p className="muted">
            {course.durationHours} horas · Instructor/a {course.instructor}
          </p>
          <p className="muted" style={{ marginBottom: "1rem" }}>
            Incluye {course.certificateName}
          </p>
          {enrollment ? (
            <Link href={`/aprender/${course.slug}`} className="btn">
              Ir al aula
            </Link>
          ) : (
            <BuyButton slug={course.slug} />
          )}
          {!session && (
            <p className="muted" style={{ marginTop: "0.8rem", fontSize: "0.9rem" }}>
              Necesitás cuenta para comprar.{" "}
              <Link href={`/login?next=/cursos/${course.slug}`}>Ingresá</Link>
            </p>
          )}
        </aside>
      </div>
    </div>
  );
}
