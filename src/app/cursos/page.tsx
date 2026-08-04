import Link from "next/link";
import { listCourses, toPublicCourse } from "@/lib/db";
import { formatPrice } from "@/lib/format";

export default async function CoursesPage() {
  const courses = (await listCourses()).map(toPublicCourse);

  return (
    <div className="shell">
      <div className="page-hero">
        <p className="eyebrow">Catálogo NEXA</p>
        <h1 className="page-title">Cursos y rutas de certificación</h1>
        <p className="muted">
          Acceso de por vida a las lecciones compradas, con reproducción cifrada
          en el aula protegida.
        </p>
      </div>
      <div className="course-grid" style={{ paddingBottom: "4rem" }}>
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
            </div>
            <h3>{course.title}</h3>
            <p>{course.subtitle}</p>
            <strong>{formatPrice(course.price, course.currency)}</strong>
          </Link>
        ))}
      </div>
    </div>
  );
}
