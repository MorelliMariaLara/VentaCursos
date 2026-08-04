import { NextRequest, NextResponse } from "next/server";
import { findLesson, getCourseById, getEnrollment } from "@/lib/db";
import { encryptChunk, verifyStreamToken } from "@/lib/video-crypto";
import { readVideoSource } from "@/lib/video-source";

export const runtime = "nodejs";

export async function GET(req: NextRequest) {
  const token = req.nextUrl.searchParams.get("token");
  if (!token) {
    return NextResponse.json({ error: "Token requerido" }, { status: 400 });
  }

  const claims = await verifyStreamToken(token);
  if (!claims) {
    return NextResponse.json({ error: "Sesión de video inválida" }, { status: 401 });
  }

  const course = await getCourseById(claims.courseId);
  if (!course) {
    return NextResponse.json({ error: "Curso no encontrado" }, { status: 404 });
  }

  const enrollment = await getEnrollment(claims.sub, claims.courseId);
  if (!enrollment) {
    return NextResponse.json({ error: "Sin acceso" }, { status: 403 });
  }

  const found = findLesson(course, claims.lessonId);
  if (!found) {
    return NextResponse.json({ error: "Lección no encontrada" }, { status: 404 });
  }

  const range = req.headers.get("range");
  let source;
  try {
    source = await readVideoSource(found.lesson.sourceUrl, range);
  } catch {
    return NextResponse.json(
      { error: "Fuente de video no disponible" },
      { status: 502 },
    );
  }

  const key = Buffer.from(claims.keyB64, "base64");
  const iv = Buffer.from(claims.ivB64, "base64");

  let byteOffset = 0;
  if (source.contentRange) {
    const match = /bytes\s+(\d+)-/i.exec(source.contentRange);
    if (match) byteOffset = Number(match[1]);
  } else if (range) {
    const match = /bytes=(\d+)-/i.exec(range);
    if (match) byteOffset = Number(match[1]);
  }

  const encrypted = encryptChunk(source.body, key, iv, byteOffset);

  const headers = new Headers();
  headers.set("Content-Type", "application/octet-stream");
  headers.set("X-Nexa-Encrypted", "AES-256-CTR");
  headers.set("X-Nexa-Byte-Offset", String(byteOffset));
  headers.set("Cache-Control", "no-store, no-cache, private");
  headers.set("Accept-Ranges", "bytes");
  headers.set("X-Content-Type-Options", "nosniff");
  headers.set("Content-Length", String(encrypted.length));

  if (source.contentRange) {
    headers.set("Content-Range", source.contentRange);
    return new NextResponse(new Uint8Array(encrypted), {
      status: 206,
      headers,
    });
  }

  return new NextResponse(new Uint8Array(encrypted), {
    status: 200,
    headers,
  });
}
