import { randomUUID } from "crypto";
import { NextResponse } from "next/server";
import { z } from "zod";
import { getSession } from "@/lib/auth";
import {
  fulfillPaidOrder,
  getCourseById,
  getOrderById,
  updateOrder,
} from "@/lib/db";
import {
  allowSimulatePayments,
  createPayment,
  isMercadoPagoConfigured,
  mapMpStatusToOrderStatus,
} from "@/lib/mercadopago";

const schema = z.object({
  orderId: z.string().min(1),
  formData: z.record(z.string(), z.unknown()).optional(),
  simulate: z.boolean().optional(),
});

export async function POST(req: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = schema.parse(await req.json());
    const order = await getOrderById(body.orderId);
    if (!order || order.userId !== session.sub) {
      return NextResponse.json({ error: "Orden no encontrada" }, { status: 404 });
    }

    if (order.status === "paid") {
      const course = await getCourseById(order.courseId);
      return NextResponse.json({
        status: "paid",
        orderId: order.id,
        redirect: course ? `/aprender/${course.slug}` : "/mis-cursos",
      });
    }

    const course = await getCourseById(order.courseId);
    if (!course) {
      return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
    }

    // Simulated approval for local/dev without credentials
    if (body.simulate || (!isMercadoPagoConfigured() && allowSimulatePayments())) {
      await updateOrder(order.id, {
        simulated: true,
        paymentMethod: "simulate",
        statusDetail: "simulated_approval",
        paymentId: `sim-${randomUUID().slice(0, 8)}`,
      });
      await fulfillPaidOrder(order.id);
      return NextResponse.json({
        status: "paid",
        orderId: order.id,
        redirect: `/aprender/${course.slug}`,
        simulated: true,
      });
    }

    if (!isMercadoPagoConfigured()) {
      return NextResponse.json(
        { error: "Mercado Pago no configurado" },
        { status: 503 },
      );
    }

    if (!body.formData) {
      return NextResponse.json(
        { error: "Faltan datos del Payment Brick" },
        { status: 400 },
      );
    }

    const paymentBody = {
      ...body.formData,
      transaction_amount: order.amount,
      description: course.title,
      external_reference: order.id,
      metadata: {
        order_id: order.id,
        course_id: course.id,
        user_id: session.sub,
      },
      payer: {
        ...((body.formData.payer as object) ?? {}),
        email:
          (body.formData.payer as { email?: string } | undefined)?.email ??
          session.email,
      },
    };

    const payment = await createPayment(paymentBody, {
      idempotencyKey: `${order.id}-${randomUUID()}`,
    });

    const mapped = mapMpStatusToOrderStatus(payment.status);
    await updateOrder(order.id, {
      paymentId: payment.id ? String(payment.id) : undefined,
      paymentMethod: payment.payment_method_id,
      statusDetail: payment.status_detail,
      payerEmail: payment.payer?.email ?? session.email,
      ...(mapped !== "paid" ? { status: mapped } : {}),
    });

    if (mapped === "paid") {
      await fulfillPaidOrder(order.id);
      return NextResponse.json({
        status: "paid",
        paymentId: payment.id,
        orderId: order.id,
        redirect: `/aprender/${course.slug}`,
      });
    }

    return NextResponse.json({
      status: mapped,
      paymentId: payment.id,
      orderId: order.id,
      statusDetail: payment.status_detail,
      redirect:
        mapped === "pending" || mapped === "in_process"
          ? `/checkout/${course.slug}/resultado?status=pending&orderId=${order.id}`
          : `/checkout/${course.slug}/resultado?status=failure&orderId=${order.id}`,
    });
  } catch (err) {
    console.error("process payment error", err);
    if (err instanceof z.ZodError) {
      return NextResponse.json({ error: "Solicitud inválida" }, { status: 400 });
    }
    return NextResponse.json(
      { error: "No se pudo procesar el pago" },
      { status: 500 },
    );
  }
}
