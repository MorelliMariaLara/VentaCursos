"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { initMercadoPago, Payment, StatusScreen } from "@mercadopago/sdk-react";
import { formatPrice } from "@/lib/format";

type PrefResponse = {
  orderId: string;
  amount: number;
  currency: string;
  simulate: boolean;
  publicKey: string | null;
  preferenceId: string | null;
  course: { id: string; slug: string; title: string };
  error?: string;
};

export function MercadoPagoCheckout({ slug }: { slug: string }) {
  const router = useRouter();
  const [pref, setPref] = useState<PrefResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [paymentId, setPaymentId] = useState<string | null>(null);
  const [simulating, setSimulating] = useState(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function boot() {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch("/api/payments/preference", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slug }),
        });
        const data = (await res.json()) as PrefResponse & { redirect?: string };
        if (res.status === 401) {
          router.push(`/login?next=/checkout/${slug}`);
          return;
        }
        if (res.status === 409 && data.redirect) {
          router.push(data.redirect);
          return;
        }
        if (!res.ok) {
          throw new Error(data.error ?? "No se pudo iniciar el pago");
        }
        if (cancelled) return;
        setPref(data);
        if (data.publicKey) {
          initMercadoPago(data.publicKey, { locale: "es-AR" });
          setReady(true);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Error de checkout");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    boot();
    return () => {
      cancelled = true;
    };
  }, [slug, router]);

  const initialization = useMemo(() => {
    if (!pref) return null;
    return {
      amount: pref.amount,
      preferenceId: pref.preferenceId ?? undefined,
      payer: {
        email: undefined as string | undefined,
      },
    };
  }, [pref]);

  async function simulatePay() {
    if (!pref) return;
    setSimulating(true);
    setError(null);
    const res = await fetch("/api/payments/process", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ orderId: pref.orderId, simulate: true }),
    });
    const data = await res.json().catch(() => ({}));
    setSimulating(false);
    if (!res.ok) {
      setError(data.error ?? "No se pudo simular el pago");
      return;
    }
    router.push(data.redirect ?? `/aprender/${slug}`);
    router.refresh();
  }

  if (loading) {
    return (
      <div className="checkout-box">
        <p>Preparando Checkout Bricks de Mercado Pago…</p>
      </div>
    );
  }

  if (error && !pref) {
    return (
      <div className="checkout-box">
        <p className="form-error">{error}</p>
      </div>
    );
  }

  if (!pref) return null;

  return (
    <div className="checkout-layout">
      <div className="checkout-summary">
        <p className="eyebrow">Checkout seguro</p>
        <h1 className="page-title">{pref.course.title}</h1>
        <p className="price">
          {formatPrice(pref.amount, pref.currency)}
        </p>
        <p className="muted">
          Pagás con Mercado Pago Checkout Bricks. El acceso al aula se habilita
          cuando el pago está aprobado.
        </p>
        <p className="muted" style={{ fontSize: "0.85rem" }}>
          Orden: {pref.orderId}
        </p>
      </div>

      <div className="checkout-box checkout-brick">
        {pref.simulate ? (
          <>
            <p className="eyebrow">Modo simulación</p>
            <p className="muted">
              No hay credenciales de Mercado Pago configuradas. En desarrollo
              podés aprobar el pago de prueba para continuar el flujo.
            </p>
            <p className="muted" style={{ fontSize: "0.9rem" }}>
              Para cobros reales configurá{" "}
              <code>MP_ACCESS_TOKEN</code> y{" "}
              <code>NEXT_PUBLIC_MP_PUBLIC_KEY</code>.
            </p>
            <button
              type="button"
              className="btn"
              onClick={simulatePay}
              disabled={simulating}
            >
              {simulating ? "Aprobando…" : "Simular pago aprobado"}
            </button>
            {error && <p className="form-error">{error}</p>}
          </>
        ) : (
          <>
            {paymentId ? (
              <StatusScreen
                initialization={{ paymentId }}
                onError={(err) => console.error(err)}
              />
            ) : (
              ready &&
              initialization && (
                <Payment
                  initialization={initialization}
                  customization={{
                    paymentMethods: {
                      maxInstallments: 12,
                      creditCard: "all",
                      debitCard: "all",
                      ticket: "all",
                      bankTransfer: "all",
                      mercadoPago: "all",
                    },
                  }}
                  onSubmit={async ({ formData }) => {
                    const res = await fetch("/api/payments/process", {
                      method: "POST",
                      headers: { "Content-Type": "application/json" },
                      body: JSON.stringify({
                        orderId: pref.orderId,
                        formData,
                      }),
                    });
                    const data = await res.json().catch(() => ({}));
                    if (!res.ok) {
                      throw new Error(data.error ?? "Pago rechazado");
                    }
                    if (data.paymentId) {
                      setPaymentId(String(data.paymentId));
                    }
                    if (data.status === "paid" && data.redirect) {
                      router.push(data.redirect);
                      router.refresh();
                    } else if (data.redirect) {
                      router.push(data.redirect);
                    }
                  }}
                  onReady={() => undefined}
                  onError={(err) => {
                    console.error(err);
                    setError("Hubo un problema con el Brick de pago");
                  }}
                />
              )
            )}
            {error && <p className="form-error">{error}</p>}
          </>
        )}
      </div>
    </div>
  );
}
