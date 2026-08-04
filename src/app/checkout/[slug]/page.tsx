import { redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, getEnrollment } from "@/lib/db";
import { MercadoPagoCheckout } from "@/components/MercadoPagoCheckout";

export default async function CheckoutPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const session = await getSession();
  if (!session) redirect(`/login?next=/checkout/${slug}`);

  const course = await getCourseBySlug(slug);
  if (!course || course.published === false) redirect("/cursos");

  const enrollment = await getEnrollment(session.sub, course.id);
  if (enrollment) redirect(`/aprender/${slug}`);

  return (
    <div className="shell" style={{ padding: "2.5rem 0 4rem" }}>
      <MercadoPagoCheckout slug={slug} />
    </div>
  );
}
