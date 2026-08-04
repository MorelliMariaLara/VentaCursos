const path = require("path");
const fs = require("fs");
const express = require("express");
const bcrypt = require("bcryptjs");
const { sign, verify } = require("./jwt");
const db = require("./db");
const { toPublicCourse, findLesson } = require("./courses");
const payments = require("./payments");
const stream = require("./stream");

loadEnv();

const app = express();
const PORT = Number(process.env.PORT || 3000);
const AUTH_SECRET = process.env.AUTH_SECRET || "nexa-dev-secret-change-me-in-prod";
const COOKIE = "nexa_session";
const SESSION_HOURS = 72;

app.use(express.json({ limit: "2mb" }));
app.use(express.urlencoded({ extended: true }));

function loadEnv() {
  for (const file of [".env.local", ".env"]) {
    const full = path.join(process.cwd(), file);
    if (!fs.existsSync(full)) continue;
    const text = fs.readFileSync(full, "utf8");
    for (const line of text.split(/\r?\n/)) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith("#")) continue;
      const i = trimmed.indexOf("=");
      if (i === -1) continue;
      const key = trimmed.slice(0, i).trim();
      const val = trimmed.slice(i + 1).trim();
      if (!process.env[key]) process.env[key] = val;
    }
  }
}

function parseCookies(req) {
  const header = req.headers.cookie || "";
  const out = {};
  for (const part of header.split(";")) {
    const [k, ...rest] = part.trim().split("=");
    if (!k) continue;
    out[k] = decodeURIComponent(rest.join("=") || "");
  }
  return out;
}

function setSession(res, token) {
  const secure = process.env.NODE_ENV === "production" ? "; Secure" : "";
  res.setHeader(
    "Set-Cookie",
    `${COOKIE}=${encodeURIComponent(token)}; Path=/; HttpOnly; SameSite=Lax; Max-Age=${SESSION_HOURS * 3600}${secure}`,
  );
}

function clearSession(res) {
  res.setHeader("Set-Cookie", `${COOKIE}=; Path=/; HttpOnly; Max-Age=0`);
}

function getSession(req) {
  const token = parseCookies(req)[COOKIE];
  if (!token) return null;
  const payload = verify(token, AUTH_SECRET);
  if (!payload?.sub || !payload.email) return null;
  return {
    sub: payload.sub,
    email: payload.email,
    name: payload.name || "",
    role: payload.role || "student",
  };
}

function requireUser(req, res) {
  const session = getSession(req);
  if (!session) {
    res.status(401).json({ error: "UNAUTHORIZED" });
    return null;
  }
  return session;
}

function requireAdmin(req, res) {
  const session = requireUser(req, res);
  if (!session) return null;
  if (session.role !== "admin") {
    res.status(403).json({ error: "FORBIDDEN" });
    return null;
  }
  return session;
}

function money(n) {
  return new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
  }).format(n);
}

// ---------- Auth ----------
app.get("/api/auth/me", async (req, res) => {
  const session = getSession(req);
  if (!session) return res.json({ user: null });
  const user = await db.findUserById(session.sub);
  if (!user) return res.json({ user: null });
  res.json({
    user: { id: user.id, name: user.name, email: user.email, role: user.role },
  });
});

app.post("/api/auth/login", async (req, res) => {
  const email = String(req.body.email || "").trim();
  const password = String(req.body.password || "");
  const user = await db.findUserByEmail(email);
  if (!user || !(await bcrypt.compare(password, user.passwordHash))) {
    return res.status(401).json({ error: "Credenciales inválidas" });
  }
  const token = sign(
    { sub: user.id, email: user.email, name: user.name, role: user.role },
    AUTH_SECRET,
    SESSION_HOURS * 3600,
  );
  setSession(res, token);
  res.json({ user: { id: user.id, name: user.name, email: user.email, role: user.role } });
});

app.post("/api/auth/register", async (req, res) => {
  try {
    const name = String(req.body.name || "").trim();
    const email = String(req.body.email || "").trim();
    const password = String(req.body.password || "");
    if (!name || !email || password.length < 6) {
      return res.status(400).json({ error: "Datos incompletos" });
    }
    const user = await db.createUser({ name, email, password });
    const token = sign(
      { sub: user.id, email: user.email, name: user.name, role: user.role },
      AUTH_SECRET,
      SESSION_HOURS * 3600,
    );
    setSession(res, token);
    res.json({ user: { id: user.id, name: user.name, email: user.email, role: user.role } });
  } catch (e) {
    if (e.message === "EMAIL_TAKEN") return res.status(409).json({ error: "Email ya registrado" });
    res.status(500).json({ error: "No se pudo registrar" });
  }
});

app.post("/api/auth/logout", (req, res) => {
  clearSession(res);
  res.json({ ok: true });
});

// ---------- Courses ----------
app.get("/api/courses", async (req, res) => {
  const courses = await db.listCourses();
  res.json({ courses: courses.map(toPublicCourse) });
});

