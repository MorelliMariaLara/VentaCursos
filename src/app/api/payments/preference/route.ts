import { NextResponse } from "next/server";
import { z } from "zod";
import { getSession } from "@/lib/auth";
import {
  createPendingOrder,
  getCourseBySlug,
  getEnrollment,
  updateOrder,
} from "@/lib/db";
import {
  allowSimulatePayments,
  createPreference,
  isMercadoPagoConfigured,
} from "@/lib/mercadopago";

const schema = z.object({
  slug: z.string().min(1),
});

export async function POST(req: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "Iniciá sesión para pagar" }, { status: 401 });
  }

  try {
    const { slug } = schema.parse(await req.json());
    const course = await getCourseBySlug(slug);
    if (!course || course.published === false) {
      return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
    }

    const enrollment = await getEnrollment(session.sub, course.id);
    if (enrollment) {
      return NextResponse.json(
        { error: "Ya tenés acceso a este curso", redirect: `/aprender/${slug}` },
        { status: 409 },
      );
    }

    const order = await createPendingOrder({
      userId: session.sub,
      courseId: course.id,
      amount: course.price,
      currency: course.currency,
    });

    if (!isMercadoPagoConfigured()) {
      if (!allowSimulatePayments()) {
        return NextResponse.json(
          {
            error:
              "Mercado Pago no está configurado. Agregá MP_ACCESS_TOKEN y NEXT_PUBLIC_MP_PUBLIC_KEY.",
          },
          { status: 503 },
        );
      }
      return NextResponse.json({
        orderId: order.id,
        amount: order.amount,
        currency: order.currency,
        simulate: true,
        publicKey: null,
        preferenceId: null,
        course: {
          id: course.id,
          slug: course.slug,
          title: course.title,
        },
      });
    }

    const preference = await createPreference({
      orderId: order.id,
      title: course.title,
      amount: course.price,
      currency: course.currency,
      payerEmail: session.email,
      courseId: course.id,
      slug: course.slug,
    });

    await updateOrder(order.id, {
      preferenceId: preference.id,
      payerEmail: session.email,
    });

    return NextResponse.json({
      orderId: order.id,
      amount: order.amount,
      currency: order.currency,
      simulate: false,
      publicKey: process.env.NEXT_PUBLIC_MP_PUBLIC_KEY,
      preferenceId: preference.id,
      course: {
        id: course.id,
        slug: course.slug,
        title: course.title,
      },
    });
  } catch (err) {
    if (err instanceof z.ZodError) {
      return NextResponse.json({ error: "Solicitud inválida" }, { status: 400 });
    }
    if (err instanceof Error && err.message === "ALREADY_OWNED") {
      return NextResponse.json(
        { error: "Ya tenés acceso a este curso" },
        { status: 409 },
      );
    }
    console.error("preference error", err);
    return NextResponse.json(
      { error: "No se pudo iniciar el checkout" },
      { status: 500 },
    );
  }
}
