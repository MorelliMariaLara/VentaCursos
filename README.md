# NEXA — Solución de venta de cursos y certificaciones

Solución web completa para **vender cursos en video**, cobrar con **Mercado Pago Checkout Bricks**, emitir **certificaciones** y proteger el contenido con **video cifrado**.

Incluye tienda, aula del alumno, pagos reales (o simulación) y **panel de administración**.

---

## Módulos de la solución

| Módulo | Ruta / componente | Función |
| --- | --- | --- |
| Catálogo y landing | `/`, `/cursos` | Vitrina de cursos y certificaciones |
| Auth | `/login`, `/registro` | Registro y sesión JWT |
| Checkout Mercado Pago | `/checkout/[slug]` | Payment Brick + preferencias + webhook |
| Aula protegida | `/aprender/[slug]` | Player AES-256-CTR + watermark |
| Certificados | `/certificado/[slug]` | Emisión al completar el curso |
| Admin | `/admin` | Cursos, órdenes, usuarios, ingresos |

---

## Arranque rápido (una solución)

### Opción A — Desarrollo local

```bash
./scripts/start.sh setup   # crea .env, instala deps, videos demo
./scripts/start.sh dev     # http://localhost:3000
```

O con Make:

```bash
make setup && make dev
```

### Opción B — Docker (solución empaquetada)

```bash
cp .env.example .env
# Editá credenciales MP si las tenés
./scripts/start.sh docker
# o: make docker
```

App en [http://localhost:3000](http://localhost:3000).

```bash
./scripts/start.sh logs    # ver logs
./scripts/start.sh down    # detener
```

---

## Cuentas demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |

Para regenerar la base local:

```bash
npm run seed
```

---

## Variables de entorno

Copiá `.env.example` → `.env` / `.env.local`:

| Variable | Uso |
| --- | --- |
| `AUTH_SECRET` | Firma de sesiones |
| `STREAM_SECRET` | Tokens del stream cifrado |
| `APP_URL` | URL pública (back_urls MP) |
| `NEXT_PUBLIC_MP_PUBLIC_KEY` | Public Key Mercado Pago |
| `MP_ACCESS_TOKEN` | Access Token Mercado Pago |
| `MP_WEBHOOK_URL` | Webhook de pagos (opcional) |
| `MP_ALLOW_SIMULATE` | `true` permite simular pagos sin credenciales |

Credenciales: [panel de desarrolladores de Mercado Pago](https://www.mercadopago.com.ar/developers/panel).

---

## Flujo de negocio

1. El alumno se registra e inicia sesión  
2. Elige un curso y paga en `/checkout/[slug]` (Checkout Bricks)  
3. Con pago **aprobado** (o webhook) se habilita el aula  
4. Reproduce lecciones cifradas; al 100% se emite certificado  
5. El admin gestiona catálogo y ve órdenes/ingresos en `/admin`

---

## Estructura del proyecto

```
├── content/videos/          # Videos fuente (solo servidor)
├── data/                    # DB JSON persistente (runtime)
├── scripts/
│   ├── setup.sh             # Setup de la solución
│   ├── start.sh             # Orquestador setup/dev/docker
│   ├── seed.ts              # Seed de usuarios y catálogo
│   └── generate-demo-videos.sh
├── src/
│   ├── app/                 # Rutas Next.js (tienda, aula, admin, APIs)
│   ├── components/          # UI + Payment Brick + player
│   └── lib/                 # Auth, DB, Mercado Pago, cifrado
├── Dockerfile
├── docker-compose.yml
├── Makefile
└── .env.example
```

---

## Scripts npm

```bash
npm run dev              # desarrollo
npm run build && npm start
npm run seed             # reset DB demo
npm run solution:setup   # setup
npm run solution:up      # docker compose up
npm run solution:down    # docker compose down
```

---

## Stack

- Next.js (App Router) + TypeScript + Tailwind CSS  
- Persistencia `data/store.json`  
- `jose` + `bcryptjs` (auth)  
- `mercadopago` + `@mercadopago/sdk-react` (Checkout Bricks)  
- Docker / Docker Compose para despliegue

---

## Protección de video

- URL fuente nunca llega al browser  
- Stream **AES-256-CTR** por sesión  
- Descifrado en memoria (blob temporal)  
- Marca de agua por usuario  
- Bloqueo de menú/guardado y detección de captura (`getDisplayMedia`)

Ninguna web puede impedir al 100% la grabación a nivel SO. Para máxima protección en producción: DRM (Widevine / FairPlay / PlayReady) + CDN.