app.get("/api/courses/:slug", async (req, res) => {
  const course = await db.getCourseBySlug(req.params.slug);
  if (!course || course.published === false) return res.status(404).json({ error: "No encontrado" });
  const session = getSession(req);
  let enrolled = false;
  let enrollment = null;
  if (session) {
    enrollment = await db.getEnrollment(session.sub, course.id);
    enrolled = Boolean(enrollment);
  }
  res.json({
    course: toPublicCourse(course),
    enrolled,
    enrollment,
    priceLabel: money(course.price),
  });
});

app.get("/api/my-courses", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const enrollments = await db.listEnrollmentsForUser(session.sub);
  const courses = await db.listCourses(true);
  const items = enrollments
    .map((e) => {
      const course = courses.find((c) => c.id === e.courseId);
      if (!course) return null;
      return { course: toPublicCourse(course), enrollment: e };
    })
    .filter(Boolean);
  res.json({ items });
});

// ---------- Payments ----------
app.get("/api/payments/config", (req, res) => {
  res.json({
    configured: payments.isMercadoPagoConfigured(),
    simulate: payments.allowSimulatePayments(),
    publicKey: payments.getPublicKey(),
  });
});

app.post("/api/payments/preference", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const course = await db.getCourseBySlug(String(req.body.slug || ""));
  if (!course) return res.status(404).json({ error: "Curso no encontrado" });
  const existing = await db.getEnrollment(session.sub, course.id);
  if (existing) return res.status(409).json({ error: "Ya tenés este curso", slug: course.slug });

  try {
    const order = await db.createPendingOrder({
      userId: session.sub,
      courseId: course.id,
      amount: course.price,
      currency: course.currency,
    });

    if (!payments.isMercadoPagoConfigured()) {
      return res.json({
        orderId: order.id,
        simulateOnly: true,
        preferenceId: null,
      });
    }

    const pref = await payments.createPreference({
      orderId: order.id,
      title: course.title,
      amount: course.price,
      currency: course.currency,
      payerEmail: session.email,
      courseId: course.id,
      slug: course.slug,
    });
    await db.updateOrder(order.id, { preferenceId: pref.id });
    res.json({ orderId: order.id, preferenceId: pref.id, simulateOnly: false });
  } catch (e) {
    if (e.message === "ALREADY_OWNED") return res.status(409).json({ error: "Ya comprado" });
    console.error(e);
    res.status(500).json({ error: "No se pudo crear la preferencia" });
  }
});

app.post("/api/payments/process", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const { orderId, simulate, formData } = req.body || {};
  const order = await db.getOrderById(String(orderId || ""));
  if (!order || order.userId !== session.sub) return res.status(404).json({ error: "Orden no encontrada" });
  const course = await db.getCourseById(order.courseId);
  if (!course) return res.status(404).json({ error: "Curso no encontrado" });

  try {
    if (simulate) {
      if (!payments.allowSimulatePayments()) {
        return res.status(400).json({ error: "Simulación deshabilitada" });
      }
      await db.updateOrder(order.id, { simulated: true, paymentMethod: "simulate" });
      await db.fulfillPaidOrder(order.id);
      return res.json({ status: "paid", slug: course.slug, redirect: `/aprender.html?slug=${course.slug}` });
    }

    if (!payments.isMercadoPagoConfigured()) {
      return res.status(400).json({ error: "Mercado Pago no configurado" });
    }

    const payment = await payments.createPayment(formData || {}, order.id);
    const status = payments.mapMpStatus(payment.status);
    await db.updateOrder(order.id, {
      status,
      paymentId: String(payment.id),
      paymentMethod: payment.payment_method_id,
      statusDetail: payment.status_detail,
      payerEmail: payment.payer?.email,
    });
    if (status === "paid") await db.fulfillPaidOrder(order.id);
    res.json({
      status,
      slug: course.slug,
      redirect: status === "paid" ? `/aprender.html?slug=${course.slug}` : `/checkout.html?slug=${course.slug}&status=${status}`,
    });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: "Error procesando pago" });
  }
});

app.post("/api/webhooks/mercadopago", async (req, res) => {
  try {
    const paymentId = req.body?.data?.id || req.query["data.id"] || req.query.id;
    if (!paymentId) return res.status(200).json({ ok: true });
    const payment = await payments.getPayment(paymentId);
    const orderId = payment.external_reference;
    if (!orderId) return res.status(200).json({ ok: true });
    const status = payments.mapMpStatus(payment.status);
    await db.updateOrder(orderId, {
      status,
      paymentId: String(payment.id),
      statusDetail: payment.status_detail,
    });
    if (status === "paid") await db.fulfillPaidOrder(orderId);
    res.json({ ok: true });
  } catch (e) {
    console.error(e);
    res.status(200).json({ ok: true });
  }
});

