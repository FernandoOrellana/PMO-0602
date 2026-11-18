# API PetCare HN – Veterinaria Huellitas

API REST en **Node.js + Express + MySQL** con Arquitectura Limpia, JWT (access + refresh), roles
(Dueño, Veterinario, Administrador), recordatorios automáticos y documentación Swagger/Postman.

## Arquitectura

```
src/
├── domain/              Reglas y constantes puras (sin dependencias externas)
├── application/
│   └── usecases/        Casos de uso: reglas de negocio y autorización por recurso
├── infrastructure/
│   ├── database/        Pool MySQL y transacciones
│   ├── repositories/    Acceso a MySQL (implementan lo que usan los casos de uso)
│   └── services/        Seguridad (bcrypt/JWT), correo, FCM, archivos
├── presentation/
│   ├── http/            Rutas, middlewares (auth, roles, validación, errores), app Express
│   └── docs/            Especificación OpenAPI (Swagger)
├── jobs/                Tareas programadas (recordatorios)
└── container.js         Inyección de dependencias (composición)
migrations/              Esquema SQL versionado
scripts/                 migrate, seed, generate-postman
tests/unit/              Pruebas unitarias (Jest, dobles de los repositorios)
```

Las dependencias apuntan hacia el dominio: los casos de uso reciben sus repositorios y servicios
por parámetro, así se prueban sin base de datos.

## Requisitos

- Node.js 18 o superior
- MySQL 8 accesible desde el equipo de la API

## Instalación

```bash
cd api
npm install
cp .env.example .env     # en Windows: copy .env.example .env
```

Edita `.env`:

| Variable | Descripción |
|---|---|
| `DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASSWORD`, `DB_NAME` | Conexión MySQL (`MPF`) |
| `JWT_ACCESS_SECRET` | Secreto largo y aleatorio para firmar tokens |
| `PUBLIC_BASE_URL` | URL pública (`https://apiveterinaria.orellanawebserver.space`) |
| `SMTP_*` | Servidor de correo para códigos (si está vacío, los correos se imprimen en consola) |
| `FIREBASE_SERVICE_ACCOUNT_PATH` | Ruta al JSON de cuenta de servicio de Firebase (si está vacío, las push se imprimen en consola) |
| `REMINDER_CRON`, `REMINDER_TZ`, `REMINDER_DAYS` | Horario y ventana de los recordatorios |
| `SEED_ADMIN_PASSWORD`, `SEED_DEMO_PASSWORD` | Claves de las cuentas de prueba |

Generar un secreto JWT:

```bash
node -e "console.log(require('crypto').randomBytes(48).toString('hex'))"
```

## Base de datos

```bash
npm run migrate   # crea las tablas en MPF (idempotente)
npm run seed      # catálogos, administrador, veterinarios, dueño demo y productos de ejemplo
```

Cuentas de prueba creadas por el seed:

| Rol | Correo | Clave |
|---|---|---|
| Administrador | `admin@huellitas.hn` | `SEED_ADMIN_PASSWORD` |
| Veterinario | `veterinario@demo.hn` | `SEED_DEMO_PASSWORD` |
| Veterinario 2 | `veterinario2@demo.hn` | `SEED_DEMO_PASSWORD` |
| Dueño | `dueno@demo.hn` | `SEED_DEMO_PASSWORD` |

Los servicios y precios son los del documento: Consulta general Lps. 350, Antirrábica Lps. 300,
Séxtuple Lps. 450, Desparasitación Lps. 200, Baño y peluquería Lps. 400. Los veterinarios, horarios
y productos son **datos de ejemplo**: cámbialos desde el rol Administrador o editando `scripts/seed.js`.

## Ejecución

```bash
npm start         # producción
npm run dev       # recarga automática al cambiar código
npm test          # pruebas unitarias
```

- Salud: `GET /health`
- Swagger UI: `/api-docs` · Especificación: `/api-docs.json`
- Colección Postman: `docs/PetCare-HN.postman_collection.json` (regenerar con `npm run postman`)

## Autenticación

1. `POST /api/auth/register` crea un dueño y envía un código de 6 dígitos al correo.
2. `POST /api/auth/verify-email` valida el código.
3. `POST /api/auth/login` devuelve `accessToken` (30 min por defecto) y `refreshToken` (7 días).
4. Envía `Authorization: Bearer <accessToken>` en cada petición protegida.
5. `POST /api/auth/refresh` rota el refresh token; el anterior queda revocado.
6. `POST /api/auth/logout` revoca el refresh token en el servidor.

Respuestas de error de seguridad: `401` sin token o token vencido, `403` sin permiso para el rol o el
recurso. Las contraseñas y los refresh tokens se guardan solo como hash (bcrypt y SHA-256).

