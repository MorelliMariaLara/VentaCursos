#!/usr/bin/env node
/**
 * Setup multiplataforma (Windows / macOS / Linux) para NEXA Web.
 */
import { execSync } from "node:child_process";
import {
  copyFileSync,
  existsSync,
  mkdirSync,
  writeFileSync,
} from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
process.chdir(root);

function run(cmd) {
  console.log(`> ${cmd}`);
  execSync(cmd, { stdio: "inherit", shell: true });
}

console.log("==> NEXA Web · setup");

const major = Number(process.versions.node.split(".")[0]);
if (major < 20) {
  console.error(
    `ERROR: Node.js 20+ requerido. Tenés ${process.version}. Descargá LTS en https://nodejs.org`,
  );
  process.exit(1);
}

if (!existsSync(".env")) {
  copyFileSync(".env.example", ".env");
  console.log("Creado .env");
}
if (!existsSync(".env.local")) {
  copyFileSync(".env.example", ".env.local");
  console.log("Creado .env.local");
}

mkdirSync("data", { recursive: true });
mkdirSync(join("content", "videos"), { recursive: true });

if (!existsSync("node_modules")) {
  console.log("Instalando dependencias (npm install)...");
  try {
    run("npm install");
  } catch {
    console.log("Reintentando con --legacy-peer-deps...");
    run("npm install --legacy-peer-deps");
  }
} else {
  console.log("node_modules ya existe");
}

const video = join("content", "videos", "lesson-a.mp4");
if (!existsSync(video)) {
  console.log(
    "Aviso: faltan videos demo en content/videos. Están en el repo; hacé git pull.",
  );
  writeFileSync(
    join("content", "videos", ".gitkeep"),
    "",
    { flag: "a" },
  );
}

console.log("");
console.log("Setup listo.");
console.log("  npm run dev     → http://localhost:3000");
console.log("  Alumno: demo@nexa.academy / demo1234");
console.log("  Admin:  admin@nexa.academy / admin1234");
