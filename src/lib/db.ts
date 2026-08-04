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
  User,
} from "./types";

const DATA_DIR = path.join(process.cwd(), "data");
const DB_PATH = path.join(DATA_DIR, "store.json");

async function ensureDb(): Promise<DatabaseShape> {
  await fs.mkdir(DATA_DIR, { recursive: true });
  try {
    const raw = await fs.readFile(DB_PATH, "utf8");
    return JSON.parse(raw) as DatabaseShape;
  } catch {
    const demoPassword = await bcrypt.hash("demo1234", 10);
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
      ],
      courses: COURSES,
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

export async function listCourses(): Promise<Course[]> {
  const db = await ensureDb();
  return db.courses;
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

export async function createUser(input: {
  name: string;
  email: string;
  password: string;
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
    role: "student",
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
  };

  const enrollment: Enrollment = existing ?? {
    id: randomUUID(),
    userId,
    courseId,
    purchasedAt: new Date().toISOString(),
    progress: {},
  };

  db.orders.push(order);
  if (!existing) db.enrollments.push(enrollment);
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
