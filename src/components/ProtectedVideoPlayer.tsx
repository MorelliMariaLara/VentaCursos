"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { decryptAesCtr } from "@/lib/client-decrypt";

type Props = {
  slug: string;
  lessonId: string;
  onCompleted?: () => void;
};

type SessionPayload = {
  streamToken: string;
  contentKey: string;
  contentIv: string;
  watermark: { label: string; code: string };
  lesson: { title: string; moduleTitle: string };
};

export function ProtectedVideoPlayer({ slug, lessonId, onCompleted }: Props) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const blobUrlRef = useRef<string | null>(null);
  const [session, setSession] = useState<SessionPayload | null>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "blocked" | "error">(
    "loading",
  );
  const [message, setMessage] = useState("Preparando sesión cifrada…");
  const [progress, setProgress] = useState(0);
  const [captureWarning, setCaptureWarning] = useState<string | null>(null);

  const revokeBlob = () => {
    if (blobUrlRef.current) {
      URL.revokeObjectURL(blobUrlRef.current);
      blobUrlRef.current = null;
    }
  };

  const blockPlayback = useCallback((reason: string) => {
    const video = videoRef.current;
    if (video) {
      video.pause();
      video.removeAttribute("src");
      video.load();
    }
    revokeBlob();
    setStatus("blocked");
    setCaptureWarning(reason);
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function boot() {
      setStatus("loading");
      setMessage("Autenticando acceso a la lección…");
      setProgress(8);
      revokeBlob();

      try {
        const sessionRes = await fetch("/api/stream/session", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ slug, lessonId }),
        });
        if (!sessionRes.ok) {
          const err = await sessionRes.json().catch(() => ({}));
          throw new Error(err.error ?? "Sin permiso para reproducir");
        }
        const sessionData = (await sessionRes.json()) as SessionPayload;
        if (cancelled) return;
        setSession(sessionData);

        setMessage("Descargando segmentos cifrados…");
        setProgress(25);
        const mediaRes = await fetch(
          `/api/stream/media?token=${encodeURIComponent(sessionData.streamToken)}`,
        );
        if (!mediaRes.ok) {
          throw new Error("No se pudo obtener el stream cifrado");
        }

        const encrypted = await mediaRes.arrayBuffer();
        if (cancelled) return;
        setMessage("Descifrando en el dispositivo…");
        setProgress(70);

        const plain = await decryptAesCtr(
          encrypted,
          sessionData.contentKey,
          sessionData.contentIv,
          0,
        );
        if (cancelled) return;

        const blob = new Blob([plain], { type: "video/mp4" });
        const url = URL.createObjectURL(blob);
        blobUrlRef.current = url;

        const video = videoRef.current;
        if (video) {
          video.src = url;
          video.load();
        }
        setProgress(100);
        setStatus("ready");
        setMessage("Listo");
      } catch (e) {
        if (cancelled) return;
        setStatus("error");
        setMessage(e instanceof Error ? e.message : "Error de reproducción");
      }
    }

    boot();
    return () => {
      cancelled = true;
      revokeBlob();
    };
  }, [slug, lessonId]);

  useEffect(() => {
    const onContext = (e: Event) => e.preventDefault();
    const onKey = (e: KeyboardEvent) => {
      const key = e.key.toLowerCase();
      if (
        (e.ctrlKey && ["s", "u", "p"].includes(key)) ||
        key === "printscreen" ||
        (e.metaKey && key === "s")
      ) {
        e.preventDefault();
        blockPlayback(
          "Se bloqueó un intento de captura o guardado. El video se pausó por seguridad.",
        );
      }
    };

    const onVisibility = () => {
      if (document.hidden) {
        videoRef.current?.pause();
      }
    };

    document.addEventListener("contextmenu", onContext);
    document.addEventListener("keydown", onKey);
    document.addEventListener("visibilitychange", onVisibility);

    // Patch getDisplayMedia to detect screen recording / sharing attempts.
    const md = navigator.mediaDevices;
    const original = md?.getDisplayMedia?.bind(md);
    if (md && original) {
      md.getDisplayMedia = async (...args: Parameters<typeof original>) => {
        blockPlayback(
          "Se detectó un intento de compartir o grabar pantalla. Reproducción suspendida.",
        );
        throw new DOMException(
          "Captura de pantalla no permitida en NEXA",
          "NotAllowedError",
        );
      };
    }

    return () => {
      document.removeEventListener("contextmenu", onContext);
      document.removeEventListener("keydown", onKey);
      document.removeEventListener("visibilitychange", onVisibility);
      if (md && original) md.getDisplayMedia = original;
    };
  }, [blockPlayback]);

  return (
    <div className="player-shell">
      <div className="player-meta">
        <p className="player-kicker">Stream cifrado AES-256-CTR</p>
        <h2>{session?.lesson.title ?? "Cargando lección"}</h2>
        <p>{session?.lesson.moduleTitle}</p>
      </div>

      <div
        className="player-stage"
        onContextMenu={(e) => e.preventDefault()}
        onDragStart={(e) => e.preventDefault()}
      >
        {status === "loading" && (
          <div className="player-overlay">
            <div className="player-spinner" />
            <p>{message}</p>
            <div className="player-bar">
              <span style={{ width: `${progress}%` }} />
            </div>
          </div>
        )}

        {status === "error" && (
          <div className="player-overlay player-overlay-error">
            <p>{message}</p>
          </div>
        )}

        {status === "blocked" && (
          <div className="player-overlay player-overlay-error">
            <p>{captureWarning}</p>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => window.location.reload()}
            >
              Reanudar con protección
            </button>
          </div>
        )}

        <video
          ref={videoRef}
          controls
          controlsList="nodownload noplaybackrate noremoteplayback"
          disablePictureInPicture
          playsInline
          preload="metadata"
          className="player-video"
          onEnded={() => onCompleted?.()}
          onPause={() => {
            /* keep watermark visible while paused */
          }}
        />

        {session && status === "ready" && (
          <div className="player-watermark" aria-hidden>
            <span>
              {session.watermark.label} · {session.watermark.code}
            </span>
            <span>
              {session.watermark.label} · {session.watermark.code}
            </span>
            <span>
              {session.watermark.label} · {session.watermark.code}
            </span>
          </div>
        )}
      </div>

      <ul className="player-shields">
        <li>URL del video oculta al navegador</li>
        <li>Descifrado solo en memoria (blob temporal)</li>
        <li>Marca de agua dinámica por usuario</li>
        <li>Bloqueo de menú, guardado y captura de pantalla detectada</li>
      </ul>
    </div>
  );
}
