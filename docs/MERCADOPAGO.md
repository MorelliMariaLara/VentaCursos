# Mercado Pago · Checkout Bricks (QR + tarjeta)

## Qué hace el flujo

1. El alumno entra a **Comprar** → `/Checkout?slug=...`
2. El backend crea una **orden pending** y una **preference** de Mercado Pago.
3. El front monta el **Payment Brick** con:
   - tarjetas
   - transferencia / ticket
   - **Mercado Pago (QR / dinero en cuenta)** → `mercadoPago: "all"` + `preferenceId`
4. Al pagar, se llama `/api/payments/process`.
5. **Solo si el pago queda `approved` (acreditado)** se crea la inscripción y se habilitan video/lecciones.
6. Si queda `pending` (QR sin pagar aún):
   - se muestra el Status Screen Brick
   - el front hace **polling** a `/api/payments/order/{orderId}`
   - el **webhook** `/api/webhooks/mercadopago` también acredita cuando MP notifica

## Credenciales

En el [panel de desarrolladores de Mercado Pago](https://www.mercadopago.com.ar/developers):

1. Creá una aplicación
2. Copiá **Public Key** y **Access Token** (TEST o producción)
3. Ponelos en `.env` o `appsettings.json`:

```env
MP_PUBLIC_KEY=TEST-xxxxxxxx
MP_ACCESS_TOKEN=TEST-xxxxxxxx
APP_URL=https://tu-dominio-publico
MP_ALLOW_SIMULATE=false
```

> En local, sin claves válidas, sigue apareciendo **Simular pago**.  
> El webhook de MP **no llega a localhost**; por eso el polling consulta el estado del pago.

## Webhook (producción)

URL a configurar en Mercado Pago:

```text
https://TU-DOMINIO/api/webhooks/mercadopago
```

Opcional en `.env`:

```env
MP_WEBHOOK_URL=https://TU-DOMINIO/api/webhooks/mercadopago
```

## Endpoints

| Método | Ruta | Uso |
| --- | --- | --- |
| GET | `/api/payments/config` | publicKey + si hay MP configurado |
| POST | `/api/payments/preference` | Crea orden + preference (Brick QR) |
| POST | `/api/payments/process` | Procesa formData del Brick |
| GET | `/api/payments/order/{id}` | Polling de acreditación |
| POST/GET | `/api/webhooks/mercadopago` | Notificaciones IPN/Webhooks |
