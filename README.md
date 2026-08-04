# NEXA — venta de cursos (sin paquetes npm)

Plataforma local simple:

- Catálogo y checkout
- Pago simulado (o Mercado Pago si configurás `.env`)
- Aula con video cifrado
- Certificados
- Admin

## Requisitos

Solo **Node.js 20+**.  
**Cero dependencias npm** (`express`, `bcrypt`, etc. no se usan).

## Arranque

```powershell
git pull origin main
node server/index.js
```

http://localhost:3000

## Cuentas

- Alumno: `demo@nexa.academy` / `demo1234`
- Admin: `admin@nexa.academy` / `admin1234`
