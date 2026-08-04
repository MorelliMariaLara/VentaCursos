import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { findUserById, getCourseBySlug, getEnrollment } from "@/lib/db";
import { formatDate } from "@/lib/format";

export default async function CertificatePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const session = await getSession();
  if (!session) redirect(`/login?next=/certificado/${slug}`);

  const course = await getCourseBySlug(slug);
  if (!course) notFound();

  const enrollment = await getEnrollment(session.sub, course.id);
  if (!enrollment?.certificateCode) {
    redirect(`/aprender/${slug}`);
  }

  const user = await findUserById(session.sub);

  return (
    <div className="cert-sheet">
      <p className="eyebrow">NEXA Academy</p>
      <p>Certifica que</p>
      <h1>{user?.name ?? session.name}</h1>
      <p>completó satisfactoriamente</p>
      <h2 style={{ fontFamily: "var(--font-display)", margin: "0.4rem 0 1rem" }}>
        {course.certificateName}
      </h2>
      <p className="muted">
        Curso: {course.title}
        <br />
        Código: <strong>{enrollment.certificateCode}</strong>
        <br />
        Emitido: {formatDate(enrollment.certificateIssuedAt!)}
      </p>
      <div style={{ marginTop: "2rem" }}>
        <Link href={`/aprender/${slug}`} className="btn btn-secondary">
          Volver al aula
        </Link>
      </div>
    </div>
  );
}
