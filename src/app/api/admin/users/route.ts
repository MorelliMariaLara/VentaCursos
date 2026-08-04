import { NextResponse } from "next/server";
import { requireAdmin } from "@/lib/admin";
import { listEnrollmentsForUser, listUsers } from "@/lib/db";

export async function GET() {
  try {
    await requireAdmin();
    const users = await listUsers();
    const enriched = await Promise.all(
      users.map(async (user) => {
        const enrollments = await listEnrollmentsForUser(user.id);
        return {
          id: user.id,
          name: user.name,
          email: user.email,
          role: user.role,
          createdAt: user.createdAt,
          coursesOwned: enrollments.length,
        };
      }),
    );
    return NextResponse.json({ users: enriched });
  } catch (err) {
    if (err instanceof Error && err.message === "UNAUTHORIZED") {
      return NextResponse.json({ error: "No autorizado" }, { status: 401 });
    }
    if (err instanceof Error && err.message === "FORBIDDEN") {
      return NextResponse.json({ error: "Solo administradores" }, { status: 403 });
    }
    return NextResponse.json({ error: "Error" }, { status: 500 });
  }
}
