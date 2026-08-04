import { AuthForm } from "@/components/AuthForm";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;
  return (
    <div className="auth-wrap">
      <p className="eyebrow">Acceso NEXA</p>
      <h1 className="page-title">Ingresar</h1>
      <p className="muted" style={{ marginBottom: "1rem" }}>
        Demo: demo@nexa.academy / demo1234
      </p>
      <AuthForm mode="login" nextPath={next || "/mis-cursos"} />
    </div>
  );
}
