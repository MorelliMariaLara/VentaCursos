import { getSession } from "./auth";
import type { SessionPayload } from "./types";

export async function requireAdmin(): Promise<SessionPayload> {
  const session = await getSession();
  if (!session) throw new Error("UNAUTHORIZED");
  if (session.role !== "admin") throw new Error("FORBIDDEN");
  return session;
}
