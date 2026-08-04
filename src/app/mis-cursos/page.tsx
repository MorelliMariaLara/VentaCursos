import Link from "next/link";
import { redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { getCourseById, listEnrollmentsForUser } from "@/lib/db";

export default async function MyCoursesPage() {
  const session = await getSession();
  if (!session) redirect("/login?next=/mis-cursos");

  const enrollments = await listEnrollmentsForUser(session.sub);
  const rows = await Promise.all(
    enrollments.map(async (e) => {
      const course = await getCourseById(e.courseId);
      const total =
        course?.modules.flatMap((m) => m.lessons).length ?? 0;
      const done = Object.values(e.progress).filter(Boolean).length;
      return { enrollment: e, course, done, total };
    }),
  );

  return (
    <div className="shell" style={{ paddingBottom: "4rem" }}>
      <div className="page-hero">
        <p className="eyebrow">Tu biblioteca</p>
        <h1 className="page-title">Mis cursos</h1>
        <p className="muted">Hola, {session.name}. Continuá donde dejaste.</p>
      </div>

      {rows.length === 0 ? (
        <div className="checkout-box">
          <p>Todavía no compraste ningún curso.</p>
          <Link href="/cursos" className="btn">
            Explorar catálogo
          </Link>
        </div>
      ) : (
        <div className="course-grid">
          {rows.map(({ enrollment, course, done, total }) =>
            course ? (
              <Link
                key={enrollment.id}
                href={`/aprender/${course.slug}`}
                className="course-tile"
                style={{ background: course.thumbnailGradient }}
              >
                <div className="course-meta">
                  <span>
                    {done}/{total} lecciones
                  </span>
                  {enrollment.certificateCode && <span>Certificado</span>}
                </div>
                <h3>{course.title}</h3>
                <p>Abrir aula protegida</p>
              </Link>
            ) : null,
          )}
        </div>
      )}
    </div>
  );
}
