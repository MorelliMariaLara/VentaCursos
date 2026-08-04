import { NextRequest, NextResponse } from "next/server";
import { findLesson, getCourseById, getEnrollment } from "@/lib/db";
import { encryptChunk, verifyStreamToken } from "@/lib/video-crypto";

export const runtime = "nodejs";

async function fetchUpstream(
  url: string,
  range?: string | null,
): Promise<Response> {
  const headers: HeadersInit = {};
  if (range) headers.Range = range;
  const res = await fetch(url, { headers, cache: "no-store" });
  return res;
}

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
  let upstream: Response;
  try {
    upstream = await fetchUpstream(found.lesson.sourceUrl, range);
  } catch {
    return NextResponse.json(
      { error: "No se pudo obtener el video fuente" },
      { status: 502 },
    );
  }

  if (!upstream.ok && upstream.status !== 206) {
    return NextResponse.json(
      { error: "Fuente de video no disponible" },
      { status: 502 },
    );
  }

  const key = Buffer.from(claims.keyB64, "base64");
  const iv = Buffer.from(claims.ivB64, "base64");

  const contentRange = upstream.headers.get("content-range");
  let byteOffset = 0;
  if (contentRange) {
    const match = /bytes\s+(\d+)-/i.exec(contentRange);
    if (match) byteOffset = Number(match[1]);
  } else if (range) {
    const match = /bytes=(\d+)-/i.exec(range);
    if (match) byteOffset = Number(match[1]);
  }

  const plain = Buffer.from(await upstream.arrayBuffer());
  const encrypted = encryptChunk(plain, key, iv, byteOffset);

  const headers = new Headers();
  headers.set("Content-Type", "application/octet-stream");
  headers.set("X-Nexa-Encrypted", "AES-256-CTR");
  headers.set("X-Nexa-Byte-Offset", String(byteOffset));
  headers.set("Cache-Control", "no-store, no-cache, private");
  headers.set("Accept-Ranges", "bytes");
  headers.set("X-Content-Type-Options", "nosniff");

  const contentLength = upstream.headers.get("content-length");
  if (contentLength) headers.set("Content-Length", contentLength);
  if (contentRange) {
    headers.set("Content-Range", contentRange);
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
