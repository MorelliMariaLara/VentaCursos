import Link from "next/link";
import { listCourses, toPublicCourse } from "@/lib/db";

export default async function CertificationsPage() {
  const courses = (await listCourses())
    .map(toPublicCourse)
    .filter((c) => c.includesCertificate);

  return (
    <div className="shell" style={{ paddingBottom: "4rem" }}>
      <div className="page-hero">
        <p className="eyebrow">Credenciales</p>
        <h1 className="page-title">Certificaciones NEXA</h1>
        <p className="muted">
          Completá el 100% de las lecciones de un curso para emitir un
          certificado con código verificable.
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
              <span>Certificación</span>
              <span>{course.level}</span>
            </div>
            <h3>{course.certificateName}</h3>
            <p>Asociada al curso {course.title}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
