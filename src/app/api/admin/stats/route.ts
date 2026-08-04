import { NextResponse } from "next/server";
import { requireAdmin } from "@/lib/admin";
import { getDb } from "@/lib/db";

export async function GET() {
  try {
    await requireAdmin();
    const db = await getDb();
    const paid = db.orders.filter((o) => o.status === "paid");
    const revenue = paid.reduce((sum, o) => sum + o.amount, 0);
    return NextResponse.json({
      users: db.users.length,
      courses: db.courses.length,
      enrollments: db.enrollments.length,
      orders: db.orders.length,
      paidOrders: paid.length,
      revenue,
      pendingOrders: db.orders.filter((o) => o.status === "pending").length,
    });
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
