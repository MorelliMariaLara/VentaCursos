# Mercado Pago · crear aplicación y conectar SANTICAZA

## 1. Crear la aplicación (panel de Mercado Pago)

1. Entrá a [Mercado Pago Developers](https://www.mercadopago.com.ar/developers) → **Ingresar**.
2. Andá a **Tus integraciones** → **Crear aplicación**.
3. Completá la verificación de identidad si te la pide.
4. **Nombre** de la app (ej: `SANTICAZA Capacitaciones`), hasta 50 caracteres.
5. Tipo de pago: **Pagos online** → Continuar.
6. Integración: **tienda hecha con desarrollo propio** (podés poner la URL de tu tienda) → Continuar.
7. Producto:
   - Ideal para esta solución: **Checkout Bricks** o **Checkout API** (si aparece).
   - Si el asistente solo ofrece **Checkout Pro**, también sirve: las **credenciales de prueba** (Public Key + Access Token) se usan igual en nuestra integración.
8. Aceptá privacidad y términos → **Confirmar**.

## 2. Credenciales de prueba

En **Tus integraciones** → tu aplicación → **Datos de integración** → **Pruebas** → **Credenciales de prueba**:

| Credencial | Dónde se usa | Ejemplo |
| --- | --- | --- |
| **Public Key** | Frontend (Wallet Brick / Checkout Pro) | `TEST-...` o `APP_USR-...` |
| **Access Token** | Backend (API) | empieza con `APP_USR-...` o `TEST-...` |

> Usá siempre las de **prueba** hasta validar. Después cambiá por las de **producción**.

## 3. Pegarlas en el proyecto

Creá un archivo `.env` en la raíz del repo (junto a `NEXA.sln`):

```env
MP_PUBLIC_KEY=APP_USR-xxxxxxxx
MP_ACCESS_TOKEN=APP_USR-xxxxxxxx
APP_URL=http://localhost:5000
MP_ALLOW_SIMULATE=false
```

### Error: `At least one policy returned UNAUTHORIZED`

Causas frecuentes:

1. **Public Key y Access Token de apps distintas** → copiá ambas de la misma aplicación (Pruebas).
2. **Mezclar `TEST-` con `APP_USR-`** → tienen que ser el mismo par (ambos TEST o ambos producción).
3. **Variables viejas en Windows/IDE** → antes el `.env` no pisaba el entorno; ahora sí. Cerrá la app y abrila de nuevo; en la consola debe decir `PK=TEST-…` y `TK=TEST-…`.
4. **Espacios al pegar** el token → una sola línea, sin comillas.
5. **Webhook a localhost** → en local la app ya **no envía** `notification_url`.
6. No reiniciaste la app después de `git pull` / editar `.env`.

Solución rápida:

```bash
git checkout main
git pull
dotnet run --project Nexa.Web
```

En la consola buscá la línea `MP configurado=True PK=TEST-… TK=TEST-…`. Si sigue `APP_USR`, borrá esas variables del sistema Windows o cerrá Visual Studio y volvé a abrir.

También podés ponerlas en `Nexa.Web/appsettings.Development.json`:

```json
{
  "MP_PUBLIC_KEY": "APP_USR-xxxxxxxx",
  "MP_ACCESS_TOKEN": "APP_USR-xxxxxxxx",
  "APP_URL": "http://localhost:5000",
  "MP_ALLOW_SIMULATE": "false"
}
```

Reiniciá la app (`dotnet run --project Nexa.Web`).

## 4. Qué hace el checkout en SANTICAZA

1. Alumno compra un curso → se crea orden `pending` + **preference** (Checkout Pro).
2. Se muestra el **Wallet Brick** (botón oficial) y un enlace de respaldo a `init_point` (QR / tarjeta / dinero en cuenta).
3. El alumno paga en Mercado Pago; en local **no hay webhook** a localhost.
4. La pantalla hace **polling** (`GET /api/payments/order/{id}`), buscando el pago por `external_reference`.
5. **Solo si el estado es `approved` (acreditado)** se habilitan video y lecciones.

## 5. Webhook (cuando tengas URL pública)

En producción, en la app de MP configurá:

```text
https://TU-DOMINIO/api/webhooks/mercadopago
```

O en `.env`:

```env
MP_WEBHOOK_URL=https://TU-DOMINIO/api/webhooks/mercadopago
APP_URL=https://TU-DOMINIO
```

En `localhost` el webhook de MP no llega; el front hace polling del estado del pago.

## 6. Seguridad (recomendaciones de Mercado Pago)

### Access Token solo por header
Ya está implementado en `PaymentService.MpFetchAsync`:

```http
Authorization: Bearer {MP_ACCESS_TOKEN}
```

- **Nunca** se manda el Access Token por query string (`?access_token=`).
- El Access Token **solo vive en el backend** (`.env` / variables de entorno). El front solo recibe la **Public Key**.
- No lo pegues en JavaScript, URLs del navegador ni capturas públicas.

### ¿Hace falta OAuth?
**No, para SANTICAZA.** OAuth sirve cuando tu plataforma cobra **en nombre de terceros** (varios vendedores/cuentas MP ajenas).

Acá hay **una sola cuenta** (la de SANTICAZA): usás el Access Token de tu propia aplicación. OAuth no aporta nada en este modelo.

### Buenas prácticas extra
1. En producción: sacar `.env` del git, rotar claves y usar secretos del hosting.
2. Webhook solo por HTTPS público (`MP_WEBHOOK_URL` / `APP_URL`).
3. No loguear el token completo (el arranque solo muestra un preview enmascarado).

## 7. No uses el SDK PHP

La doc de MP a veces muestra:

```php
MercadoPagoConfig::setAccessToken("TEST_ACCESS_TOKEN");
```

Este proyecto es **ASP.NET Core**: no hace falta SDK PHP. El Access Token se lee de `MP_ACCESS_TOKEN` y se envía en el header `Authorization: Bearer ...` desde `PaymentService`.

## Endpoints

| Método | Ruta | Uso |
| --- | --- | --- |
| GET | `/api/payments/config` | Public Key al front |
| POST | `/api/payments/preference` | Preference Checkout Pro (`init_point` + Wallet Brick) |
| POST | `/api/payments/process` | Simulación local / legado Brick |
| GET | `/api/payments/order/{id}` | Polling hasta acreditar (busca por `external_reference`) |
| POST/GET | `/api/webhooks/mercadopago` | Notificaciones de MP |
