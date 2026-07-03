# Frontend — Angular SPA

Feature-based standalone Angular app aligned with [blueprint §30](../../docs/System-Analysis-and-Migration-Blueprint.md).

## Structure

```text
src/app/
├── core/           # guards, interceptors, auth, api client
├── shared/         # reusable UI (add Material/AG Grid per §19)
└── features/
    ├── home/
    ├── catalogue/
    ├── cart/
    ├── checkout/
    ├── auth/
    ├── account/
    ├── admin/
    ├── blog/
    ├── services/
    ├── promotions/
    └── orders/
```

## Commands

```bash
npm install
npm start          # http://localhost:4200 (proxies /api → backend)
npm run build
```

## API integration

- Dev proxy: `proxy.conf.json` → `https://localhost:7001`
- Typed models: generate from OpenAPI (`IdealWeightNutrition.Contracts` / Swagger) when endpoints are added

## Next steps

1. Add Angular Material theme + RTL (`ar` default)
2. Implement auth forms wired to `POST /api/auth/*`
3. Build catalogue pages against product endpoints
4. Expand admin feature routes per §30.4
