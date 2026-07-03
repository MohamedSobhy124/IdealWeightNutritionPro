# Deploying to SmarterASP.NET

SmarterASP.NET is a **Windows / IIS** host with **SQL Server included** — a good fit for this project (unlike Render, which has no SQL Server).

This guide deploys:

| Site | Technology | Example URL |
|------|------------|-------------|
| **Storefront** | Angular 19 (static SPA) | `https://idealweightnutrition.ae` |
| **API** | .NET 8 FastEndpoints | `https://admin.idealweightnutrition.ae` |

The storefront calls the API on a **subdomain** (CORS + cookies are configured for that).

Official SmarterASP references:

- [ASP.NET Core hosting](https://www.smarterasp.net/asp.net_core_hosting)
- [Angular hosting](https://www.smarterasp.net/angular)
- [Publish ASP.NET Core + Angular](https://www.smarterasp.net/support/kb/a2203/how-to-publish-asp_net-core-with-angular-to-our-server.aspx)

---

## 1. SmarterASP control panel — before you deploy

### A. Create / locate your SQL database

1. Log in to **SmarterASP.NET control panel**.
2. Open **Databases → SQL Server**.
3. Create a database (or use existing `db_ac153b_idealweightdb`).
4. Copy the connection string from the panel. It looks like:

```
Server=SQL####.site4now.net;Database=db_ac153b_idealweightdb;User Id=db_ac153b_idealweightdb;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

> **Important:** Use **SQL authentication** from the panel — not `Integrated Security=True`.

### B. Create two websites (recommended)

| Site name | Purpose | .NET version |
|-----------|---------|--------------|
| Main site | Angular storefront (`www` or root domain) | Static / HTML |
| API site | .NET API (`api.` subdomain) | **ASP.NET Core 8** |

In the control panel for the **API site**:

- Set **ASP.NET Core version** to **8.x**
- Enable **WebSockets** (needed for SignalR notifications)
- Enable **Always On** / disable idle timeout if available (background jobs: stock alerts, payment verification)

### C. Upload product media (legacy wwwroot)

Product images live outside the git repo. Upload your legacy `wwwroot` folder to the server, e.g.:

```
D:\home\site\wwwroot\legacy-media\
  Images\Products\     ← product photos
  images\              ← banners, blogs, flash sales
  videos\              ← home banner video
```

You can FTP the folder from your PC:

```
IdealWeightNutrition\IdealWeightNutrition\wwwroot\
```

---

## 2. Build publish packages on your PC

From PowerShell in the `modernization` folder:

```powershell
.\scripts\publish-smarterasp.ps1
```

Output:

```
modernization\publish\smarterasp\
  api\          ← upload to API site root
  web\          ← upload to storefront site root
```

### Before building the frontend

Edit `frontend/src/environments/environment.prod.ts` if your URLs differ:

```typescript
apiBaseUrl: 'https://admin.idealweightnutrition.ae/api',
signalRHubUrl: 'https://admin.idealweightnutrition.ae/hubs/notifications',
legacyAssetsBaseUrl: 'https://admin.idealweightnutrition.ae',
siteUrl: 'https://idealweightnutrition.ae',
```

For a SmarterASP **temporary URL** (e.g. `http://yoursite-001.site4now.net`), use those URLs instead.

---

## 3. Deploy the API (`iwn-api`)

### Upload files

Upload **everything** inside `publish\smarterasp\api\` to the **API site root** via:

- **FTP** (FileZilla), or
- **Web Deploy** (Visual Studio publish profile from SmarterASP panel)

The folder must contain `IdealWeightNutrition.Api.dll`, `web.config`, and all DLL dependencies.

### Configure production settings on the server

Create this file on the server (FTP), next to `appsettings.json`:

**`appsettings.Production.local.json`** (never commit — contains secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL####.site4now.net;Database=db_ac153b_idealweightdb;User Id=...;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Jwt": {
    "SigningKey": "YOUR-32-CHAR-OR-LONGER-RANDOM-SECRET"
  },
  "LegacyStorage": {
    "WwwRootPath": "D:\\home\\site\\wwwroot\\legacy-media"
  },
  "ProductStorage": {
    "ProductsPath": "D:\\home\\site\\wwwroot\\legacy-media\\Images\\Products"
  },
  "Authentication": {
    "Google": {
      "ClientId": "...",
      "ClientSecret": "..."
    }
  },
  "Smtp": {
    "Password": "..."
  },
  "Geidea": {
    "MerchantPublicKey": "...",
    "MerchantApiPassword": "..."
  },
  "Tamara": {
    "ApiToken": "...",
    "NotificationToken": "..."
  },
  "Tappy": {
    "ApiKey": "...",
    "MerchantId": "..."
  }
}
```

Adjust `LegacyStorage:WwwRootPath` to the **actual absolute path** on your SmarterASP account (check via FTP or control panel file manager).

Alternatively, set these as **Environment Variables** in the SmarterASP control panel using .NET naming:

| Env var | Example |
|---------|---------|
| `ConnectionStrings__DefaultConnection` | SQL connection string |
| `Jwt__SigningKey` | random secret |
| `LegacyStorage__WwwRootPath` | `D:\home\site\wwwroot\legacy-media` |
| `Cors__AllowedOrigins__0` | `https://idealweightnutrition.ae` |
| `App__FrontendBaseUrl` | `https://idealweightnutrition.ae` |
| `App__PublicApiBaseUrl` | `https://admin.idealweightnutrition.ae` |

### Test the API

Open in browser:

```
https://admin.idealweightnutrition.ae/api/health
```

Expected: JSON with `"status": "healthy"`.

Also test an image URL:

```
https://admin.idealweightnutrition.ae/Images/Products/SOME-GUID.png
```

---

## 4. Deploy the storefront (`iwn-web`)

Upload **everything** inside `publish\smarterasp\web\` to the **main site root**.

The folder includes:

- `index.html`, JS/CSS bundles
- `web.config` — IIS rewrite rules for Angular client-side routing

### Test the storefront

1. Open `https://idealweightnutrition.ae`
2. Browse shop, open a product
3. Try login / add to cart

---

## 5. DNS (custom domain)

In your domain DNS panel:

| Record | Points to |
|--------|-----------|
| `@` or `www` | SmarterASP IP for main site |
| `api` | SmarterASP IP for API site |

Bind both domains in the SmarterASP control panel and enable **free SSL**.

---

## 6. Google OAuth (if used)

In [Google Cloud Console](https://console.cloud.google.com/), add:

- **Authorized JavaScript origins:** `https://idealweightnutrition.ae`
- **Authorized redirect URIs:** `https://admin.idealweightnutrition.ae/signin-google`

Update `App__FrontendBaseUrl` and `App__PublicApiBaseUrl` to match your live URLs.

---

## 7. Payment gateways

Set callback / return URLs in Geidea, Tamara, and Tabby dashboards to your **production** URLs:

- Storefront: `https://idealweightnutrition.ae`
- API callbacks: `https://admin.idealweightnutrition.ae/...`

---

## 8. Troubleshooting

| Problem | Fix |
|---------|-----|
| API 500 on startup | Check `logs\stdout` in API site folder; verify SQL connection string |
| CORS errors in browser | Set `Cors__AllowedOrigins__0` to exact storefront URL (with `https://`) |
| Product images 404 | Check `LegacyStorage__WwwRootPath`; ensure `Images/Products` uploaded |
| Angular routes 404 | Ensure `web.config` is in storefront root |
| SignalR not connecting | Enable WebSockets on API site; check `signalRHubUrl` in environment.prod.ts |
| Upload fails | `web.config` allows 50 MB (`maxAllowedContentLength`) |

---

## 9. Visual Studio publish (optional)

1. Open `backend/IdealWeightNutrition.Modernized.sln` in Visual Studio.
2. Right-click **IdealWeightNutrition.Api → Publish**.
3. Import the publish profile downloaded from SmarterASP control panel (or edit `Properties/PublishProfiles/SmarterASP.pubxml`).
4. Set **Environment** to `Production`.
5. Publish.

---

## 10. Local test before upload

```powershell
cd modernization
docker compose up --build
# storefront: http://localhost:4000
# API health: http://localhost:8080/api/health
```

Or run API + frontend separately (see `README.md`).
