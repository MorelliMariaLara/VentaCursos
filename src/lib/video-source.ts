import { promises as fs } from "fs";
import path from "path";

const VIDEO_ROOT = path.join(process.cwd(), "content", "videos");

export async function readVideoSource(
  sourceUrl: string,
  rangeHeader?: string | null,
): Promise<{
  body: Buffer;
  status: number;
  contentLength: number;
  contentRange?: string;
  totalSize: number;
}> {
  if (sourceUrl.startsWith("local:")) {
    const fileName = sourceUrl.slice("local:".length);
    if (
      !fileName ||
      fileName.includes("..") ||
      fileName.includes("/") ||
      fileName.includes("\\")
    ) {
      throw new Error("INVALID_LOCAL_SOURCE");
    }
    const fullPath = path.join(VIDEO_ROOT, fileName);
    const file = await fs.readFile(fullPath);
    const totalSize = file.length;

    if (rangeHeader) {
      const match = /bytes=(\d+)-(\d*)/i.exec(rangeHeader);
      if (match) {
        const start = Number(match[1]);
        const end = match[2] ? Number(match[2]) : totalSize - 1;
        const slice = file.subarray(start, end + 1);
        return {
          body: slice,
          status: 206,
          contentLength: slice.length,
          contentRange: `bytes ${start}-${end}/${totalSize}`,
          totalSize,
        };
      }
    }

    return {
      body: file,
      status: 200,
      contentLength: totalSize,
      totalSize,
    };
  }

  const headers: HeadersInit = {};
  if (rangeHeader) headers.Range = rangeHeader;
  const res = await fetch(sourceUrl, { headers, cache: "no-store" });
  if (!res.ok && res.status !== 206) {
    throw new Error("REMOTE_SOURCE_UNAVAILABLE");
  }
  const body = Buffer.from(await res.arrayBuffer());
  const contentRange = res.headers.get("content-range") ?? undefined;
  const contentLength = Number(
    res.headers.get("content-length") ?? body.length,
  );
  const totalFromRange = contentRange
    ? Number(/\/(\d+)$/.exec(contentRange)?.[1] ?? contentLength)
    : contentLength;

  return {
    body,
    status: res.status,
    contentLength,
    contentRange,
    totalSize: totalFromRange,
  };
}
