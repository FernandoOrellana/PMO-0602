# PetCare HN – Veterinaria Huellitas

Proyecto de Programación Móvil I (PMO-0602).

- `api/`: API REST en Node.js + Express + MySQL, con Arquitectura Limpia y JWT.
- `mobile/`: aplicación Android en Kotlin, Jetpack Compose y MVVM.

## API

Instrucciones completas en [`api/README.md`](api/README.md).

```bash
cd api
npm install
cp .env.example .env   # completa los valores
npm run migrate
npm run seed
npm start
```

Swagger en `/api-docs`. Colección de Postman en `api/docs/`.

## Móvil

Abre la carpeta `mobile/` con Android Studio. La URL de la API está en `mobile/app/build.gradle.kts` (`API_BASE_URL`).
Para notificaciones push, coloca `google-services.json` en `mobile/app/` (no se versiona).
