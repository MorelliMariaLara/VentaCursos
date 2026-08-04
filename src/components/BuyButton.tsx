"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

export function BuyButton({
  slug,
  label = "Comprar acceso",
}: {
  slug: string;
  label?: string;
}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function buy() {
    setLoading(true);
    setError(null);
    const res = await fetch("/api/checkout", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ slug }),
    });
    const data = await res.json().catch(() => ({}));
    setLoading(false);
    if (res.status === 401) {
      router.push(`/login?next=/cursos/${slug}`);
      return;
    }
    if (!res.ok) {
      setError(data.error ?? "No se pudo comprar");
      return;
    }
    router.push(data.redirect ?? `/aprender/${slug}`);
    router.refresh();
  }

  return (
    <div className="buy-block">
      <button type="button" className="btn" onClick={buy} disabled={loading}>
        {loading ? "Procesando…" : label}
      </button>
      {error && <p className="form-error">{error}</p>}
    </div>
  );
}
