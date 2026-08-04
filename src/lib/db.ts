import { promises as fs } from "fs";
import path from "path";
import { randomUUID } from "crypto";
import bcrypt from "bcryptjs";
import { COURSES } from "./courses-data";
import type {
  Course,
  DatabaseShape,
  Enrollment,
  Order,
  OrderStatus,
  User,
} from "./types";

const DATA_DIR = path.join(process.cwd(), "data");
const DB_PATH = path.join(DATA_DIR, "store.json");

function withPublished(courses: Course[]): Course[] {
  return courses.map((c) => ({
    ...c,
    published: c.published ?? true,
  }));
}

async function ensureDb(): Promise<DatabaseShape> {
  await fs.mkdir(DATA_DIR, { recursive: true });
  try {
    const raw = await fs.readFile(DB_PATH, "utf8");
    const db = JSON.parse(raw) as DatabaseShape;
    let dirty = false;

    if (!db.courses?.length) {
      db.courses = withPublished(COURSES);
      dirty = true;
    } else {
      db.courses = withPublished(db.courses);
    }

    const hasAdmin = db.users.some((u) => u.role === "admin");
    if (!hasAdmin) {
      db.users.push({
        id: "user-admin",
        name: "Admin NEXA",
        email: "admin@nexa.academy",
        passwordHash: await bcrypt.hash("admin1234", 10),
        role: "admin",
        createdAt: new Date().toISOString(),
      });
      dirty = true;
    }

    if (dirty) await writeDb(db);
    return db;
  } catch {
    const demoPassword = await bcrypt.hash("demo1234", 10);
    const adminPassword = await bcrypt.hash("admin1234", 10);
    const seed: DatabaseShape = {
      users: [
        {
          id: "user-demo",
          name: "Estudiante Demo",
          email: "demo@nexa.academy",
          passwordHash: demoPassword,
          role: "student",
          createdAt: new Date().toISOString(),
        },
        {
          id: "user-admin",
          name: "Admin NEXA",
          email: "admin@nexa.academy",
          passwordHash: adminPassword,
          role: "admin",
          createdAt: new Date().toISOString(),
        },
      ],
      courses: withPublished(COURSES),
      enrollments: [],
      orders: [],
    };
    await fs.writeFile(DB_PATH, JSON.stringify(seed, null, 2), "utf8");
    return seed;
  }
}

async function writeDb(db: DatabaseShape): Promise<void> {
  await fs.mkdir(DATA_DIR, { recursive: true });
  await fs.writeFile(DB_PATH, JSON.stringify(db, null, 2), "utf8");
}

export async function getDb(): Promise<DatabaseShape> {
  return ensureDb();
}

export async function listCourses(options?: {
  includeUnpublished?: boolean;
}): Promise<Course[]> {
  const db = await ensureDb();
  if (options?.includeUnpublished) return db.courses;
  return db.courses.filter((c) => c.published !== false);
}

export async function getCourseBySlug(slug: string): Promise<Course | undefined> {
  const db = await ensureDb();
  return db.courses.find((c) => c.slug === slug);
}

export async function getCourseById(id: string): Promise<Course | undefined> {
  const db = await ensureDb();
  return db.courses.find((c) => c.id === id);
}

export async function findUserByEmail(email: string): Promise<User | undefined> {
  const db = await ensureDb();
  return db.users.find((u) => u.email.toLowerCase() === email.toLowerCase());
}

export async function findUserById(id: string): Promise<User | undefined> {
  const db = await ensureDb();
  return db.users.find((u) => u.id === id);
}

export async function listUsers(): Promise<User[]> {
  const db = await ensureDb();
  return db.users;
}

export async function createUser(input: {
  name: string;
  email: string;
  password: string;
  role?: User["role"];
}): Promise<User> {
  const db = await ensureDb();
  const exists = db.users.some(
    (u) => u.email.toLowerCase() === input.email.toLowerCase(),
  );
  if (exists) {
    throw new Error("EMAIL_TAKEN");
  }
  const user: User = {
    id: randomUUID(),
    name: input.name,
    email: input.email.toLowerCase(),
    passwordHash: await bcrypt.hash(input.password, 10),
    role: input.role ?? "student",
    createdAt: new Date().toISOString(),
  };
  db.users.push(user);
  await writeDb(db);
  return user;
}

export async function getEnrollment(
  userId: string,
  courseId: string,
): Promise<Enrollment | undefined> {
  const db = await ensureDb();
  return db.enrollments.find(
    (e) => e.userId === userId && e.courseId === courseId,
  );
}

