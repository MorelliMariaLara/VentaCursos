import { NextResponse } from "next/server";
import { z } from "zod";
import { getSession } from "@/lib/auth";
import { findLesson, findUserById, getCourseBySlug, getEnrollment } from "@/lib/db";
import {
  createStreamToken,
  generateContentKey,
  watermarkFingerprint,
} from "@/lib/video-crypto";

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

    const enrollment = await getEnrollment(session.sub, course.id);
    if (!enrollment) {
      return NextResponse.json(
        { error: "Debés comprar el curso para ver el video" },
        { status: 403 },
      );
    }

    const found = findLesson(course, body.lessonId);
    if (!found) {
      return NextResponse.json({ error: "Lección no encontrada" }, { status: 404 });
    }

    const user = await findUserById(session.sub);
    const { key, iv } = generateContentKey();
    const streamToken = await createStreamToken({
      userId: session.sub,
      courseId: course.id,
      lessonId: body.lessonId,
      key,
      iv,
    });

    return NextResponse.json({
      streamToken,
      contentKey: key.toString("base64"),
      contentIv: iv.toString("base64"),
      algorithm: "AES-256-CTR",
      watermark: {
        label: user?.email ?? session.email,
        code: watermarkFingerprint(session.sub, session.email),
      },
      lesson: {
        id: found.lesson.id,
        title: found.lesson.title,
        moduleTitle: found.moduleTitle,
        durationMinutes: found.lesson.durationMinutes,
      },
    });
  } catch {
    return NextResponse.json(
      { error: "No se pudo crear la sesión de video" },
      { status: 500 },
    );
  }
}
