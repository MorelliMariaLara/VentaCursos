async function api(path, options = {}) {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    credentials: "same-origin",
    ...options,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(data.error || "Error");
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

function money(n, currency = "ARS") {
  return new Intl.NumberFormat("es-AR", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(n);
}

function qs(name) {
  return new URLSearchParams(location.search).get(name);
}

async function getUser() {
  const { user } = await api("/api/auth/me");
  return user;
}

async function renderHeader() {
  const el = document.getElementById("site-header");
  if (!el) return;
  let user = null;
  try { user = await getUser(); } catch { /* ignore */ }
  el.innerHTML = `
    <div class="wrap">
      <a class="brand" href="/">NEXA</a>
      <nav class="nav">
        <a href="/cursos.html">Cursos</a>
        ${user ? `<a href="/mis-cursos.html">Mis cursos</a>` : ""}
        ${user?.role === "admin" ? `<a href="/admin.html">Admin</a>` : ""}
        ${user
          ? `<span class="hint">${user.name}</span><button class="btn btn-ghost" id="logoutBtn" type="button">Salir</button>`
          : `<a href="/login.html">Ingresar</a><a class="btn btn-primary" href="/registro.html">Crear cuenta</a>`}
      </nav>
    </div>`;
  const btn = document.getElementById("logoutBtn");
  if (btn) {
    btn.onclick = async () => {
      await api("/api/auth/logout", { method: "POST", body: "{}" });
      location.href = "/";
    };
  }
}

document.addEventListener("DOMContentLoaded", renderHeader);