// ---------- Stream / progress ----------
app.post("/api/stream/session", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const { slug, lessonId } = req.body || {};
  const course = await db.getCourseBySlug(String(slug || ""));
  if (!course) return res.status(404).json({ error: "Curso no encontrado" });
  const enrollment = await db.getEnrollment(session.sub, course.id);
  if (!enrollment) return res.status(403).json({ error: "Sin acceso" });
  const found = findLesson(course, String(lessonId || ""));
  if (!found) return res.status(404).json({ error: "Lección no encontrada" });

  const { key, iv } = stream.generateContentKey();
  const token = stream.createStreamToken({
    userId: session.sub,
    courseId: course.id,
    lessonId: found.lesson.id,
    key,
    iv,
  });
  res.json({
    token,
    keyB64: key.toString("base64"),
    ivB64: iv.toString("base64"),
    watermark: stream.watermarkFingerprint(session.sub, session.email),
    mediaUrl: `/api/stream/media?token=${encodeURIComponent(token)}`,
    lesson: { id: found.lesson.id, title: found.lesson.title, moduleTitle: found.moduleTitle },
  });
});

app.get("/api/stream/media", async (req, res) => {
  const claims = stream.verifyStreamToken(String(req.query.token || ""));
  if (!claims) return res.status(401).end();
  const course = await db.getCourseById(claims.courseId);
  if (!course) return res.status(404).end();
  const found = findLesson(course, claims.lessonId);
  if (!found) return res.status(404).end();

  try {
    const range = req.headers.range;
    const src = await stream.readVideoSource(found.lesson.sourceUrl, range);
    const key = Buffer.from(claims.keyB64, "base64");
    const iv = Buffer.from(claims.ivB64, "base64");
    const encrypted = stream.encryptChunk(src.body, key, iv, src.start || 0);
    res.status(src.status);
    res.setHeader("Content-Type", "application/octet-stream");
    res.setHeader("Content-Length", encrypted.length);
    res.setHeader("Cache-Control", "no-store");
    res.setHeader("X-Content-Encoding", "aes-256-ctr");
    if (src.contentRange) res.setHeader("Content-Range", src.contentRange);
    res.send(encrypted);
  } catch (e) {
    console.error(e);
    res.status(500).end();
  }
});

app.post("/api/progress", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const course = await db.getCourseBySlug(String(req.body.slug || ""));
  if (!course) return res.status(404).json({ error: "Curso no encontrado" });
  try {
    const enrollment = await db.markLessonComplete(session.sub, course.id, String(req.body.lessonId || ""));
    res.json({ enrollment });
  } catch (e) {
    if (e.message === "NOT_ENROLLED") return res.status(403).json({ error: "Sin acceso" });
    res.status(500).json({ error: "Error" });
  }
});

app.get("/api/certificate/:slug", async (req, res) => {
  const session = requireUser(req, res);
  if (!session) return;
  const course = await db.getCourseBySlug(req.params.slug);
  if (!course) return res.status(404).json({ error: "No encontrado" });
  const enrollment = await db.getEnrollment(session.sub, course.id);
  if (!enrollment?.certificateCode) return res.status(404).json({ error: "Sin certificado" });
  res.json({
    course: toPublicCourse(course),
    certificateCode: enrollment.certificateCode,
    issuedAt: enrollment.certificateIssuedAt,
    studentName: session.name,
  });
});

// ---------- Admin ----------
app.get("/api/admin/stats", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  res.json(await db.stats());
});

app.get("/api/admin/orders", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  res.json({ orders: await db.listOrders() });
});

app.get("/api/admin/users", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  const users = await db.listUsers();
  res.json({
    users: users.map((u) => ({
      id: u.id,
      name: u.name,
      email: u.email,
      role: u.role,
      createdAt: u.createdAt,
    })),
  });
});

app.get("/api/admin/courses", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  const courses = await db.listCourses(true);
  res.json({ courses: courses.map(toPublicCourse) });
});

app.post("/api/admin/courses", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  try {
    const course = await db.upsertCourse(req.body || {});
    res.json({ course: toPublicCourse(course) });
  } catch (e) {
    res.status(400).json({ error: e.message });
  }
});

app.delete("/api/admin/courses/:id", async (req, res) => {
  if (!requireAdmin(req, res)) return;
  try {
    await db.deleteCourse(req.params.id);
    res.json({ ok: true });
  } catch (e) {
    res.status(400).json({ error: e.message });
  }
});

// Static pages
app.use(express.static(path.join(process.cwd(), "public")));

app.get("/", (req, res) => {
  res.sendFile(path.join(process.cwd(), "public", "index.html"));
});

app.listen(PORT, () => {
  console.log("");
  console.log("  NEXA listo → http://localhost:" + PORT);
  console.log("  Alumno: demo@nexa.academy / demo1234");
  console.log("  Admin:  admin@nexa.academy / admin1234");
  console.log("");
});
