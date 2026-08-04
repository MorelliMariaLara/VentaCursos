import { NextResponse } from "next/server";
import { z } from "zod";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, markLessonComplete } from "@/lib/db";

const schema = z.object({
  slug: z.string().min(1),
  lessonId: z.string().min(1),
});

export async function POST(req: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = schema.parse(await req.json());
    const course = await getCourseBySlug(body.slug);
    if (!course) {
      return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
    }
    const enrollment = await markLessonComplete(
      session.sub,
      course.id,
      body.lessonId,
    );
    return NextResponse.json({
      progress: enrollment.progress,
      certificateCode: enrollment.certificateCode ?? null,
      certificateIssuedAt: enrollment.certificateIssuedAt ?? null,
    });
  } catch (err) {
    if (err instanceof Error && err.message === "NOT_ENROLLED") {
      return NextResponse.json({ error: "No estás inscrito" }, { status: 403 });
    }
    return NextResponse.json({ error: "Error al guardar progreso" }, { status: 500 });
  }
}
