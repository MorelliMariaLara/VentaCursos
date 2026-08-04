import { NextRequest, NextResponse } from "next/server";
import {
  fulfillPaidOrder,
  getOrderById,
  updateOrder,
} from "@/lib/db";
import {
  getPayment,
  isMercadoPagoConfigured,
  mapMpStatusToOrderStatus,
} from "@/lib/mercadopago";

export async function POST(req: NextRequest) {
  if (!isMercadoPagoConfigured()) {
    return NextResponse.json({ ok: true, skipped: true });
  }

  try {
    const body = await req.json().catch(() => ({}));
    const type = body.type ?? body.action ?? req.nextUrl.searchParams.get("type");
    const dataId =
      body?.data?.id ??
      req.nextUrl.searchParams.get("data.id") ??
      req.nextUrl.searchParams.get("id");

    if (!dataId) {
      return NextResponse.json({ ok: true });
    }

    // payment notifications
    if (
      type === "payment" ||
      String(type).includes("payment") ||
      !type
    ) {
      const payment = await getPayment(dataId);
      const orderId = payment.external_reference;
      if (!orderId) {
        return NextResponse.json({ ok: true });
      }

      const order = await getOrderById(orderId);
      if (!order) {
        return NextResponse.json({ ok: true });
      }

      const mapped = mapMpStatusToOrderStatus(payment.status);
      await updateOrder(order.id, {
        status: mapped === "paid" ? order.status : mapped,
        paymentId: payment.id ? String(payment.id) : undefined,
        paymentMethod: payment.payment_method_id,
        statusDetail: payment.status_detail,
        payerEmail: payment.payer?.email,
      });

      if (mapped === "paid" && order.status !== "paid") {
        await fulfillPaidOrder(order.id);
      }
    }

    return NextResponse.json({ ok: true });
  } catch (err) {
    console.error("webhook error", err);
    return NextResponse.json({ ok: false }, { status: 500 });
  }
}

export async function GET() {
  return NextResponse.json({ ok: true, service: "mercadopago-webhook" });
}
