# Ideal Weight Nutrition — Modernized Platform

Parallel implementation of the target architecture defined in [System Analysis & Migration Blueprint](../docs/System-Analysis-and-Migration-Blueprint.md).

## Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 19+ (standalone, signals, lazy routes) |
| API | .NET 8 FastEndpoints (documented target: .NET 9) |
| Application | CQRS, FluentValidation, Mapster |
| Data | EF Core + SQL Server |
| Cache | Redis |
| Auth | JWT access + refresh tokens |

## Repository layout

```text
modernization/
├── backend/          # Clean Architecture API solution
├── frontend/         # Angular SPA (storefront + admin)
├── docker/           # Dockerfiles
├── docs/             # Blueprint reference + screen/wireframe placeholders
├── docker-compose.yml
└── README.md
```

## Prerequisites

- .NET 8 SDK
- Node.js 20+ and npm
- Angular CLI (`npm install -g @angular/cli`)
- Docker (optional, for Redis/SQL Server local dev)

## Quick start (local SSMS + Angular)

### 1. Database

Connect SSMS to **`.`** → database **`db_ac153b_idealweightdb`** (Windows Authentication).  
Apply migrations if needed:

```powershell
.\modernization\scripts\apply-database.ps1
```

See [docs/LOCAL-DEV.md](docs/LOCAL-DEV.md) for full SSMS and connection-string details.

### 2. Backend API

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/IdealWeightNutrition.Api --launch-profile https
```

- API: `https://localhost:7128` (HTTP fallback: `http://localhost:5228`)
- Swagger opens automatically at `/swagger` when using the `https` launch profile
- Health: `GET /api/health`

**Cursor / VS Code:** open the `modernization/backend` folder and run **IdealWeight API (Swagger)** from the Run panel.

`appsettings.Development.json` points at your local **`db_ac153b_idealweightdb`** via Windows Authentication and uses in-memory cache (no Redis required for local dev).

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

- App: `http://localhost:4200`
- `/api` requests are proxied to `https://localhost:7128` (`proxy.conf.json`)
- If HTTPS proxy fails: run API with `--launch-profile http`, then `npm run start:http-api`

### Docker (optional Redis / SQL Server)

```bash
docker compose up -d
```

## Migration approach

Strangler pattern: new API and Angular SPA run alongside the legacy ASP.NET Core MVC app. Feature modules migrate per the blueprint phases (Auth → Catalogue → Cart/Checkout → Admin).

## Documentation

- **Authoritative blueprint:** [docs/System-Analysis-and-Migration-Blueprint.md](../docs/System-Analysis-and-Migration-Blueprint.md)
- **Screen placeholders:** `docs/screens/`
- **Wireframe placeholders:** `docs/wireframes/`

## First modules (recommended build order)

1. Auth (JWT + refresh, guards)
2. Health + shared contracts
3. Catalogue (products, categories, brands)
4. Cart + Checkout + Payments
5. Orders + Returns
6. Admin operations
