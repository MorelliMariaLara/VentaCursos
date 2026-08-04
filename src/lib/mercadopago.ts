import { MercadoPagoConfig, Payment, Preference } from "mercadopago";

export function isMercadoPagoConfigured(): boolean {
  return Boolean(
    process.env.MP_ACCESS_TOKEN && process.env.NEXT_PUBLIC_MP_PUBLIC_KEY,
  );
}

export function allowSimulatePayments(): boolean {
  if (process.env.MP_ALLOW_SIMULATE === "true") return true;
  if (process.env.MP_ALLOW_SIMULATE === "false") return false;
  return process.env.NODE_ENV !== "production" && !isMercadoPagoConfigured();
}

export function getPublicKey(): string | null {
  return process.env.NEXT_PUBLIC_MP_PUBLIC_KEY ?? null;
}

function getClient() {
  const accessToken = process.env.MP_ACCESS_TOKEN;
  if (!accessToken) {
    throw new Error("MP_ACCESS_TOKEN_MISSING");
  }
  return new MercadoPagoConfig({
    accessToken,
    options: { timeout: 10000 },
  });
}

export async function createPreference(input: {
  orderId: string;
  title: string;
  amount: number;
  currency: string;
  payerEmail: string;
  courseId: string;
  slug: string;
}) {
  const client = getClient();
  const preference = new Preference(client);
  const appUrl = process.env.APP_URL ?? "http://localhost:3000";

  const result = await preference.create({
    body: {
      items: [
        {
          id: input.courseId,
          title: input.title,
          quantity: 1,
          unit_price: input.amount,
          currency_id: input.currency,
        },
      ],
      payer: {
        email: input.payerEmail,
      },
      external_reference: input.orderId,
      purpose: "wallet_purchase",
      metadata: {
        order_id: input.orderId,
        course_id: input.courseId,
        slug: input.slug,
      },
      back_urls: {
        success: `${appUrl}/checkout/${input.slug}/resultado?status=success`,
        failure: `${appUrl}/checkout/${input.slug}/resultado?status=failure`,
        pending: `${appUrl}/checkout/${input.slug}/resultado?status=pending`,
      },
      auto_return: "approved",
      notification_url: process.env.MP_WEBHOOK_URL || undefined,
    },
  });

  return result;
}

export async function createPayment(
  formData: Record<string, unknown>,
  options: { idempotencyKey: string },
) {
  const client = getClient();
  const payment = new Payment(client);
  return payment.create({
    body: formData as never,
    requestOptions: {
      idempotencyKey: options.idempotencyKey,
    },
  });
}

export async function getPayment(paymentId: string | number) {
  const client = getClient();
  const payment = new Payment(client);
  return payment.get({ id: paymentId });
}

export function mapMpStatusToOrderStatus(
  status?: string | null,
): "paid" | "pending" | "failed" | "rejected" | "in_process" | "cancelled" | "refunded" {
  switch (status) {
    case "approved":
      return "paid";
    case "pending":
    case "authorized":
      return "pending";
    case "in_process":
    case "in_mediation":
      return "in_process";
    case "rejected":
      return "rejected";
    case "cancelled":
      return "cancelled";
    case "refunded":
    case "charged_back":
      return "refunded";
    default:
      return "failed";
  }
}
