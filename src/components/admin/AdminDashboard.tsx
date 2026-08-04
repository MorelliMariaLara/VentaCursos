"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { formatPrice } from "@/lib/format";

type Stats = {
  users: number;
  courses: number;
  enrollments: number;
  orders: number;
  paidOrders: number;
  revenue: number;
  pendingOrders: number;
};

type CourseRow = {
  id: string;
  slug: string;
  title: string;
  category: string;
  level: string;
  price: number;
  currency: string;
  published: boolean;
  instructor: string;
  subtitle: string;
  description: string;
  durationHours: number;
  includesCertificate: boolean;
  certificateName: string;
};

type OrderRow = {
  id: string;
  status: string;
  amount: number;
  currency: string;
  createdAt: string;
  paymentId?: string;
  paymentMethod?: string;
  simulated?: boolean;
  userEmail: string;
  userName: string;
  courseTitle: string;
};

type UserRow = {
  id: string;
  name: string;
  email: string;
  role: string;
  createdAt: string;
  coursesOwned: number;
};

type Tab = "resumen" | "cursos" | "ordenes" | "usuarios";

const emptyCourse = {
  title: "",
  slug: "",
  subtitle: "",
  description: "",
  category: "General",
  level: "Inicial",
  price: 50000,
  currency: "ARS",
  durationHours: 8,
  instructor: "Equipo NEXA",
  certificateName: "",
  published: true,
};

