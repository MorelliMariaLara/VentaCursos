import { NextResponse } from "next/server";
import { z } from "zod";

const schema = z.object({
  slug: z.string().min(1),
});

/** Legacy endpoint: redirects clients to Mercado Pago Checkout Bricks page. */
export async function POST(req: Request) {
  try {
    const { slug } = schema.parse(await req.json());
    return NextResponse.json({
      ok: true,
      redirect: `/checkout/${slug}`,
    });
  } catch {
    return NextResponse.json({ error: "Solicitud inválida" }, { status: 400 });
  }
}
