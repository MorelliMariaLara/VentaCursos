# Cómo abrir NEXA como proyecto de inicio

Este repositorio **es el proyecto web**.  
Carpeta a abrir: la raíz `VentaCursos` (donde está `package.json` y `NEXA.code-workspace`).

---

## Opción 1 — Doble clic (Windows)

1. Abrí la carpeta del repo  
2. Ejecutá **`iniciar.bat`**  
3. Se instala lo necesario (si falta) y abre **http://localhost:3000**

---

## Opción 2 — Cursor / VS Code (recomendado)

1. Abrí el archivo **`NEXA.code-workspace`** (o “Open Folder” sobre `VentaCursos`)  
2. En la terminal integrada:

```bash
npm install
npm run solution:start
```

3. O usá **Run and Debug** → configuración  
   **“NEXA Web (proyecto de inicio)”** → Start (F5)

4. Abrí el navegador en **http://localhost:3000**

---

## Opción 3 — Terminal

```bash
cd VentaCursos
./iniciar.sh          # Mac / Linux
# o
npm install
npm run dev
```

---

## Cuentas de prueba

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | `demo@nexa.academy` | `demo1234` |
| Admin | `admin@nexa.academy` | `admin1234` |

---

## Qué vas a ver

| URL | Qué es |
| --- | --- |
| `/` | Landing NEXA |
| `/cursos` | Catálogo |
| `/checkout/[slug]` | Pago Mercado Pago |
| `/aprender/[slug]` | Aula con video cifrado |
| `/admin` | Panel administrador |

---

## Requisito

- **Node.js 20+** (LTS): https://nodejs.org  

Si querés pagos reales, completá las claves de Mercado Pago en `.env.local` (ver `.env.example`).
