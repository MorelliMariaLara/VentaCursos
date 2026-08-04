const fs = require("fs/promises");
const path = require("path");
const { createCipheriv, randomBytes, createHmac } = require("crypto");
const { sign, verify } = require("./jwt");

const VIDEO_ROOT = path.join(process.cwd(), "content", "videos");

function streamSecret() {
  return process.env.STREAM_SECRET || "nexa-stream-secret-change-me";
}

function generateContentKey() {
  return { key: randomBytes(32), iv: randomBytes(16) };
}

function createStreamToken({ userId, courseId, lessonId, key, iv }) {
  return sign(
    {
      sub: userId,
      courseId,
      lessonId,
      keyB64: key.toString("base64"),
      ivB64: iv.toString("base64"),
    },
    streamSecret(),
    2 * 60 * 60,
  );
}

function verifyStreamToken(token) {
  const payload = verify(token, streamSecret());
  if (
    !payload?.sub ||
    !payload.courseId ||
    !payload.lessonId ||
    !payload.keyB64 ||
    !payload.ivB64
  ) {
    return null;
  }
  return payload;
}

function encryptChunk(plain, key, iv, byteOffset) {
  const counter = Buffer.from(iv);
  let carry = Math.floor(byteOffset / 16);
  for (let i = 15; i >= 0 && carry > 0; i -= 1) {
    const sum = counter[i] + (carry & 0xff);
    counter[i] = sum & 0xff;
    carry = (carry >>> 8) + (sum > 0xff ? 1 : 0);
  }
  const cipher = createCipheriv("aes-256-ctr", key, counter);
  const prefix = byteOffset % 16;
  if (prefix === 0) return Buffer.concat([cipher.update(plain), cipher.final()]);
  const pad = Buffer.alloc(prefix);
  const mixed = Buffer.concat([cipher.update(Buffer.concat([pad, plain])), cipher.final()]);
  return mixed.subarray(prefix);
}

function watermarkFingerprint(userId, email) {
  return createHmac("sha256", "nexa-wm")
    .update(`${userId}:${email}`)
    .digest("hex")
    .slice(0, 12)
    .toUpperCase();
}

async function readVideoSource(sourceUrl, rangeHeader) {
  if (!sourceUrl.startsWith("local:")) {
    throw new Error("ONLY_LOCAL_SUPPORTED");
  }
  const fileName = sourceUrl.slice("local:".length);
  if (!fileName || fileName.includes("..") || fileName.includes("/") || fileName.includes("\\")) {
    throw new Error("INVALID_LOCAL_SOURCE");
  }
  const file = await fs.readFile(path.join(VIDEO_ROOT, fileName));
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
        start,
      };
    }
  }
  return { body: file, status: 200, contentLength: totalSize, totalSize, start: 0 };
}

module.exports = {
  generateContentKey,
  createStreamToken,
  verifyStreamToken,
  encryptChunk,
  watermarkFingerprint,
  readVideoSource,
};
