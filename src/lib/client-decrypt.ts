function counterFromIv(iv: Uint8Array, blockOffset: number): Uint8Array<ArrayBuffer> {
  const counter = new Uint8Array(iv) as Uint8Array<ArrayBuffer>;
  let carry = blockOffset;
  for (let i = 15; i >= 0 && carry > 0; i -= 1) {
    const sum = counter[i] + (carry & 0xff);
    counter[i] = sum & 0xff;
    carry = Math.floor(carry / 256) + (sum > 0xff ? 1 : 0);
  }
  return counter;
}

export async function decryptAesCtr(
  encrypted: ArrayBuffer,
  keyB64: string,
  ivB64: string,
  byteOffset = 0,
): Promise<ArrayBuffer> {
  const keyBytes = Uint8Array.from(atob(keyB64), (c) => c.charCodeAt(0));
  const ivBytes = Uint8Array.from(atob(ivB64), (c) => c.charCodeAt(0));
  const cryptoKey = await crypto.subtle.importKey(
    "raw",
    keyBytes,
    { name: "AES-CTR" },
    false,
    ["decrypt"],
  );

  const prefix = byteOffset % 16;
  const blockOffset = Math.floor(byteOffset / 16);
  const counter = counterFromIv(ivBytes, blockOffset);

  if (prefix === 0) {
    return crypto.subtle.decrypt(
      { name: "AES-CTR", counter, length: 128 },
      cryptoKey,
      encrypted,
    );
  }

  const enc = new Uint8Array(encrypted);
  const padded = new Uint8Array(prefix + enc.length);
  padded.set(enc, prefix);
  const decrypted = new Uint8Array(
    await crypto.subtle.decrypt(
      { name: "AES-CTR", counter, length: 128 },
      cryptoKey,
      padded,
    ),
  );
  return decrypted.subarray(prefix).buffer.slice(
    decrypted.byteOffset,
    decrypted.byteOffset + decrypted.byteLength,
  );
}
