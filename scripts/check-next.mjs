import { existsSync } from "node:fs";
import { join } from "node:path";

const nextBin = join(process.cwd(), "node_modules", "next", "dist", "bin", "next");

if (!existsSync(nextBin)) {
  console.error("");
  console.error("ERROR: Next.js no esta instalado en este proyecto.");
  console.error("");
  console.error("Antes de 'npm run dev' tenes que instalar dependencias.");
  console.error("En PowerShell, en la carpeta del repo:");
  console.error("");
  console.error('  Set-ExecutionPolicy -Scope Process Bypass');
  console.error("  .\\reparar.ps1");
  console.error("");
  console.error("O doble clic en install.bat");
  console.error("");
  process.exit(1);
}
