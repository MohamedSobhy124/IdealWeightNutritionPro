# Local development — frontend, API, and SSMS

## 1. Database (SQL Server / SSMS)

Your machine already has the legacy database locally. Connect in **SSMS** with:

| Setting | Value |
|--------|--------|
| Server type | Database Engine |
| Server name | `.` or `(local)` or `localhost` |
| Authentication | Windows Authentication |
| Database | `db_ac153b_idealweightdb` |

### Apply / update schema (migrations)

From the repo root in PowerShell:

```powershell
.\modernization\scripts\apply-database.ps1
```

Optional SQL login (e.g. Docker):

```powershell
.\modernization\scripts\apply-database.ps1 -Server "localhost,1433" -SqlUser "sa" -SqlPassword "Your_strong_Password123"
```

### API connection string

`modernization/backend/src/IdealWeightNutrition.Api/appsettings.Development.json` is set to:

```
Server=.;Database=db_ac153b_idealweightdb;Integrated Security=True;TrustServerCertificate=True
```

Redis is disabled in Development (`Redis:ConnectionString` is empty) so the API uses in-memory cache and does not require `localhost:6379`.

Override with user secrets if needed:

```bash
cd modernization/backend/src/IdealWeightNutrition.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=db_ac153b_idealweightdb;Integrated Security=True;TrustServerCertificate=True"
```

---

## 2. Backend API

```bash
cd modernization/backend
dotnet run --project src/IdealWeightNutrition.Api --launch-profile https
```

- Swagger: https://localhost:7128/swagger  
- Health: https://localhost:7128/api/health  
- HTTP (no TLS): http://localhost:5228 — use launch profile `http` or `npm run start:http-api` on the frontend

Trust the dev HTTPS certificate once:

```bash
dotnet dev-certs https --trust
```

---

## 3. Frontend (proxied to API)

```bash
cd modernization/frontend
npm install
npm start
```

- App: http://localhost:4200  
- API calls go to `/api/*` → proxied to `https://localhost:7128` (`proxy.conf.json`)

If HTTPS proxy fails, run the API with `--launch-profile http` and use:

```bash
npm run start:http-api
```

---

## 4. Verify end-to-end

1. Open http://localhost:4200/shop — products load from local DB  
2. Swagger https://localhost:7128/swagger — `GET /api/health` returns OK  
3. Log in with an existing user from `AspNetUsers` in SSMS  

---

## Docker (optional)

If you use Docker instead of local SSMS:

```bash
cd modernization
docker compose up -d
```

For **Redis only** (guest cart + catalogue cache across API restarts):

```bash
cd modernization
docker compose up -d redis
```

Then set `Redis:ConnectionString` to `localhost:6379` in `appsettings.Development.json` or user secrets.

Then set the connection string to:

```
Server=localhost,1433;Database=IdealWeightNutrition;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True
```

and run `apply-database.ps1` with `-Server` / `-SqlUser` / `-SqlPassword` as above.
