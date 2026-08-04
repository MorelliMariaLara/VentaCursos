import { createCipheriv, randomBytes, createHmac } from "crypto";
import { SignJWT, jwtVerify } from "jose";

const STREAM_SECRET = () =>
  new TextEncoder().encode(
    process.env.STREAM_SECRET ?? "nexa-stream-secret-change-me",
  );

export interface StreamSessionClaims {
  sub: string;
  courseId: string;
  lessonId: string;
  keyB64: string;
  ivB64: string;
}

export function generateContentKey() {
  return {
    key: randomBytes(32),
    iv: randomBytes(16),
  };
}

export async function createStreamToken(input: {
  userId: string;
  courseId: string;
  lessonId: string;
  key: Buffer;
  iv: Buffer;
}): Promise<string> {
  return new SignJWT({
    courseId: input.courseId,
    lessonId: input.lessonId,
    keyB64: input.key.toString("base64"),
    ivB64: input.iv.toString("base64"),
  })
    .setProtectedHeader({ alg: "HS256" })
    .setSubject(input.userId)
    .setIssuedAt()
    .setExpirationTime("2h")
    .sign(STREAM_SECRET());
}

export async function verifyStreamToken(
  token: string,
): Promise<StreamSessionClaims | null> {
  try {
    const { payload } = await jwtVerify(token, STREAM_SECRET());
    if (
      !payload.sub ||
      typeof payload.courseId !== "string" ||
      typeof payload.lessonId !== "string" ||
      typeof payload.keyB64 !== "string" ||
      typeof payload.ivB64 !== "string"
    ) {
      return null;
    }
    return {
      sub: payload.sub,
      courseId: payload.courseId,
      lessonId: payload.lessonId,
      keyB64: payload.keyB64,
      ivB64: payload.ivB64,
    };
  } catch {
    return null;
  }
}

/** AES-256-CTR encrypt a buffer. Counter is derived from base IV + block offset. */
export function encryptChunk(
  plain: Buffer,
  key: Buffer,
  iv: Buffer,
  byteOffset: number,
): Buffer {
  const counter = Buffer.from(iv);
  const blockOffset = Math.floor(byteOffset / 16);
  // increment 128-bit counter (big-endian) by blockOffset
  let carry = blockOffset;
  for (let i = 15; i >= 0 && carry > 0; i -= 1) {
    const sum = counter[i] + (carry & 0xff);
    counter[i] = sum & 0xff;
    carry = (carry >>> 8) + (sum > 0xff ? 1 : 0);
  }
  const cipher = createCipheriv("aes-256-ctr", key, counter);
  // If offset is not block-aligned, discard prefix keystream bytes
  const prefix = byteOffset % 16;
  if (prefix === 0) {
    return Buffer.concat([cipher.update(plain), cipher.final()]);
  }
  const pad = Buffer.alloc(prefix);
  const mixed = Buffer.concat([
    cipher.update(Buffer.concat([pad, plain])),
    cipher.final(),
  ]);
  return mixed.subarray(prefix);
}

export function decryptChunk(
  encrypted: Buffer,
  key: Buffer,
  iv: Buffer,
  byteOffset: number,
): Buffer {
  // CTR is symmetric
  return encryptChunk(encrypted, key, iv, byteOffset);
}

export function watermarkFingerprint(userId: string, email: string): string {
  return createHmac("sha256", "nexa-wm")
    .update(`${userId}:${email}`)
    .digest("hex")
    .slice(0, 12)
    .toUpperCase();
}
