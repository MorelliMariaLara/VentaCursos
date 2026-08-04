const fs = require("fs/promises");
const path = require("path");
const { randomUUID } = require("crypto");
const bcrypt = require("bcryptjs");
const { COURSES, findLesson } = require("./courses");

const DATA_DIR = path.join(process.cwd(), "data");
const DB_PATH = path.join(DATA_DIR, "store.json");

async function writeDb(db) {
  await fs.mkdir(DATA_DIR, { recursive: true });
  await fs.writeFile(DB_PATH, JSON.stringify(db, null, 2), "utf8");
}

async function ensureDb() {
  await fs.mkdir(DATA_DIR, { recursive: true });
  try {
    const raw = await fs.readFile(DB_PATH, "utf8");
    const db = JSON.parse(raw);
    let dirty = false;
    if (!db.courses?.length) {
      db.courses = COURSES.map((c) => ({ ...c, published: c.published ?? true }));
      dirty = true;
    }
    if (!db.users.some((u) => u.role === "admin")) {
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
    const seed = {
      users: [
        {
          id: "user-demo",
          name: "Estudiante Demo",
          email: "demo@nexa.academy",
          passwordHash: await bcrypt.hash("demo1234", 10),
          role: "student",
          createdAt: new Date().toISOString(),
        },
        {
          id: "user-admin",
          name: "Admin NEXA",
          email: "admin@nexa.academy",
          passwordHash: await bcrypt.hash("admin1234", 10),
          role: "admin",
          createdAt: new Date().toISOString(),
        },
      ],
      courses: COURSES.map((c) => ({ ...c, published: true })),
      enrollments: [],
      orders: [],
    };
    await writeDb(seed);
    return seed;
  }
}

async function getDb() {
  return ensureDb();
}

async function listCourses(includeUnpublished = false) {
  const db = await ensureDb();
  return includeUnpublished ? db.courses : db.courses.filter((c) => c.published !== false);
}

async function getCourseBySlug(slug) {
  const db = await ensureDb();
  return db.courses.find((c) => c.slug === slug);
}

async function getCourseById(id) {
  const db = await ensureDb();
  return db.courses.find((c) => c.id === id);
}

async function findUserByEmail(email) {
  const db = await ensureDb();
  return db.users.find((u) => u.email.toLowerCase() === email.toLowerCase());
}

async function findUserById(id) {
  const db = await ensureDb();
  return db.users.find((u) => u.id === id);
}

async function listUsers() {
  const db = await ensureDb();
  return db.users;
}

async function createUser({ name, email, password, role = "student" }) {
  const db = await ensureDb();
  if (db.users.some((u) => u.email.toLowerCase() === email.toLowerCase())) {
    throw new Error("EMAIL_TAKEN");
  }
  const user = {
    id: randomUUID(),
    name,
    email: email.toLowerCase(),
    passwordHash: await bcrypt.hash(password, 10),
    role,
    createdAt: new Date().toISOString(),
  };
  db.users.push(user);
  await writeDb(db);
  return user;
}

async function getEnrollment(userId, courseId) {
  const db = await ensureDb();
  return db.enrollments.find((e) => e.userId === userId && e.courseId === courseId);
}

async function listEnrollmentsForUser(userId) {
  const db = await ensureDb();
  return db.enrollments.filter((e) => e.userId === userId);
}

async function listOrders() {
  const db = await ensureDb();
  return [...db.orders].sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt));
}

async function getOrderById(id) {
  const db = await ensureDb();
  return db.orders.find((o) => o.id === id);
}

