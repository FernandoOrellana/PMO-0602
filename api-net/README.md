# API PetCare HN – versión ASP.NET Core

API REST en **ASP.NET Core 10** con Arquitectura Limpia, Entity Framework Core (MySQL), JWT, FluentValidation,
Swagger (Swashbuckle) y pruebas con xUnit + Moq. Mantiene el mismo contrato que la versión Node (`../api`),
así que la app Android funciona sin cambios.

## Estructura

```
src/
├── PetCare.Domain          Entidades, enums y reglas de negocio puras
├── PetCare.Application     Casos de uso (servicios), contratos (DTOs), validaciones FluentValidation e interfaces
├── PetCare.Infrastructure  EF Core (MySQL), repositorios, BCrypt, JWT, correo SMTP, FCM, archivos y job de recordatorios
└── PetCare.Api             Controladores, autenticación JWT, Swagger y manejo global de errores
tests/
└── PetCare.Tests           xUnit + Moq (reglas, casos de uso, autorización)
```

## Requisitos

- .NET 10 SDK (desarrollo) o .NET 10 Runtime (servidor)
- MySQL 8 con la base `MPF` (el mismo esquema que usa la versión Node)

## Configuración

La configuración base está en `src/PetCare.Api/appsettings.json`. Los secretos van en
`src/PetCare.Api/appsettings.Production.json`, que **no se versiona** (está en `.gitignore`):

```json
{
  "ConnectionStrings": {
    "MySql": "Server=192.168.100.20;Port=510;Database=MPF;User ID=AdminBD;Password=TU_CONTRASENA;"
  },
  "Jwt": { "Secret": "UN_SECRETO_LARGO_ALEATORIO", "AccessMinutes": 30 },
  "App": {
    "PublicBaseUrl": "https://apiveterinaria.orellanawebserver.space",
    "CodigoExpiraMinutos": 15,
    "RefreshTokenDias": 7,
    "RecordatorioDias": 7,
    "ZonaHoraria": "America/Tegucigalpa"
  },
  "Smtp": { "Host": "smtp.gmail.com", "Port": 587, "Secure": false,
            "User": "correo@gmail.com", "Password": "CONTRASENA_DE_APLICACION",
            "From": "PetCare HN <correo@gmail.com>" },
  "Almacenamiento": { "Carpeta": "uploads" },
  "Recordatorios": { "Habilitado": true, "Hora": "08:00" },
  "Seed": { "AdminEmail": "admin@huellitas.hn", "AdminPassword": "", "DemoPassword": "Demo12345" }
}
```

## Comandos

```bash
dotnet restore
dotnet build
dotnet test                                   # pruebas unitarias y de autorización
dotnet publish src/PetCare.Api -c Release -o /opt/petcare-net

# Esquema y datos (en la carpeta publicada)
dotnet PetCare.Api.dll migrate                # idempotente: no duplica tablas
dotnet PetCare.Api.dll seed                   # solo si la base está vacía

# Ejecutar
ASPNETCORE_ENVIRONMENT=Production dotnet PetCare.Api.dll --urls http://127.0.0.1:3002
```

Swagger: `/api-docs` · Especificación: `/api-docs/v1/swagger.json` · Salud: `/health`

## Equivalencias con la versión Node

| Node (`../api`)            | ASP.NET (`api-net`)                     |
|----------------------------|-----------------------------------------|
| Express                    | ASP.NET Core Web API (controladores)    |
| mysql2 (SQL directo)       | EF Core 10 + MySql.EntityFrameworkCore  |
| jsonwebtoken               | JwtBearer (access token)                |
| bcryptjs                   | BCrypt.Net-Next (mismo formato de hash) |
| Zod                        | FluentValidation                        |
| Jest                       | xUnit + Moq                             |
| swagger-ui-express         | Swashbuckle                             |
| multer                     | IFormFile                               |
| node-cron                  | BackgroundService                       |
| nodemailer                 | MailKit                                 |
| firebase-admin             | FirebaseAdmin                           |

Los hashes de contraseña y de tokens son compatibles con los de la versión Node, así que los usuarios
existentes siguen pudiendo iniciar sesión sin cambios.
