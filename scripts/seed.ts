/**
 * Reinicia / asegura el seed de la base local (usuarios demo + catálogo).
 * Uso: npm run seed
 */
import { promises as fs } from "fs";
import path from "path";

const DB_PATH = path.join(process.cwd(), "data", "store.json");

async function main() {
  await fs.mkdir(path.dirname(DB_PATH), { recursive: true });
  try {
    await fs.unlink(DB_PATH);
    console.log("Base anterior eliminada:", DB_PATH);
  } catch {
    console.log("No había base previa.");
  }

  // Import dinámico para forzar ensureDb/seed
  const { getDb, listUsers, listCourses } = await import("../src/lib/db");
  const db = await getDb();
  const users = await listUsers();
  const courses = await listCourses({ includeUnpublished: true });

  console.log("Seed OK");
  console.log(`  Usuarios: ${users.length}`);
  console.log(`  Cursos:   ${courses.length}`);
  console.log(`  Órdenes:  ${db.orders.length}`);
  console.log("");
  console.log("Cuentas:");
  console.log("  demo@nexa.academy  / demo1234");
  console.log("  admin@nexa.academy / admin1234");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
