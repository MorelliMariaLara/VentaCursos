import { NextResponse } from "next/server";
import { z } from "zod";
import { createUser } from "@/lib/db";
import { createSessionToken, setSessionCookie } from "@/lib/auth";

const schema = z.object({
  name: z.string().min(2).max(80),
  email: z.string().email(),
  password: z.string().min(8).max(100),
});

export async function POST(req: Request) {
  try {
    const body = schema.parse(await req.json());
    const user = await createUser(body);
    const token = await createSessionToken(user);
    await setSessionCookie(token);
    return NextResponse.json({
      user: { id: user.id, name: user.name, email: user.email },
    });
  } catch (err) {
    if (err instanceof z.ZodError) {
      return NextResponse.json({ error: "Datos inválidos" }, { status: 400 });
    }
    if (err instanceof Error && err.message === "EMAIL_TAKEN") {
      return NextResponse.json(
        { error: "Ese correo ya está registrado" },
        { status: 409 },
      );
    }
    return NextResponse.json({ error: "No se pudo registrar" }, { status: 500 });
  }
}
