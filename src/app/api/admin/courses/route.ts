import { NextResponse } from "next/server";
import { z } from "zod";
import { requireAdmin } from "@/lib/admin";
import { deleteCourse, listCourses, upsertCourse } from "@/lib/db";

const lessonSchema = z.object({
  id: z.string(),
  title: z.string(),
  durationMinutes: z.number(),
  sourceUrl: z.string(),
  order: z.number(),
});

const moduleSchema = z.object({
  id: z.string(),
  title: z.string(),
  lessons: z.array(lessonSchema),
});

const courseSchema = z.object({
  id: z.string().optional(),
  slug: z.string().min(2),
  title: z.string().min(2),
  subtitle: z.string().optional(),
  description: z.string().optional(),
  category: z.string().optional(),
  level: z.enum(["Inicial", "Intermedio", "Avanzado"]).optional(),
  price: z.number().positive(),
  currency: z.enum(["ARS", "USD"]).optional(),
  durationHours: z.number().optional(),
  includesCertificate: z.boolean().optional(),
  certificateName: z.string().optional(),
  thumbnailGradient: z.string().optional(),
  instructor: z.string().optional(),
  learningOutcomes: z.array(z.string()).optional(),
  modules: z.array(moduleSchema).optional(),
  published: z.boolean().optional(),
});

export async function GET() {
  try {
    await requireAdmin();
    const courses = await listCourses({ includeUnpublished: true });
    return NextResponse.json({ courses });
  } catch (err) {
    if (err instanceof Error && err.message === "UNAUTHORIZED") {
      return NextResponse.json({ error: "No autorizado" }, { status: 401 });
    }
    if (err instanceof Error && err.message === "FORBIDDEN") {
      return NextResponse.json({ error: "Solo administradores" }, { status: 403 });
    }
    return NextResponse.json({ error: "Error" }, { status: 500 });
  }
}

export async function POST(req: Request) {
  try {
    await requireAdmin();
    const body = courseSchema.parse(await req.json());
    const course = await upsertCourse(body);
    return NextResponse.json({ course });
  } catch (err) {
    if (err instanceof z.ZodError) {
      return NextResponse.json({ error: "Datos inválidos", details: err.flatten() }, { status: 400 });
    }
    if (err instanceof Error && err.message === "SLUG_TAKEN") {
      return NextResponse.json({ error: "El slug ya existe" }, { status: 409 });
    }
    if (err instanceof Error && err.message === "UNAUTHORIZED") {
      return NextResponse.json({ error: "No autorizado" }, { status: 401 });
    }
    if (err instanceof Error && err.message === "FORBIDDEN") {
      return NextResponse.json({ error: "Solo administradores" }, { status: 403 });
    }
    return NextResponse.json({ error: "No se pudo guardar" }, { status: 500 });
  }
}

export async function DELETE(req: Request) {
  try {
    await requireAdmin();
    const { searchParams } = new URL(req.url);
    const id = searchParams.get("id");
    if (!id) {
      return NextResponse.json({ error: "id requerido" }, { status: 400 });
    }
    await deleteCourse(id);
    return NextResponse.json({ ok: true });
  } catch (err) {
    if (err instanceof Error && err.message === "COURSE_NOT_FOUND") {
      return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
    }
    if (err instanceof Error && err.message === "UNAUTHORIZED") {
      return NextResponse.json({ error: "No autorizado" }, { status: 401 });
    }
    if (err instanceof Error && err.message === "FORBIDDEN") {
      return NextResponse.json({ error: "Solo administradores" }, { status: 403 });
    }
    return NextResponse.json({ error: "No se pudo eliminar" }, { status: 500 });
  }
}
