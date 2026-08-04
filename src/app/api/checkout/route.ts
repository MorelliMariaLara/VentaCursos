import { NextResponse } from "next/server";
import { z } from "zod";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, purchaseCourse } from "@/lib/db";

const schema = z.object({
  slug: z.string().min(1),
});

export async function POST(req: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "Iniciá sesión para comprar" }, { status: 401 });
  }

  try {
    const { slug } = schema.parse(await req.json());
    const course = await getCourseBySlug(slug);
    if (!course) {
      return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
    }

    const { enrollment, order } = await purchaseCourse(session.sub, course.id);
    return NextResponse.json({
      ok: true,
      orderId: order.id,
      enrollmentId: enrollment.id,
      redirect: `/aprender/${course.slug}`,
    });
  } catch (err) {
    if (err instanceof z.ZodError) {
      return NextResponse.json({ error: "Solicitud inválida" }, { status: 400 });
    }
    return NextResponse.json({ error: "No se pudo completar la compra" }, { status: 500 });
  }
}
