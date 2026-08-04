const http = require("http");
const path = require("path");
const fs = require("fs");
const fsp = require("fs/promises");
const { URL } = require("url");
const { sign, verify } = require("./jwt");
const db = require("./db");
const { toPublicCourse, findLesson } = require("./courses");
const payments = require("./payments");
const stream = require("./stream");
const { verifyPassword } = require("./password");

loadEnv();

const PORT = Number(process.env.PORT || 3000);
const AUTH_SECRET = process.env.AUTH_SECRET || "nexa-dev-secret-change-me-in-prod";
const COOKIE = "nexa_session";
const SESSION_HOURS = 72;
const PUBLIC_DIR = path.join(process.cwd(), "public");

function loadEnv() {
  for (const file of [".env.local", ".env"]) {
    const full = path.join(process.cwd(), file);
    if (!fs.existsSync(full)) continue;
    for (const line of fs.readFileSync(full, "utf8").split(/\r?\n/)) {
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

function send(res, status, data, headers = {}) {
  const body = typeof data === "string" || Buffer.isBuffer(data) ? data : JSON.stringify(data);
  const base = {
    "Content-Type": typeof data === "string" || Buffer.isBuffer(data)
      ? headers["Content-Type"] || "text/plain; charset=utf-8"
      : "application/json; charset=utf-8",
    ...headers,
  };
  res.writeHead(status, base);
  res.end(body);
}

function parseCookies(req) {
  const out = {};
  for (const part of String(req.headers.cookie || "").split(";")) {
    const [k, ...rest] = part.trim().split("=");
    if (!k) continue;
    out[k] = decodeURIComponent(rest.join("=") || "");
  }
  return out;
}

function appendSetCookie(res, value) {
  const prev = res.getHeader("Set-Cookie");
  if (!prev) res.setHeader("Set-Cookie", value);
  else if (Array.isArray(prev)) res.setHeader("Set-Cookie", [...prev, value]);
  else res.setHeader("Set-Cookie", [prev, value]);
}

function setSession(res, token) {
  const secure = process.env.NODE_ENV === "production" ? "; Secure" : "";
  appendSetCookie(
    res,
    `${COOKIE}=${encodeURIComponent(token)}; Path=/; HttpOnly; SameSite=Lax; Max-Age=${SESSION_HOURS * 3600}${secure}`,
  );
}

function clearSession(res) {
  appendSetCookie(res, `${COOKIE}=; Path=/; HttpOnly; Max-Age=0`);
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

async function readBody(req) {
  const chunks = [];
  for await (const chunk of req) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString("utf8");
  if (!raw) return {};
  const type = String(req.headers["content-type"] || "");
  if (type.includes("application/json")) {
    try { return JSON.parse(raw); } catch { return {}; }
  }
  if (type.includes("application/x-www-form-urlencoded")) {
    const out = {};
    for (const [k, v] of new URLSearchParams(raw)) out[k] = v;
    return out;
  }
  try { return JSON.parse(raw); } catch { return { raw }; }
}

function money(n) {
  return new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency: "ARS",
    maximumFractionDigits: 0,
  }).format(n);
}

const MIME = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".ico": "image/x-icon",
  ".json": "application/json; charset=utf-8",
};

async function serveStatic(req, res, pathname) {
  let rel = pathname === "/" ? "/index.html" : pathname;
  rel = decodeURIComponent(rel.split("?")[0]);
  const full = path.normalize(path.join(PUBLIC_DIR, rel));
  if (!full.startsWith(PUBLIC_DIR)) return send(res, 403, { error: "Forbidden" });
  try {
    const data = await fsp.readFile(full);
    send(res, 200, data, { "Content-Type": MIME[path.extname(full)] || "application/octet-stream" });
  } catch {
    send(res, 404, { error: "Not found" });
  }
}

async function handleApi(req, res, url) {
  const method = req.method || "GET";
  const p = url.pathname;
  const body = method === "GET" || method === "HEAD" ? {} : await readBody(req);

  // Auth
  if (method === "GET" && p === "/api/auth/me") {
    const session = getSession(req);
    if (!session) return send(res, 200, { user: null });
    const user = await db.findUserById(session.sub);
    if (!user) return send(res, 200, { user: null });
    return send(res, 200, {
      user: { id: user.id, name: user.name, email: user.email, role: user.role },
    });
  }

  if (method === "POST" && p === "/api/auth/login") {
    const email = String(body.email || "").trim();
    const password = String(body.password || "");
    const user = await db.findUserByEmail(email);
    if (!user || !(await verifyPassword(password, user.passwordHash))) {
      return send(res, 401, { error: "Credenciales inválidas" });
    }
    const token = sign(
      { sub: user.id, email: user.email, name: user.name, role: user.role },
      AUTH_SECRET,
      SESSION_HOURS * 3600,
    );
    setSession(res, token);
    return send(res, 200, {
      user: { id: user.id, name: user.name, email: user.email, role: user.role },
    });
  }

  if (method === "POST" && p === "/api/auth/register") {
    try {
      const name = String(body.name || "").trim();
      const email = String(body.email || "").trim();
      const password = String(body.password || "");
      if (!name || !email || password.length < 6) {
        return send(res, 400, { error: "Datos incompletos" });
      }
      const user = await db.createUser({ name, email, password });
      const token = sign(
        { sub: user.id, email: user.email, name: user.name, role: user.role },
        AUTH_SECRET,
        SESSION_HOURS * 3600,
      );
      setSession(res, token);
      return send(res, 200, {
        user: { id: user.id, name: user.name, email: user.email, role: user.role },
      });
    } catch (e) {
      if (e.message === "EMAIL_TAKEN") return send(res, 409, { error: "Email ya registrado" });
      return send(res, 500, { error: "No se pudo registrar" });
    }
  }

  if (method === "POST" && p === "/api/auth/logout") {
    clearSession(res);
    return send(res, 200, { ok: true });
  }

  // Courses
  if (method === "GET" && p === "/api/courses") {
    const courses = await db.listCourses();
    return send(res, 200, { courses: courses.map(toPublicCourse) });
  }

  if (method === "GET" && p.startsWith("/api/courses/")) {
    const slug = decodeURIComponent(p.slice("/api/courses/".length));
    const course = await db.getCourseBySlug(slug);
    if (!course || course.published === false) return send(res, 404, { error: "No encontrado" });
    const session = getSession(req);
    let enrollment = null;
    if (session) enrollment = await db.getEnrollment(session.sub, course.id);
    return send(res, 200, {
      course: toPublicCourse(course),
      enrolled: Boolean(enrollment),
      enrollment,
      priceLabel: money(course.price),
    });
  }

  if (method === "GET" && p === "/api/my-courses") {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const enrollments = await db.listEnrollmentsForUser(session.sub);
    const courses = await db.listCourses(true);
    const items = enrollments
      .map((e) => {
        const course = courses.find((c) => c.id === e.courseId);
        return course ? { course: toPublicCourse(course), enrollment: e } : null;
      })
      .filter(Boolean);
    return send(res, 200, { items });
  }

  // Payments
  if (method === "GET" && p === "/api/payments/config") {
    return send(res, 200, {
      configured: payments.isMercadoPagoConfigured(),
      simulate: payments.allowSimulatePayments(),
      publicKey: payments.getPublicKey(),
    });
  }

  if (method === "POST" && p === "/api/payments/preference") {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const course = await db.getCourseBySlug(String(body.slug || ""));
    if (!course) return send(res, 404, { error: "Curso no encontrado" });
    if (await db.getEnrollment(session.sub, course.id)) {
      return send(res, 409, { error: "Ya tenés este curso", slug: course.slug });
    }
    try {
      const order = await db.createPendingOrder({
        userId: session.sub,
        courseId: course.id,
        amount: course.price,
        currency: course.currency,
      });
      if (!payments.isMercadoPagoConfigured()) {
        return send(res, 200, { orderId: order.id, simulateOnly: true, preferenceId: null });
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
      return send(res, 200, { orderId: order.id, preferenceId: pref.id, simulateOnly: false });
    } catch (e) {
      if (e.message === "ALREADY_OWNED") return send(res, 409, { error: "Ya comprado" });
      console.error(e);
      return send(res, 500, { error: "No se pudo crear la preferencia" });
    }
  }

  if (method === "POST" && p === "/api/payments/process") {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const order = await db.getOrderById(String(body.orderId || ""));
    if (!order || order.userId !== session.sub) return send(res, 404, { error: "Orden no encontrada" });
    const course = await db.getCourseById(order.courseId);
    if (!course) return send(res, 404, { error: "Curso no encontrado" });
    try {
      if (body.simulate) {
        if (!payments.allowSimulatePayments()) return send(res, 400, { error: "Simulación deshabilitada" });
        await db.updateOrder(order.id, { simulated: true, paymentMethod: "simulate" });
        await db.fulfillPaidOrder(order.id);
        return send(res, 200, {
          status: "paid",
          slug: course.slug,
          redirect: `/aprender.html?slug=${course.slug}`,
        });
      }
      if (!payments.isMercadoPagoConfigured()) {
        return send(res, 400, { error: "Mercado Pago no configurado" });
      }
      const payment = await payments.createPayment(body.formData || {}, order.id);
      const status = payments.mapMpStatus(payment.status);
      await db.updateOrder(order.id, {
        status,
        paymentId: String(payment.id),
        paymentMethod: payment.payment_method_id,
        statusDetail: payment.status_detail,
        payerEmail: payment.payer?.email,
      });
      if (status === "paid") await db.fulfillPaidOrder(order.id);
      return send(res, 200, {
        status,
        slug: course.slug,
        redirect:
          status === "paid"
            ? `/aprender.html?slug=${course.slug}`
            : `/checkout.html?slug=${course.slug}&status=${status}`,
      });
    } catch (e) {
      console.error(e);
      return send(res, 500, { error: "Error procesando pago" });
    }
  }

  if ((method === "POST" || method === "GET") && p === "/api/webhooks/mercadopago") {
    try {
      const paymentId = body?.data?.id || url.searchParams.get("data.id") || url.searchParams.get("id");
      if (!paymentId) return send(res, 200, { ok: true });
      const payment = await payments.getPayment(paymentId);
      const orderId = payment.external_reference;
      if (!orderId) return send(res, 200, { ok: true });
      const status = payments.mapMpStatus(payment.status);
      await db.updateOrder(orderId, {
        status,
        paymentId: String(payment.id),
        statusDetail: payment.status_detail,
      });
      if (status === "paid") await db.fulfillPaidOrder(orderId);
      return send(res, 200, { ok: true });
    } catch (e) {
      console.error(e);
      return send(res, 200, { ok: true });
    }
  }

  // Stream / progress
  if (method === "POST" && p === "/api/stream/session") {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const course = await db.getCourseBySlug(String(body.slug || ""));
    if (!course) return send(res, 404, { error: "Curso no encontrado" });
    if (!(await db.getEnrollment(session.sub, course.id))) {
      return send(res, 403, { error: "Sin acceso" });
    }
    const found = findLesson(course, String(body.lessonId || ""));
    if (!found) return send(res, 404, { error: "Lección no encontrada" });
    const { key, iv } = stream.generateContentKey();
    const token = stream.createStreamToken({
      userId: session.sub,
      courseId: course.id,
      lessonId: found.lesson.id,
      key,
      iv,
    });
    return send(res, 200, {
      token,
      keyB64: key.toString("base64"),
      ivB64: iv.toString("base64"),
      watermark: stream.watermarkFingerprint(session.sub, session.email),
      mediaUrl: `/api/stream/media?token=${encodeURIComponent(token)}`,
      lesson: { id: found.lesson.id, title: found.lesson.title, moduleTitle: found.moduleTitle },
    });
  }

  if (method === "GET" && p === "/api/stream/media") {
    const claims = stream.verifyStreamToken(String(url.searchParams.get("token") || ""));
    if (!claims) return send(res, 401, "unauthorized");
    const course = await db.getCourseById(claims.courseId);
    if (!course) return send(res, 404, "missing");
    const found = findLesson(course, claims.lessonId);
    if (!found) return send(res, 404, "missing");
    try {
      const src = await stream.readVideoSource(found.lesson.sourceUrl, req.headers.range);
      const key = Buffer.from(claims.keyB64, "base64");
      const iv = Buffer.from(claims.ivB64, "base64");
      const encrypted = stream.encryptChunk(src.body, key, iv, src.start || 0);
      const headers = {
        "Content-Type": "application/octet-stream",
        "Content-Length": String(encrypted.length),
        "Cache-Control": "no-store",
        "X-Content-Encoding": "aes-256-ctr",
      };
      if (src.contentRange) headers["Content-Range"] = src.contentRange;
      return send(res, src.status, encrypted, headers);
    } catch (e) {
      console.error(e);
      return send(res, 500, "error");
    }
  }

  if (method === "POST" && p === "/api/progress") {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const course = await db.getCourseBySlug(String(body.slug || ""));
    if (!course) return send(res, 404, { error: "Curso no encontrado" });
    try {
      const enrollment = await db.markLessonComplete(
        session.sub,
        course.id,
        String(body.lessonId || ""),
      );
      return send(res, 200, { enrollment });
    } catch (e) {
      if (e.message === "NOT_ENROLLED") return send(res, 403, { error: "Sin acceso" });
      return send(res, 500, { error: "Error" });
    }
  }

  if (method === "GET" && p.startsWith("/api/certificate/")) {
    const session = getSession(req);
    if (!session) return send(res, 401, { error: "UNAUTHORIZED" });
    const slug = decodeURIComponent(p.slice("/api/certificate/".length));
    const course = await db.getCourseBySlug(slug);
    if (!course) return send(res, 404, { error: "No encontrado" });
    const enrollment = await db.getEnrollment(session.sub, course.id);
    if (!enrollment?.certificateCode) return send(res, 404, { error: "Sin certificado" });
    return send(res, 200, {
      course: toPublicCourse(course),
      certificateCode: enrollment.certificateCode,
      issuedAt: enrollment.certificateIssuedAt,
      studentName: session.name,
    });
  }

  // Admin
  const admin = () => {
    const session = getSession(req);
    if (!session) return { err: [401, { error: "UNAUTHORIZED" }] };
    if (session.role !== "admin") return { err: [403, { error: "FORBIDDEN" }] };
    return { session };
  };

  if (method === "GET" && p === "/api/admin/stats") {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    return send(res, 200, await db.stats());
  }
  if (method === "GET" && p === "/api/admin/orders") {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    return send(res, 200, { orders: await db.listOrders() });
  }
  if (method === "GET" && p === "/api/admin/users") {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    const users = await db.listUsers();
    return send(res, 200, {
      users: users.map((u) => ({
        id: u.id,
        name: u.name,
        email: u.email,
        role: u.role,
        createdAt: u.createdAt,
      })),
    });
  }
  if (method === "GET" && p === "/api/admin/courses") {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    return send(res, 200, { courses: (await db.listCourses(true)).map(toPublicCourse) });
  }
  if (method === "POST" && p === "/api/admin/courses") {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    try {
      const course = await db.upsertCourse(body || {});
      return send(res, 200, { course: toPublicCourse(course) });
    } catch (e) {
      return send(res, 400, { error: e.message });
    }
  }
  if (method === "DELETE" && p.startsWith("/api/admin/courses/")) {
    const a = admin();
    if (a.err) return send(res, a.err[0], a.err[1]);
    try {
      await db.deleteCourse(decodeURIComponent(p.slice("/api/admin/courses/".length)));
      return send(res, 200, { ok: true });
    } catch (e) {
      return send(res, 400, { error: e.message });
    }
  }

  return send(res, 404, { error: "API not found" });
}

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url || "/", `http://${req.headers.host || "localhost"}`);
    if (url.pathname.startsWith("/api/")) return await handleApi(req, res, url);
    return await serveStatic(req, res, url.pathname);
  } catch (e) {
    console.error(e);
    send(res, 500, { error: "Internal error" });
  }
});

server.listen(PORT, () => {
  console.log("");
  console.log("  NEXA listo → http://localhost:" + PORT);
  console.log("  Alumno: demo@nexa.academy / demo1234");
  console.log("  Admin:  admin@nexa.academy / admin1234");
  console.log("  (sin npm install — solo Node.js)");
  console.log("");
});
