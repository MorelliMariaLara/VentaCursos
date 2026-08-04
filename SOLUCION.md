# NEXA — Entrega de la solución

## Qué se entrega

Plataforma lista para operar la **venta de cursos y certificaciones en video**, con:

1. **Tienda online** — catálogo, detalle de curso, registro/login  
2. **Cobros con Mercado Pago Checkout Bricks** — tarjetas, ticket, transferencia, dinero en cuenta + webhook  
3. **Aula del alumno** — video cifrado AES-256-CTR, watermark, anti-descarga/captura  
4. **Certificaciones** — emisión automática al completar el curso  
5. **Panel administrador** — cursos, órdenes, usuarios e ingresos  
6. **Empaquetado** — scripts de setup, Docker Compose y variables de entorno documentadas  

## Cómo ponerla en marcha

```bash
./scripts/start.sh setup
./scripts/start.sh dev        # desarrollo
# ó
./scripts/start.sh docker     # solución en contenedor
```

## Accesos de prueba

- Alumno: `demo@nexa.academy` / `demo1234`  
- Admin: `admin@nexa.academy` / `admin1234`  

## Activar pagos reales

1. Obtener Public Key y Access Token en Mercado Pago  
2. Completar `.env` / `.env.local`  
3. Configurar `APP_URL` y `MP_WEBHOOK_URL` con dominio público  
4. Reiniciar la app (`npm run dev` o `./scripts/start.sh docker`)  

Sin credenciales, el checkout ofrece **simulación de pago** (solo desarrollo).

## Alcance y siguientes evoluciones sugeridas

- Subida de videos desde el admin (storage S3/R2)  
- DRM comercial (Widevine/FairPlay)  
- Base de datos SQL (Postgres) en lugar de JSON  
- Multi-instructor y cupones de descuento  
