import Link from "next/link";
import { getSession } from "@/lib/auth";
import { LogoutButton } from "./LogoutButton";

export async function SiteHeader() {
  const session = await getSession();

  return (
    <header className="site-header">
      <div className="shell header-inner">
        <Link href="/" className="brand">
          <span className="brand-mark" aria-hidden />
          <span className="brand-text">NEXA</span>
        </Link>
        <nav className="nav-links">
          <Link href="/cursos">Cursos</Link>
          <Link href="/certificaciones">Certificaciones</Link>
          {session ? (
            <>
              <Link href="/mis-cursos">Mis cursos</Link>
              {session.role === "admin" && <Link href="/admin">Admin</Link>}
              <span className="nav-user">{session.name.split(" ")[0]}</span>
              <LogoutButton />
            </>
          ) : (
            <>
              <Link href="/login">Ingresar</Link>
              <Link href="/registro" className="btn btn-small">
                Crear cuenta
              </Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
