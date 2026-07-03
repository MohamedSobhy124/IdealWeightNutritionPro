# Deploying to Render

This project deploys as **two Docker web services**:

| Service   | Folder      | Stack                     | Public? | Port source |
| --------- | ----------- | ------------------------- | ------- | ----------- |
| `iwn-api` | `backend/`  | .NET 8 (FastEndpoints)    | yes     | `$PORT`     |
| `iwn-web` | `frontend/` | Angular 19 SSR (Express)  | yes     | `$PORT`     |

The storefront (`iwn-web`) is the browser-facing origin. Its SSR/Express server
**proxies** `/api`, `/hubs`, `/videos`, `/images`, `/sitemap.xml`, `/robots.txt`,
and `/signin-google` to `iwn-api` using the `BACKEND_URL` environment variable.

> The repository root pushed to GitHub is the `modernization/` folder, so all
> paths below are relative to that root.

## Option A — Blueprint (recommended)

1. Push this repo to GitHub (already done: `MohamedSobhy124/IdealWeightNutritionPro`).
2. Render Dashboard → **New +** → **Blueprint** → select the repo.
3. Render reads [`render.yaml`](../render.yaml) and creates both services.
4. Fill every `sync: false` env var (see below), then **Apply**.

## Option B — Create each service manually

### API service (`iwn-api`)

| Field           | Value                    |
| --------------- | ------------------------ |
| Language        | **Docker**               |
| Root Directory  | `backend`                |
| Dockerfile Path | `./Dockerfile`           |
| Health Check    | `/api/health`            |

### Web service (`iwn-web`)

| Field           | Value          |
| --------------- | -------------- |
| Language        | **Docker**     |
| Root Directory  | `frontend`     |
| Dockerfile Path | `./Dockerfile` |

> **About the "Dockerfile Path" field:** it is relative to the Root Directory.
> With Root Directory `backend`, the path is simply `./Dockerfile`. If you leave
> Root Directory empty, use `./backend/Dockerfile` and `./frontend/Dockerfile`.

## Environment variables

### `iwn-api`

| Key                                     | Required | Notes                                                                 |
| --------------------------------------- | -------- | --------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`                | yes      | `Production`                                                          |
| `ConnectionStrings__DefaultConnection`  | yes      | SQL Server, **SQL auth** (not Integrated Security). See DB note below |
| `Jwt__SigningKey`                       | yes      | 32+ char secret (Render can generate)                                 |
| `Cors__AllowedOrigins__0`               | rec.     | Public storefront URL, e.g. `https://iwn-web.onrender.com`            |
| `App__FrontendBaseUrl`                  | rec.     | Same storefront URL                                                   |
| `App__PublicApiBaseUrl`                 | rec.     | Public API URL, e.g. `https://iwn-api.onrender.com`                   |
| `SiteSettings__BaseUrl`                 | rec.     | Storefront URL (used for SEO links/emails)                            |
| `Redis__ConnectionString`               | no       | Leave empty to disable                                                |
| `Authentication__Google__ClientId/Secret` | no    | Google sign-in                                                        |
| `Smtp__Host/Username/Password`          | no       | Email; if unset, emails are logged only                               |
| `Geidea__*`, `Tamara__*`, `Tappy__*`    | no       | Payment gateways (only what you use)                                  |

> Config keys use the .NET double-underscore convention: `Section__Key` maps to
> `Section:Key` in `appsettings.json`. Array items use `Section__Key__0`.

### `iwn-web`

| Key           | Required | Notes                                                       |
| ------------- | -------- | ----------------------------------------------------------- |
| `BACKEND_URL` | yes      | Full URL **with scheme** of the API, e.g. `https://iwn-api.onrender.com` |

## Known gaps to resolve before go-live

1. **Database (SQL Server).** Render has no managed SQL Server — only PostgreSQL.
   Point `ConnectionStrings__DefaultConnection` at an external, reachable SQL
   Server (Azure SQL Database, a managed MSSQL host, or your existing production
   DB). The app is **database-first** against the existing schema
   (`db_ac153b_idealweightdb`); it does not run EF migrations. On startup it will
   create the `RefreshTokens` table and seed identity roles if it can connect.
   The local `Integrated Security=True` string will **not** work on Linux — use
   SQL authentication.

2. **Product images.** In development the API serves `/images` and `/videos` from
   the legacy MVC `wwwroot`, which is not part of this repo/container. In
   production those assets need a real host (object storage / CDN) or the
   images must be copied into the API image and served from there.

3. **Secrets.** Real credentials live only in the git-ignored
   `backend/src/IdealWeightNutrition.Api/appsettings.Development.local.json`
   locally. In Render, provide them as environment variables (above). Rotate any
   keys that were previously committed.

## Local containerized run

```bash
cd modernization
docker compose up --build
# storefront: http://localhost:4000   API: http://localhost:8080/api/health
```