async function createPendingOrder({ userId, courseId, amount, currency }) {
  const db = await ensureDb();
  if (db.orders.some((o) => o.userId === userId && o.courseId === courseId && o.status === "paid")) {
    throw new Error("ALREADY_OWNED");
  }
  const order = {
    id: randomUUID(),
    userId,
    courseId,
    amount,
    currency,
    status: "pending",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
  db.orders.push(order);
  await writeDb(db);
  return order;
}

async function updateOrder(orderId, patch) {
  const db = await ensureDb();
  const order = db.orders.find((o) => o.id === orderId);
  if (!order) throw new Error("ORDER_NOT_FOUND");
  Object.assign(order, patch, { updatedAt: new Date().toISOString() });
  await writeDb(db);
  return order;
}

async function fulfillPaidOrder(orderId) {
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

async function markLessonComplete(userId, courseId, lessonId) {
  const db = await ensureDb();
  const enrollment = db.enrollments.find((e) => e.userId === userId && e.courseId === courseId);
  if (!enrollment) throw new Error("NOT_ENROLLED");
  enrollment.progress[lessonId] = true;
  const course = db.courses.find((c) => c.id === courseId);
  if (course?.includesCertificate && !enrollment.certificateIssuedAt) {
    const all = course.modules.flatMap((m) => m.lessons);
    if (all.every((l) => enrollment.progress[l.id])) {
      enrollment.certificateIssuedAt = new Date().toISOString();
      enrollment.certificateCode = `NEXA-${course.slug.slice(0, 4).toUpperCase()}-${randomUUID().slice(0, 8).toUpperCase()}`;
    }
  }
  await writeDb(db);
  return enrollment;
}

async function upsertCourse(input) {
  const db = await ensureDb();
  const now = new Date().toISOString();
  if (input.id) {
    const idx = db.courses.findIndex((c) => c.id === input.id);
    if (idx === -1) throw new Error("COURSE_NOT_FOUND");
    if (db.courses.some((c) => c.slug === input.slug && c.id !== input.id)) throw new Error("SLUG_TAKEN");
    db.courses[idx] = { ...db.courses[idx], ...input, updatedAt: now };
    await writeDb(db);
    return db.courses[idx];
  }
  if (db.courses.some((c) => c.slug === input.slug)) throw new Error("SLUG_TAKEN");
  const course = {
    id: randomUUID(),
    slug: input.slug,
    title: input.title,
    subtitle: input.subtitle || "",
    description: input.description || "",
    category: input.category || "General",
    level: input.level || "Inicial",
    price: Number(input.price) || 0,
    currency: input.currency || "ARS",
    durationHours: Number(input.durationHours) || 1,
    includesCertificate: input.includesCertificate !== false,
    certificateName: input.certificateName || `Certificación NEXA en ${input.title}`,
    thumbnailGradient:
      input.thumbnailGradient ||
      "linear-gradient(135deg, #0B3D4A 0%, #1A7A6D 55%, #C45C26 100%)",
    instructor: input.instructor || "Equipo NEXA",
    learningOutcomes: input.learningOutcomes || [],
    modules: input.modules || [],
    published: input.published !== false,
    updatedAt: now,
  };
  db.courses.push(course);
  await writeDb(db);
  return course;
}

async function deleteCourse(id) {
  const db = await ensureDb();
  const before = db.courses.length;
  db.courses = db.courses.filter((c) => c.id !== id);
  if (db.courses.length === before) throw new Error("COURSE_NOT_FOUND");
  await writeDb(db);
}

async function stats() {
  const db = await ensureDb();
  const paid = db.orders.filter((o) => o.status === "paid");
  return {
    users: db.users.length,
    courses: db.courses.length,
    enrollments: db.enrollments.length,
    orders: db.orders.length,
    revenue: paid.reduce((sum, o) => sum + (o.amount || 0), 0),
  };
}

module.exports = {
  getDb,
  listCourses,
  getCourseBySlug,
  getCourseById,
  findUserByEmail,
  findUserById,
  listUsers,
  createUser,
  getEnrollment,
  listEnrollmentsForUser,
  listOrders,
  getOrderById,
  createPendingOrder,
  updateOrder,
  fulfillPaidOrder,
  markLessonComplete,
  upsertCourse,
  deleteCourse,
  stats,
  findLesson,
};
