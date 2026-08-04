# NEXA — Venta de cursos y certificaciones

Plataforma web para vender cursos en video y emitir certificaciones, con **reproducción cifrada** para dificultar la descarga y la captura de pantalla.

## Qué incluye

- Catálogo de cursos y certificaciones
- Registro / login con sesión JWT en cookie httpOnly
- **Checkout Bricks de Mercado Pago** (tarjetas, ticket, transferencia, dinero en cuenta)
- Webhook de confirmación de pagos
- Panel de administración (`/admin`) para cursos, órdenes y usuarios
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
- `mercadopago` + `@mercadopago/sdk-react` (Checkout Bricks)

## Cómo correr

```bash
cp .env.example .env.local
npm install
npm run dev
```

Abrí [http://localhost:3000](http://localhost:3000).

Cuentas demo:
- Alumno: `demo@nexa.academy` / `demo1234`
- Admin: `admin@nexa.academy` / `admin1234`

## Mercado Pago

1. Creá una aplicación en el [panel de desarrolladores](https://www.mercadopago.com.ar/developers/panel)
2. Copiá la **Public Key** y el **Access Token** de prueba
3. Configurá en `.env.local`:
   - `NEXT_PUBLIC_MP_PUBLIC_KEY`
   - `MP_ACCESS_TOKEN`
   - `APP_URL` (tu dominio público)
   - `MP_WEBHOOK_URL` (opcional, ej. `https://tu-dominio.com/api/webhooks/mercadopago`)
4. Sin credenciales, en desarrollo podés usar **simulación de pago** (`MP_ALLOW_SIMULATE=true`)

## Flujo de prueba

1. Ingresá como alumno o admin
2. Comprá un curso → `/checkout/[slug]` (Payment Brick)
3. Con pago aprobado, entrá al aula y reproducí una lección
4. Completá todas las lecciones para emitir el certificado
5. Como admin, gestioná catálogo y órdenes en `/admin`

## Límites reales de protección

Ninguna web app puede impedir al 100% la grabación de pantalla a nivel sistema operativo. NEXA eleva el costo de piratería con cifrado de sesión, watermark y bloqueos en el browser. Para máxima protección en producción conviene sumar **DRM comercial** (Widevine / FairPlay / PlayReady) y CDN de video.
