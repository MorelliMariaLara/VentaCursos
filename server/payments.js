function isMercadoPagoConfigured() {
  return Boolean(process.env.MP_ACCESS_TOKEN && process.env.MP_PUBLIC_KEY);
}

function allowSimulatePayments() {
  if (process.env.MP_ALLOW_SIMULATE === "true") return true;
  if (process.env.MP_ALLOW_SIMULATE === "false") return false;
  return !isMercadoPagoConfigured();
}

function getPublicKey() {
  return process.env.MP_PUBLIC_KEY || null;
}

async function mpFetch(pathname, { method = "GET", body, idempotencyKey } = {}) {
  const token = process.env.MP_ACCESS_TOKEN;
  if (!token) throw new Error("MP_ACCESS_TOKEN_MISSING");
  const headers = {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
  if (idempotencyKey) headers["X-Idempotency-Key"] = idempotencyKey;
  const res = await fetch(`https://api.mercadopago.com${pathname}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(data.message || "MP_API_ERROR");
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

async function createPreference(input) {
  const appUrl = process.env.APP_URL || "http://localhost:3000";
  return mpFetch("/checkout/preferences", {
    method: "POST",
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
      payer: { email: input.payerEmail },
      external_reference: input.orderId,
      metadata: {
        order_id: input.orderId,
        course_id: input.courseId,
        slug: input.slug,
      },
      back_urls: {
        success: `${appUrl}/checkout.html?slug=${input.slug}&status=success`,
        failure: `${appUrl}/checkout.html?slug=${input.slug}&status=failure`,
        pending: `${appUrl}/checkout.html?slug=${input.slug}&status=pending`,
      },
      auto_return: "approved",
      notification_url: process.env.MP_WEBHOOK_URL || undefined,
    },
  });
}

async function createPayment(formData, idempotencyKey) {
  return mpFetch("/v1/payments", {
    method: "POST",
    body: formData,
    idempotencyKey,
  });
}

async function getPayment(paymentId) {
  return mpFetch(`/v1/payments/${paymentId}`);
}

function mapMpStatus(status) {
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

module.exports = {
  isMercadoPagoConfigured,
  allowSimulatePayments,
  getPublicKey,
  createPreference,
  createPayment,
  getPayment,
  mapMpStatus,
};
