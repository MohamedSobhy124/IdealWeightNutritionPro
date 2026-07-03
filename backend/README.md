# Backend — Clean Architecture

```text
src/
├── IdealWeightNutrition.Api/           # FastEndpoints, auth, middleware
│   └── Features/                       # Vertical API slices (Auth, Cart, Orders, …)
├── IdealWeightNutrition.Application/   # CQRS handlers, validators, abstractions
│   └── Features/
├── IdealWeightNutrition.Domain/        # Entities, value objects, domain rules
├── IdealWeightNutrition.Infrastructure/# EF Core, Redis, payment adapters, email
└── IdealWeightNutrition.Contracts/     # Public DTOs shared with Angular (OpenAPI source)
tests/
└── IdealWeightNutrition.Application.Tests/
```

See [blueprint §10](../../docs/System-Analysis-and-Migration-Blueprint.md) and [§29 per-endpoint plan](../../docs/System-Analysis-and-Migration-Blueprint.md).

## User secrets (development)

```bash
cd src/IdealWeightNutrition.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<your-dev-signing-key>"
```

## Auth module (Phase 3)

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/login` | Email/password → JWT + refresh token |
| POST | `/api/auth/register` | New customer account (role forced to Customer) |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/logout` | Revoke refresh token |
| GET | `/api/auth/me` | Current user profile (Bearer token) |

On first dev run, the API ensures a `RefreshTokens` table exists (additive; safe alongside legacy DB) and seeds Identity roles.

Point `ConnectionStrings:DefaultConnection` at your legacy database to sign in with existing users, or use a local SQL Server from `docker compose up`.

**Build fails with MSB3027 / file locked?** Stop the running API first (`IdealWeightNutrition.Api` in Task Manager, or close the debug session in Visual Studio), then rebuild.

## Catalogue module

| Method | Route | Description |
|---|---|---|
| GET | `/api/products` | Paginated list (`search`, `categoryId`, `brandId`, `availability`, `sortBy`, `page`, `pageSize`) |
| GET | `/api/products/{slug}` | Product detail by slug (numeric id fallback) |
| GET | `/api/categories` | Active categories |
| GET | `/api/brands` | Active brands |

Reads from legacy `Products`, `Categries`, `Brands`, `ProductImages`, `ProductVariants` tables via EF Core (no schema changes).

## Cart module

| Method | Route | Description |
|---|---|---|
| GET | `/api/cart` | Current cart with priced line items |
| POST | `/api/cart/items` | Add/update quantity (`productId`, `quantity`, optional `productVariantId`) |
| PUT | `/api/cart/items/{lineId}` | Set quantity (0 removes) |
| DELETE | `/api/cart/items/{lineId}` | Remove line |
| DELETE | `/api/cart` | Clear cart |

- **Authenticated users:** persisted in legacy `ShoppingCarts` table.
- **Guests:** Redis or in-memory distributed cache + `iwn_cart_id` HttpOnly cookie (14-day sliding).

## Checkout module

| Method | Route | Description |
|---|---|---|
| GET | `/api/checkout/cities` | Active UAE delivery cities |
| GET | `/api/checkout/cities/{cityId}/remote-areas` | Remote areas for a city |
| POST | `/api/checkout/shipping-quote` | Subtotal + shipping + total (`cityId`, optional `remoteAreaId`) |
| POST | `/api/checkout/otp` | Send guest email verification code |
| POST | `/api/checkout/verify-otp` | Verify guest email code |
| POST | `/api/checkout` | Create order from cart (v1: **COD** only) |

- **Orders** persist to legacy `orderHeaders` / `orderDetails`; cities from `Cities` / `RemoteAreas`.
- **Guest checkout** requires OTP (stored in distributed cache; code emailed when SMTP is configured, otherwise logged to console in Development).
- **Authenticated users** skip OTP; cart is cleared after a successful order.
- **Free delivery** follows legacy rules when all cart products allow it and subtotal meets the minimum.

## Orders module

| Method | Route | Description |
|---|---|---|
| GET | `/api/orders` | Current user's order history (JWT) |
| GET | `/api/orders/{orderId}` | Order details (Bearer owner, or `?email=` for guest orders) |
| POST | `/api/orders/track` | Lookup by `orderId` + `email` |

## Auth + cart

Login and register automatically merge the guest cart (`iwn_cart_id` cookie) into the authenticated user's `ShoppingCarts` rows and clear the guest cookie.
