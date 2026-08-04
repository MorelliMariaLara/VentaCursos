# NEXA — venta de cursos (simple)

Plataforma web liviana para vender cursos con:

- Catálogo y checkout
- Mercado Pago (o pago simulado en local)
- Aula con video cifrado (AES)
- Certificados
- Panel admin

## Stack

- **Node.js + Express**
- **HTML / CSS / JS** (sin React, sin Next, sin Tailwind)
- Base de datos en archivo: `data/store.json`
- Videos en `content/videos/` (no públicos)

Dependencias npm: **`express`** + **`bcryptjs`**.

## Arranque

```bash
npm install
npm run dev
```

http://localhost:3000

### Windows

```powershell
git pull origin main
npm install
npm run dev
```

## Cuentas

- Alumno: `demo@nexa.academy` / `demo1234`
- Admin: `admin@nexa.academy` / `admin1234`

## Mercado Pago (opcional)

Copiá `.env.example` a `.env` y completá:

```
MP_PUBLIC_KEY=...
MP_ACCESS_TOKEN=...
```

Sin credenciales, el checkout ofrece **Simular pago**.
