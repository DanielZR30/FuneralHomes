# Funeral Homes · Plataforma de Microservicios

Monorepositorio con los microservicios de la plataforma **Funeral Homes**, desarrollados en **.NET 10**.
La organización sigue el repositorio de referencia de la clase ([Inmoby](https://github.com/DominguezDocente/Inmoby/tree/master/Microservices)):
**cada microservicio vive en su propia carpeta, con su propia solución (`.slnx`) y su propia base de datos**.

## Microservicios

| Microservicio | Carpeta | Id | Puerto (http/https) | Base de datos | Responsabilidad | Estado |
|---|---|---|---|---|---|---|
| **Customers** | [`Customers/`](Customers/README.md) | `fh.api.customer` | 5112 / 7035 | `fh_db_customer` | Titulares, suscripciones y beneficiarios | ✅ Entrega 1 |
| **Identity** | `Identity/` | `fh.api.identity` | 5113 / 7036 | `fh_db_identity` | Autenticación de usuarios, roles y emisión de JWT | 🚧 Entrega 2 |
| **Financials** | `Financials/` | `fh.api.financial` | 5114 / 7037 | `fh_db_financial` | Catálogo de planes y cálculo de la cuota periódica | 🚧 Entrega 2 |

## Autenticación y autorización (Entrega 2)

* **Identity** autentica con credenciales (ASP.NET Core Identity + EF Core) y emite un **JWT** firmado (HMAC-SHA256) con los claims del usuario y sus roles.
* **Customers** y **Financials** protegen sus endpoints validando el JWT (firma, emisor, audiencia y vigencia) y autorizan **por roles** (`Admin`, `Operator`, `Auditor`).
* Sin token → `401 Unauthorized`; token válido sin el rol requerido → `403 Forbidden`.

El plan de trabajo por integrante, los contratos compartidos y el guión del video los maneja el equipo por separado.

## Estructura del repositorio

```text
FuneralHomes/
├── Customers/        # Microservicio de titulares, suscripciones y beneficiarios (Clean Architecture)
│   ├── src/          # Domain, Application, Persistence, Infrastructure, Api
│   ├── tests/        # Pruebas unitarias (xUnit + Moq)
│   ├── README.md     # Documentación del microservicio
│   └── Customers.slnx
├── Identity/         # Microservicio de autenticación y emisión de JWT (entrega 2)
├── Financials/       # Microservicio de planes y cuotas (entrega 2)
├── docs/             # Documentación compartida: ENDPOINTS, módulos y entrega2 (plan del equipo)
└── scripts/          # Scripts compartidos: reset-db, smoke-test
```

## Ejecución local (prerrequisitos comunes)

* **.NET 10 SDK**
* **Docker** (PostgreSQL) y la herramienta `dotnet ef` (`dotnet tool install --global dotnet-ef`)

```powershell
# 1. PostgreSQL (una sola instancia para los tres microservicios)
docker run --name fh-pg -e POSTGRES_PASSWORD=secret -p 5432:5432 -d postgres:16

# 2. Base de datos del microservicio Customers
.\scripts\reset-db.ps1

# 3. Ejecutar la API de Customers (Swagger en https://localhost:7035/swagger)
dotnet run --project Customers/src/FH.Customers.Api --launch-profile https
```

Cada microservicio tiene su propio `README.md` con instrucciones detalladas.
