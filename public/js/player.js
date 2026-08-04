async function decryptBytes(encrypted, keyB64, ivB64, byteOffset = 0) {
  const keyRaw = Uint8Array.from(atob(keyB64), (c) => c.charCodeAt(0));
  const ivRaw = Uint8Array.from(atob(ivB64), (c) => c.charCodeAt(0));
  const counter = new Uint8Array(ivRaw);
  let carry = Math.floor(byteOffset / 16);
  for (let i = 15; i >= 0 && carry > 0; i -= 1) {
    const sum = counter[i] + (carry & 0xff);
    counter[i] = sum & 0xff;
    carry = (carry >>> 8) + (sum > 255 ? 1 : 0);
  }
  const cryptoKey = await crypto.subtle.importKey("raw", keyRaw, { name: "AES-CTR" }, false, ["decrypt"]);
  const prefix = byteOffset % 16;
  let input = encrypted;
  if (prefix) {
    const padded = new Uint8Array(prefix + encrypted.byteLength);
    padded.set(new Uint8Array(encrypted), prefix);
    input = padded;
  }
  const plain = await crypto.subtle.decrypt({ name: "AES-CTR", counter, length: 128 }, cryptoKey, input);
  return prefix ? plain.slice(prefix) : plain;
}

async function loadEncryptedVideo(videoEl, session) {
  const res = await fetch(session.mediaUrl, { credentials: "same-origin" });
  if (!res.ok) throw new Error("No se pudo cargar el video");
  const buf = await res.arrayBuffer();
  const plain = await decryptBytes(buf, session.keyB64, session.ivB64, 0);
  const blob = new Blob([plain], { type: "video/mp4" });
  const url = URL.createObjectURL(blob);
  if (videoEl.dataset.objectUrl) URL.revokeObjectURL(videoEl.dataset.objectUrl);
  videoEl.dataset.objectUrl = url;
  videoEl.src = url;
}
