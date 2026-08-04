"use client";

import { useRouter } from "next/navigation";

export function BuyButton({
  slug,
  label = "Comprar con Mercado Pago",
}: {
  slug: string;
  label?: string;
}) {
  const router = useRouter();

  return (
    <div className="buy-block">
      <button
        type="button"
        className="btn"
        onClick={() => router.push(`/checkout/${slug}`)}
      >
        {label}
      </button>
    </div>
  );
}