export function AdminDashboard() {
  const [tab, setTab] = useState<Tab>("resumen");
  const [stats, setStats] = useState<Stats | null>(null);
  const [courses, setCourses] = useState<CourseRow[]>([]);
  const [orders, setOrders] = useState<OrderRow[]>([]);
  const [users, setUsers] = useState<UserRow[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState(emptyCourse);
  const [editingId, setEditingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    const [s, c, o, u] = await Promise.all([
      fetch("/api/admin/stats"),
      fetch("/api/admin/courses"),
      fetch("/api/admin/orders"),
      fetch("/api/admin/users"),
    ]);
    if ([s, c, o, u].some((r) => r.status === 401 || r.status === 403)) {
      setError("Necesitás permisos de administrador.");
      return;
    }
    const sj = await s.json();
    const cj = await c.json();
    const oj = await o.json();
    const uj = await u.json();
    setStats(sj);
    setCourses(cj.courses ?? []);
    setOrders(oj.orders ?? []);
    setUsers(uj.users ?? []);
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function onSaveCourse(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    const payload = {
      ...form,
      id: editingId ?? undefined,
      price: Number(form.price),
      durationHours: Number(form.durationHours),
      certificateName:
        form.certificateName || `Certificación NEXA en ${form.title}`,
      learningOutcomes: [
        "Completar el temario del curso",
        "Aplicar los conceptos en un caso práctico",
      ],
      modules: editingId
        ? undefined
        : [
            {
              id: `mod-${Date.now()}`,
              title: "Módulo introductorio",
              lessons: [
                {
                  id: `les-${Date.now()}`,
                  title: "Bienvenida",
                  durationMinutes: 10,
                  sourceUrl: "local:lesson-a.mp4",
                  order: 1,
                },
              ],
            },
          ],
    };

    const res = await fetch("/api/admin/courses", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    const data = await res.json().catch(() => ({}));
    setSaving(false);
    if (!res.ok) {
      setError(data.error ?? "No se pudo guardar el curso");
      return;
    }
    setForm(emptyCourse);
    setEditingId(null);
    await load();
  }

  function editCourse(course: CourseRow) {
    setEditingId(course.id);
    setForm({
      title: course.title,
      slug: course.slug,
      subtitle: course.subtitle,
      description: course.description,
      category: course.category,
      level: course.level,
      price: course.price,
      currency: course.currency,
      durationHours: course.durationHours,
      instructor: course.instructor,
      certificateName: course.certificateName,
      published: course.published,
    });
    setTab("cursos");
  }

  async function removeCourse(id: string) {
    if (!confirm("¿Eliminar este curso del catálogo?")) return;
    const res = await fetch(`/api/admin/courses?id=${id}`, { method: "DELETE" });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      setError(data.error ?? "No se pudo eliminar");
      return;
    }
    await load();
  }

  async function togglePublish(course: CourseRow) {
    const res = await fetch("/api/admin/courses", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        id: course.id,
        slug: course.slug,
        title: course.title,
        price: course.price,
        published: !course.published,
      }),
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      setError(data.error ?? "No se pudo actualizar");
      return;
    }
    await load();
  }

  return (
    <div className="admin-shell">
      <aside className="admin-nav">
        <p className="eyebrow">Panel</p>
        <h1>Administración</h1>
        {(
          [
            ["resumen", "Resumen"],
            ["cursos", "Cursos"],
            ["ordenes", "Órdenes"],
            ["usuarios", "Usuarios"],
          ] as const
        ).map(([id, label]) => (
          <button
            key={id}
            type="button"
            className={tab === id ? "admin-tab active" : "admin-tab"}
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
      </aside>

      <section className="admin-main">
        {error && <p className="form-error">{error}</p>}

        {tab === "resumen" && stats && (
          <div className="admin-stats">
            <article>
              <span>Ingresos cobrados</span>
              <strong>{formatPrice(stats.revenue, "ARS")}</strong>
            </article>
            <article>
              <span>Órdenes pagadas</span>
              <strong>{stats.paidOrders}</strong>
            </article>
            <article>
              <span>Pendientes</span>
              <strong>{stats.pendingOrders}</strong>
            </article>
            <article>
              <span>Inscripciones</span>
              <strong>{stats.enrollments}</strong>
            </article>
            <article>
              <span>Usuarios</span>
              <strong>{stats.users}</strong>
            </article>
            <article>
              <span>Cursos</span>
              <strong>{stats.courses}</strong>
            </article>
          </div>
        )}

        {tab === "cursos" && (
          <div className="admin-grid">
            <form className="admin-form" onSubmit={onSaveCourse}>
              <h2>{editingId ? "Editar curso" : "Nuevo curso"}</h2>
              <label>
                Título
                <input
                  required
                  value={form.title}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      title: e.target.value,
                      slug:
                        editingId
                          ? f.slug
                          : e.target.value
                              .toLowerCase()
                              .normalize("NFD")
                              .replace(/[\u0300-\u036f]/g, "")
                              .replace(/[^a-z0-9]+/g, "-")
                              .replace(/(^-|-$)/g, ""),
                    }))
                  }
                />
              </label>
              <label>
                Slug
                <input
                  required
                  value={form.slug}
                  onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value }))}
                />
              </label>
              <label>
                Subtítulo
                <input
                  value={form.subtitle}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, subtitle: e.target.value }))
                  }
                />
              </label>
              <label>
                Descripción
                <textarea
                  rows={3}
                  value={form.description}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, description: e.target.value }))
                  }
                />
              </label>
              <div className="admin-form-row">
                <label>
                  Precio
                  <input
                    type="number"
                    required
                    value={form.price}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, price: Number(e.target.value) }))
                    }
                  />
                </label>
                <label>
                  Moneda
                  <select
                    value={form.currency}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, currency: e.target.value }))
                    }
                  >
                    <option value="ARS">ARS</option>
                    <option value="USD">USD</option>
                  </select>
                </label>
              </div>
              <div className="admin-form-row">
                <label>
                  Categoría
                  <input
                    value={form.category}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, category: e.target.value }))
                    }
                  />
                </label>
                <label>
                  Nivel
                  <select
                    value={form.level}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, level: e.target.value }))
                    }
                  >
                    <option>Inicial</option>
                    <option>Intermedio</option>
                    <option>Avanzado</option>
                  </select>
                </label>
              </div>
              <label>
                Instructor/a
                <input
                  value={form.instructor}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, instructor: e.target.value }))
                  }
                />
              </label>
              <label className="admin-check">
                <input
                  type="checkbox"
                  checked={form.published}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, published: e.target.checked }))
                  }
                />
                Publicado en el catálogo
              </label>
              <div style={{ display: "flex", gap: "0.6rem" }}>
                <button className="btn" type="submit" disabled={saving}>
                  {saving ? "Guardando…" : editingId ? "Actualizar" : "Crear curso"}
                </button>
                {editingId && (
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => {
                      setEditingId(null);
                      setForm(emptyCourse);
                    }}
                  >
                    Cancelar
                  </button>
                )}
              </div>
            </form>

            <div className="admin-table-wrap">
              <h2>Catálogo ({courses.length})</h2>
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Curso</th>
                    <th>Precio</th>
                    <th>Estado</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {courses.map((course) => (
                    <tr key={course.id}>
                      <td>
                        <strong>{course.title}</strong>
                        <div className="muted">{course.slug}</div>
                      </td>
                      <td>{formatPrice(course.price, course.currency)}</td>
                      <td>{course.published ? "Publicado" : "Oculto"}</td>
                      <td className="admin-actions">
                        <button type="button" onClick={() => editCourse(course)}>
                          Editar
                        </button>
                        <button type="button" onClick={() => togglePublish(course)}>
                          {course.published ? "Ocultar" : "Publicar"}
                        </button>
                        <button type="button" onClick={() => removeCourse(course.id)}>
                          Borrar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {tab === "ordenes" && (
          <div className="admin-table-wrap">
            <h2>Órdenes Mercado Pago</h2>
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Alumno</th>
                  <th>Curso</th>
                  <th>Monto</th>
                  <th>Estado</th>
                  <th>Pago</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id}>
                    <td>{new Date(order.createdAt).toLocaleString("es-AR")}</td>
                    <td>
                      {order.userName}
                      <div className="muted">{order.userEmail}</div>
                    </td>
                    <td>{order.courseTitle}</td>
                    <td>{formatPrice(order.amount, order.currency)}</td>
                    <td>
                      <span className={`pill status-${order.status}`}>
                        {order.status}
                      </span>
                    </td>
                    <td>
                      {order.simulated
                        ? "Simulado"
                        : order.paymentId || order.paymentMethod || "—"}
                    </td>
                  </tr>
                ))}
                {orders.length === 0 && (
                  <tr>
                    <td colSpan={6}>Todavía no hay órdenes.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {tab === "usuarios" && (
          <div className="admin-table-wrap">
            <h2>Usuarios</h2>
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Email</th>
                  <th>Rol</th>
                  <th>Cursos</th>
                  <th>Alta</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>{user.name}</td>
                    <td>{user.email}</td>
                    <td>{user.role}</td>
                    <td>{user.coursesOwned}</td>
                    <td>{new Date(user.createdAt).toLocaleDateString("es-AR")}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
