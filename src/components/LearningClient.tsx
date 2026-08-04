"use client";

import { useMemo, useState } from "react";
import { ProtectedVideoPlayer } from "./ProtectedVideoPlayer";
import Link from "next/link";

type Lesson = {
  id: string;
  title: string;
  durationMinutes: number;
  order: number;
};

type Module = {
  id: string;
  title: string;
  lessons: Lesson[];
};

type Props = {
  slug: string;
  courseTitle: string;
  certificateName: string;
  modules: Module[];
  initialProgress: Record<string, boolean>;
  certificateCode?: string | null;
  certificateIssuedAt?: string | null;
};

export function LearningClient({
  slug,
  courseTitle,
  certificateName,
  modules,
  initialProgress,
  certificateCode,
  certificateIssuedAt,
}: Props) {
  const lessons = useMemo(
    () => modules.flatMap((m) => m.lessons.map((l) => ({ ...l, moduleTitle: m.title }))),
    [modules],
  );
  const [activeId, setActiveId] = useState(lessons[0]?.id ?? "");
  const [progress, setProgress] = useState(initialProgress);
  const [certCode, setCertCode] = useState(certificateCode ?? null);
  const [certAt, setCertAt] = useState(certificateIssuedAt ?? null);

  const completed = lessons.filter((l) => progress[l.id]).length;
  const pct = lessons.length ? Math.round((completed / lessons.length) * 100) : 0;

  async function markComplete(lessonId: string) {
    const res = await fetch("/api/progress", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ slug, lessonId }),
    });
    if (!res.ok) return;
    const data = await res.json();
    setProgress(data.progress ?? {});
    if (data.certificateCode) setCertCode(data.certificateCode);
    if (data.certificateIssuedAt) setCertAt(data.certificateIssuedAt);
  }

  return (
    <div className="learn-layout">
      <aside className="learn-sidebar">
        <p className="eyebrow">Aula protegida</p>
        <h1>{courseTitle}</h1>
        <div className="progress-track">
          <div className="progress-fill" style={{ width: `${pct}%` }} />
        </div>
        <p className="progress-label">
          {completed}/{lessons.length} lecciones · {pct}%
        </p>

        {modules.map((mod) => (
          <div key={mod.id} className="lesson-group">
            <h3>{mod.title}</h3>
            <ul>
              {mod.lessons.map((lesson) => (
                <li key={lesson.id}>
                  <button
                    type="button"
                    className={
                      lesson.id === activeId ? "lesson-btn active" : "lesson-btn"
                    }
                    onClick={() => setActiveId(lesson.id)}
                  >
                    <span>{lesson.title}</span>
                    <em>
                      {progress[lesson.id] ? "Hecha" : `${lesson.durationMinutes} min`}
                    </em>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        ))}

        {certCode && (
          <div className="cert-panel">
            <p className="eyebrow">Certificación emitida</p>
            <strong>{certificateName}</strong>
            <p>Código: {certCode}</p>
            {certAt && <p>Fecha: {new Date(certAt).toLocaleDateString("es-AR")}</p>}
            <Link href={`/certificado/${slug}`} className="btn btn-small">
              Ver certificado
            </Link>
          </div>
        )}
      </aside>

      <section className="learn-main">
        {activeId ? (
          <ProtectedVideoPlayer
            key={activeId}
            slug={slug}
            lessonId={activeId}
            onCompleted={() => markComplete(activeId)}
          />
        ) : (
          <p>Seleccioná una lección.</p>
        )}
        {activeId && !progress[activeId] && (
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => markComplete(activeId)}
          >
            Marcar lección como completada
          </button>
        )}
      </section>
    </div>
  );
}