export async function listEnrollmentsForUser(
  userId: string,
): Promise<Enrollment[]> {
  const db = await ensureDb();
  return db.enrollments.filter((e) => e.userId === userId);
}

export async function listOrders(): Promise<Order[]> {
  const db = await ensureDb();
  return [...db.orders].sort(
    (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt),
  );
}

export async function getOrderById(id: string): Promise<Order | undefined> {
  const db = await ensureDb();
  return db.orders.find((o) => o.id === id);
}

export async function createPendingOrder(input: {
  userId: string;
  courseId: string;
  amount: number;
  currency: string;
}): Promise<Order> {
  const db = await ensureDb();
  const existingPaid = db.orders.find(
    (o) =>
      o.userId === input.userId &&
      o.courseId === input.courseId &&
      o.status === "paid",
  );
  if (existingPaid) {
    throw new Error("ALREADY_OWNED");
  }

  const order: Order = {
    id: randomUUID(),
    userId: input.userId,
    courseId: input.courseId,
    amount: input.amount,
    currency: input.currency,
    status: "pending",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
  db.orders.push(order);
  await writeDb(db);
  return order;
}

export async function updateOrder(
  orderId: string,
  patch: Partial<Order>,
): Promise<Order> {
  const db = await ensureDb();
  const order = db.orders.find((o) => o.id === orderId);
  if (!order) throw new Error("ORDER_NOT_FOUND");
  Object.assign(order, patch, { updatedAt: new Date().toISOString() });
  await writeDb(db);
  return order;
}

export async function enrollUser(input: {
  userId: string;
  courseId: string;
  orderId?: string;
}): Promise<Enrollment> {
  const db = await ensureDb();
  const existing = db.enrollments.find(
    (e) => e.userId === input.userId && e.courseId === input.courseId,
  );
  if (existing) {
    if (input.orderId && !existing.orderId) {
      existing.orderId = input.orderId;
      await writeDb(db);
    }
    return existing;
  }

  const enrollment: Enrollment = {
    id: randomUUID(),
    userId: input.userId,
    courseId: input.courseId,
    purchasedAt: new Date().toISOString(),
    progress: {},
    orderId: input.orderId,
  };
  db.enrollments.push(enrollment);
  await writeDb(db);
  return enrollment;
}

/** @deprecated use createPendingOrder + fulfillPayment */
export async function purchaseCourse(
  userId: string,
  courseId: string,
): Promise<{ order: Order; enrollment: Enrollment }> {
  const db = await ensureDb();
  const course = db.courses.find((c) => c.id === courseId);
  if (!course) throw new Error("COURSE_NOT_FOUND");

  const existing = db.enrollments.find(
    (e) => e.userId === userId && e.courseId === courseId,
  );
  if (existing) {
    const order = db.orders.find(
      (o) => o.userId === userId && o.courseId === courseId && o.status === "paid",
    );
    if (order) return { order, enrollment: existing };
  }

  const order: Order = {
    id: randomUUID(),
    userId,
    courseId,
    amount: course.price,
    currency: course.currency,
    status: "paid",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    simulated: true,
  };

  const enrollment: Enrollment = existing ?? {
    id: randomUUID(),
    userId,
    courseId,
    purchasedAt: new Date().toISOString(),
    progress: {},
    orderId: order.id,
  };

  db.orders.push(order);
  if (!existing) db.enrollments.push(enrollment);
  await writeDb(db);
  return { order, enrollment };
}

export async function fulfillPaidOrder(orderId: string): Promise<{
  order: Order;
  enrollment: Enrollment;
}> {
  const db = await ensureDb();
  const order = db.orders.find((o) => o.id === orderId);
  if (!order) throw new Error("ORDER_NOT_FOUND");

  order.status = "paid";
  order.updatedAt = new Date().toISOString();

  let enrollment = db.enrollments.find(
    (e) => e.userId === order.userId && e.courseId === order.courseId,
  );
  if (!enrollment) {
    enrollment = {
      id: randomUUID(),
      userId: order.userId,
      courseId: order.courseId,
      purchasedAt: new Date().toISOString(),
      progress: {},
      orderId: order.id,
    };
    db.enrollments.push(enrollment);
  } else {
    enrollment.orderId = order.id;
  }

  await writeDb(db);
  return { order, enrollment };
}

export async function markLessonComplete(
  userId: string,
  courseId: string,
  lessonId: string,
): Promise<Enrollment> {
  const db = await ensureDb();
  const enrollment = db.enrollments.find(
    (e) => e.userId === userId && e.courseId === courseId,
  );
  if (!enrollment) throw new Error("NOT_ENROLLED");

  enrollment.progress[lessonId] = true;

  const course = db.courses.find((c) => c.id === courseId);
  if (course && course.includesCertificate && !enrollment.certificateIssuedAt) {
    const allLessons = course.modules.flatMap((m) => m.lessons);
    const done = allLessons.every((l) => enrollment.progress[l.id]);
    if (done) {
      enrollment.certificateIssuedAt = new Date().toISOString();
      enrollment.certificateCode = `NEXA-${course.slug
        .slice(0, 4)
        .toUpperCase()}-${randomUUID().slice(0, 8).toUpperCase()}`;
    }
  }

  await writeDb(db);
  return enrollment;
}

export async function upsertCourse(
  input: Partial<Course> & {
    title: string;
    slug: string;
    price: number;
  },
): Promise<Course> {
  const db = await ensureDb();
  const now = new Date().toISOString();

  if (input.id) {
    const idx = db.courses.findIndex((c) => c.id === input.id);
    if (idx === -1) throw new Error("COURSE_NOT_FOUND");
    const slugTaken = db.courses.some(
      (c) => c.slug === input.slug && c.id !== input.id,
    );
    if (slugTaken) throw new Error("SLUG_TAKEN");
    const clean = Object.fromEntries(
      Object.entries(input).filter(([, v]) => v !== undefined),
    ) as Partial<Course>;
    db.courses[idx] = {
      ...db.courses[idx],
      ...clean,
      updatedAt: now,
    } as Course;
    await writeDb(db);
    return db.courses[idx];
  }

  if (db.courses.some((c) => c.slug === input.slug)) {
    throw new Error("SLUG_TAKEN");
  }

  const course: Course = {
    id: randomUUID(),
    slug: input.slug,
    title: input.title,
    subtitle: input.subtitle ?? "",
    description: input.description ?? "",
    category: input.category ?? "General",
    level: input.level ?? "Inicial",
    price: input.price,
    currency: input.currency ?? "ARS",
    durationHours: input.durationHours ?? 1,
    includesCertificate: input.includesCertificate ?? true,
    certificateName:
      input.certificateName ?? `Certificación NEXA en ${input.title}`,
    thumbnailGradient:
      input.thumbnailGradient ??
      "linear-gradient(135deg, #0B3D4A 0%, #1A7A6D 55%, #C45C26 100%)",
    instructor: input.instructor ?? "Equipo NEXA",
    learningOutcomes: input.learningOutcomes ?? [],
    modules: input.modules ?? [],
    published: input.published ?? true,
    updatedAt: now,
  };
  db.courses.push(course);
  await writeDb(db);
  return course;
}

export async function deleteCourse(id: string): Promise<void> {
  const db = await ensureDb();
  const before = db.courses.length;
  db.courses = db.courses.filter((c) => c.id !== id);
  if (db.courses.length === before) throw new Error("COURSE_NOT_FOUND");
  await writeDb(db);
}

export async function setOrderStatus(
  orderId: string,
  status: OrderStatus,
  extras?: Partial<Order>,
): Promise<Order> {
  return updateOrder(orderId, { status, ...extras });
}

export function findLesson(
  course: Course,
  lessonId: string,
): { moduleTitle: string; lesson: Course["modules"][0]["lessons"][0] } | null {
  for (const mod of course.modules) {
    const lesson = mod.lessons.find((l) => l.id === lessonId);
    if (lesson) return { moduleTitle: mod.title, lesson };
  }
  return null;
}

export function toPublicCourse(course: Course) {
  return {
    id: course.id,
    slug: course.slug,
    title: course.title,
    subtitle: course.subtitle,
    description: course.description,
    category: course.category,
    level: course.level,
    price: course.price,
    currency: course.currency,
    durationHours: course.durationHours,
    includesCertificate: course.includesCertificate,
    certificateName: course.certificateName,
    thumbnailGradient: course.thumbnailGradient,
    instructor: course.instructor,
    learningOutcomes: course.learningOutcomes,
    published: course.published,
    modules: course.modules.map((m) => ({
      id: m.id,
      title: m.title,
      lessons: m.lessons.map((l) => ({
        id: l.id,
        title: l.title,
        durationMinutes: l.durationMinutes,
        order: l.order,
      })),
    })),
  };
}
