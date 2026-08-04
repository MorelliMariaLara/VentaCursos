import { NextResponse } from "next/server";
import { requireAdmin } from "@/lib/admin";
import { findUserById, getCourseById, listOrders } from "@/lib/db";

export async function GET() {
  try {
    await requireAdmin();
    const orders = await listOrders();
    const enriched = await Promise.all(
      orders.map(async (order) => {
        const user = await findUserById(order.userId);
        const course = await getCourseById(order.courseId);
        return {
          ...order,
          userEmail: user?.email ?? "—",
          userName: user?.name ?? "—",
          courseTitle: course?.title ?? "—",
          courseSlug: course?.slug ?? null,
        };
      }),
    );
    return NextResponse.json({ orders: enriched });
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
