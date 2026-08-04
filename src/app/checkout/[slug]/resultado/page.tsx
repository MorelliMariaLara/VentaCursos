import Link from "next/link";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, getEnrollment, getOrderById } from "@/lib/db";

export default async function CheckoutResultPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ status?: string; orderId?: string }>;
}) {
  const { slug } = await params;
  const { status, orderId } = await searchParams;
  const session = await getSession();
  const course = await getCourseBySlug(slug);
  const enrollment =
    session && course
      ? await getEnrollment(session.sub, course.id)
      : null;
  const order = orderId ? await getOrderById(orderId) : null;

  const effective =
    enrollment || order?.status === "paid"
      ? "success"
      : status ?? order?.status ?? "pending";

  return (
    <div className="auth-wrap">
      <p className="eyebrow">Resultado del pago</p>
      <h1 className="page-title">
        {effective === "success" || effective === "paid"
          ? "Pago aprobado"
          : effective === "failure" || effective === "rejected"
            ? "Pago no aprobado"
            : "Pago pendiente"}
      </h1>
      <p className="muted" style={{ marginBottom: "1.2rem" }}>
        {effective === "success" || effective === "paid"
          ? "Ya podés entrar al aula protegida."
          : effective === "failure" || effective === "rejected"
            ? "Podés intentar nuevamente con otro medio de pago."
            : "Cuando Mercado Pago confirme el pago, habilitaremos el acceso automáticamente."}
      </p>
      {enrollment || order?.status === "paid" ? (
        <Link href={`/aprender/${slug}`} className="btn">
          Ir al aula
        </Link>
      ) : (
        <div style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap" }}>
          <Link href={`/checkout/${slug}`} className="btn">
            Reintentar pago
          </Link>
          <Link href={`/cursos/${slug}`} className="btn btn-secondary">
            Volver al curso
          </Link>
        </div>
      )}
    </div>
  );
}
