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
| **Public Key** | Frontend (Payment Brick) | `TEST-...` o `APP_USR-...` |
| **Access Token** | Backend (API) | empieza con `APP_USR-...` o `TEST-...` |

> Usá siempre las de **prueba** hasta validar. Después cambiá por las de **producción**.

## 3. Pegarlas en el proyecto

Creá un archivo `.env` en la raíz del repo (junto a `NEXA.sln`):

```env
MP_PUBLIC_KEY=APP_USR-xxxxxxxx  # Public Key de prueba
MP_ACCESS_TOKEN=APP_USR-xxxxxxxx  # Access Token de prueba
APP_URL=http://localhost:5000
MP_ALLOW_SIMULATE=false
```

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

1. Alumno compra un curso → se crea orden `pending` + **preference**.
2. Se muestra el **Payment Brick** (tarjeta + **Mercado Pago / QR**).
3. Al pagar, el backend llama a la API de pagos con tu **Access Token**.
4. **Solo si el estado es `approved` (acreditado)** se habilitan video y lecciones.
5. Si queda pendiente (QR sin pagar), la pantalla espera y consulta el estado hasta acreditar.

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

## 6. No uses el SDK PHP

La doc de MP a veces muestra:

```php
MercadoPagoConfig::setAccessToken("TEST_ACCESS_TOKEN");
```

Este proyecto es **ASP.NET Core**: no hace falta SDK PHP. El Access Token se lee de `MP_ACCESS_TOKEN` y se envía en el header `Authorization: Bearer ...` desde `PaymentService`.

## Endpoints

| Método | Ruta | Uso |
| --- | --- | --- |
| GET | `/api/payments/config` | Public Key al front |
| POST | `/api/payments/preference` | Preference para el Brick / QR |
| POST | `/api/payments/process` | Procesa el pago del Brick |
| GET | `/api/payments/order/{id}` | Polling hasta acreditar |
| POST/GET | `/api/webhooks/mercadopago` | Notificaciones de MP |
