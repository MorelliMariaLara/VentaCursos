import { AuthForm } from "@/components/AuthForm";

export default function RegisterPage() {
  return (
    <div className="auth-wrap">
      <p className="eyebrow">Crear cuenta</p>
      <h1 className="page-title">Registrate en NEXA</h1>
      <p className="muted" style={{ marginBottom: "1rem" }}>
        Con tu cuenta podés comprar cursos y obtener certificaciones.
      </p>
      <AuthForm mode="register" />
    </div>
  );
}
