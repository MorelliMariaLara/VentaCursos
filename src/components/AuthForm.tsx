"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";

type Mode = "login" | "register";

export function AuthForm({
  mode,
  nextPath = "/mis-cursos",
}: {
  mode: Mode;
  nextPath?: string;
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    const form = new FormData(e.currentTarget);
    const payload = {
      name: String(form.get("name") ?? ""),
      email: String(form.get("email") ?? ""),
      password: String(form.get("password") ?? ""),
    };

    const endpoint = mode === "login" ? "/api/auth/login" : "/api/auth/register";
    const body =
      mode === "login"
        ? { email: payload.email, password: payload.password }
        : payload;

    const res = await fetch(endpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const data = await res.json().catch(() => ({}));
    setLoading(false);
    if (!res.ok) {
      setError(data.error ?? "No se pudo continuar");
      return;
    }
    router.push(nextPath);
    router.refresh();
  }

  return (
    <form className="auth-form" onSubmit={onSubmit}>
      {mode === "register" && (
        <label>
          Nombre
          <input name="name" required minLength={2} placeholder="Tu nombre" />
        </label>
      )}
      <label>
        Correo
        <input
          name="email"
          type="email"
          required
          placeholder="vos@empresa.com"
          defaultValue={mode === "login" ? "demo@nexa.academy" : ""}
        />
      </label>
      <label>
        Contraseña
        <input
          name="password"
          type="password"
          required
          minLength={mode === "register" ? 8 : 1}
          placeholder={mode === "login" ? "demo1234" : "Mínimo 8 caracteres"}
          defaultValue={mode === "login" ? "demo1234" : ""}
        />
      </label>
      {error && <p className="form-error">{error}</p>}
      <button className="btn" type="submit" disabled={loading}>
        {loading
          ? "Esperá…"
          : mode === "login"
            ? "Ingresar"
            : "Crear cuenta"}
      </button>
      <p className="form-switch">
        {mode === "login" ? (
          <>
            ¿No tenés cuenta? <Link href="/registro">Registrate</Link>
          </>
        ) : (
          <>
            ¿Ya tenés cuenta? <Link href="/login">Ingresá</Link>
          </>
        )}
      </p>
    </form>
  );
}
