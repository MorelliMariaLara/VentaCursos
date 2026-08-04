# NEXA — Venta de cursos y certificaciones

Plataforma web para vender cursos en video y emitir certificaciones, con **reproducción cifrada** para dificultar la descarga y la captura de pantalla.

## Qué incluye

- Catálogo de cursos y certificaciones
- Registro / login con sesión JWT en cookie httpOnly
- Checkout demo (pago simulado) e inscripción
- Aula con player protegido:
  - Stream proxied por el servidor (la URL fuente nunca llega al browser)
  - Cifrado **AES-256-CTR** por sesión de lección
  - Descifrado en memoria (blob temporal)
  - Marca de agua dinámica por usuario
  - Bloqueo de menú contextual, atajos de guardado y detección de `getDisplayMedia`
- Certificado con código al completar el 100% del curso

## Stack

- Next.js (App Router) + TypeScript + Tailwind CSS
- Persistencia local en `data/store.json`
- `jose` (JWT) + `bcryptjs` (contraseñas)

## Cómo correr

```bash
npm install
npm run dev
```

Abrí [http://localhost:3000](http://localhost:3000).

Cuenta demo: `demo@nexa.academy` / `demo1234`

## Flujo de prueba

1. Ingresá con la cuenta demo o creá una nueva
2. Comprá un curso desde el catálogo
3. Entrá al aula (`/aprender/[slug]`) y reproducí una lección
4. Completá todas las lecciones para emitir el certificado

## Límites reales de protección

Ninguna web app puede impedir al 100% la grabación de pantalla a nivel sistema operativo. NEXA eleva el costo de piratería con cifrado de sesión, watermark y bloqueos en el browser. Para máxima protección en producción conviene sumar **DRM comercial** (Widevine / FairPlay / PlayReady) y CDN de video.
