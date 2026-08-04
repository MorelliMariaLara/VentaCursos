const { randomBytes, scrypt, timingSafeEqual } = require("crypto");
const { promisify } = require("util");

const scryptAsync = promisify(scrypt);

async function hashPassword(password) {
  const salt = randomBytes(16);
  const hash = await scryptAsync(String(password), salt, 32);
  return `scrypt$${salt.toString("hex")}$${Buffer.from(hash).toString("hex")}`;
}

async function verifyPassword(password, stored) {
  if (!stored || !String(stored).startsWith("scrypt$")) return false;
  const parts = String(stored).split("$");
  if (parts.length !== 3) return false;
  const salt = Buffer.from(parts[1], "hex");
  const expected = Buffer.from(parts[2], "hex");
  const actual = Buffer.from(await scryptAsync(String(password), salt, 32));
  if (actual.length !== expected.length) return false;
  return timingSafeEqual(actual, expected);
}

module.exports = { hashPassword, verifyPassword };