## Endpoints principales

| Módulo | Método y ruta | Rol |
|---|---|---|
| Auth | `POST /api/auth/register`, `verify-email`, `resend-code`, `login`, `refresh`, `logout` | Público |
| Auth | `POST /api/auth/forgot-password`, `reset-password`, `GET /api/auth/me` | Público / autenticado |
| Mascotas | `GET/POST /api/mascotas`, `GET/PUT/DELETE /api/mascotas/{id}` | Dueño (propias) |
| Mascotas | `POST /api/mascotas/{id}/foto` (multipart `foto`) | Dueño |
| Carné | `GET /api/mascotas/{id}/carne`, `GET /api/mascotas/{id}/historial` | Dueño, Veterinario, Admin |
| Vacunas | `POST /api/mascotas/{id}/vacunas` | Veterinario |
| Catálogos | `GET /api/especies`, `/vacunas`, `/servicios`, `/veterinarios` | Autenticado |
| Disponibilidad | `GET /api/veterinarios/{id}/disponibilidad?fecha=YYYY-MM-DD` | Autenticado |
| Citas | `POST /api/citas`, `GET /api/citas`, `GET /api/citas/{id}` | Dueño / Veterinario / Admin según alcance |
| Citas | `PATCH /api/citas/{id}/cancelar` | Dueño |
| Consulta | `POST /api/citas/{id}/consulta` | Veterinario asignado |
| Tienda | `GET /api/productos`, `GET /api/productos/{id}` | Autenticado |
| Pedidos | `POST /api/pedidos`, `GET /api/pedidos`, `GET /api/pedidos/{id}` | Dueño / Admin |
| Pedidos | `PATCH /api/pedidos/{id}/estado` | Administrador |
| Notificaciones | `GET /api/notificaciones`, `PATCH /api/notificaciones/{id}/leer` | Autenticado |
| Dispositivo | `POST /api/usuarios/me/dispositivos` | Autenticado |
| Admin | `/api/admin/productos`, `/api/admin/servicios`, `/api/admin/veterinarios` (CRUD) | Administrador |
| Admin | `PUT /api/admin/veterinarios/{id}/horarios` | Administrador |
| Reportes | `GET /api/admin/reportes/ventas`, `GET /api/admin/reportes/citas` | Administrador |

Formato de respuesta:

```json
{ "success": true, "data": { }, "meta": { "page": 1, "limit": 20, "total": 3, "totalPages": 1 } }
{ "success": false, "error": { "code": "VALIDATION_ERROR", "message": "Datos inválidos", "details": [] } }
```

Las listas son paginadas con `page` y `limit` (máximo 100).

## Recordatorios automáticos

Un job diario (por defecto 08:00, zona horaria de Honduras) hace dos cosas:

- Busca vacunas cuya próxima dosis cae en los próximos 7 días y avisa al dueño.
- Avisa al dueño de las citas del día siguiente.

Cada aviso se guarda en la tabla `notificaciones` y se envía por FCM a los dispositivos registrados.
Cada recordatorio se envía una sola vez.

## Despliegue

Esquema de dos equipos:

- **Equipo de base de datos:** MySQL en `192.168.100.20`, puerto `510`, base `MPF`.
- **Equipo de la API:** Node.js con este proyecto, expuesto en `apiveterinaria.orellanawebserver.space`
  mediante el proxy HTTPS.

Pasos en el equipo de la API:

1. Copiar la carpeta `api/` (sin `node_modules` ni `.env`).
2. `npm install`, crear `.env` con los datos de conexión y el secreto JWT.
3. `npm run migrate` y, la primera vez, `npm run seed`.
4. Ejecutar `npm start` (o usar un gestor como PM2 o un servicio de Windows).
5. Redirigir el dominio hacia el puerto `PORT` de la API.

La carpeta `uploads/` guarda las fotos de mascotas y productos. Conviene montarla como volumen
persistente y respaldarla junto con la base de datos.

## Seguridad

- `.env` nunca se sube a Git (incluido en `.gitignore`). Solo se versiona `.env.example`.
- La contraseña de la base de datos que compartiste en el chat quedó expuesta: cámbiala en MySQL
  y actualiza `DB_PASSWORD` en el `.env`.
- Helmet activo, límite de 100 KB en JSON, límite de 5 MB en imágenes (solo JPG, PNG y WEBP).
- Validación de entradas con Zod; los errores devuelven los campos con problemas.

## Pruebas

```bash
npm test
```

Cubre reglas del dominio (disponibilidad, solapamiento, transiciones de pedido, totales, próximas
dosis), casos de uso de autenticación, citas y tienda con repositorios simulados, autorización por
roles (401/403) y recordatorios.
