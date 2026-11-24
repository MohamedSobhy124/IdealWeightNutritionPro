# IdealWeightNutrition – Features & Implementation Guide

E-commerce platform for nutrition products built with **ASP.NET Core 7 (MVC + Razor Pages for Identity)**, **EF Core 7 / SQL Server**, and **ASP.NET Core Identity**.

---

## 1. Solution Architecture

| Project | Responsibility |
|---|---|
| `IdealWeightNutrition` | Web app: Areas (Admin / Customer / Identity), Controllers, Views, Services, SignalR Hubs, ViewComponents, Model Binders |
| `IdealWeightNutrition.Models` | Domain entities and ViewModels (`Product`, `OrderHeader`, `ShoppingCart`, `PromoCode`, …) |
| `IdealWeightNutrition.DataAccess` | `ApplicationDBContext`, Migrations, Repository + Unit of Work, `DbInitializer` |
| `IdealWeightNutrition.Utility` | Constants (`SD`), Email sender, payment gateway helpers, OTP, guest cart helper |

**Patterns used**
- **Repository + Unit of Work** – `Repository/Repository.cs`, `Repository/UnitOfWork.cs`, registered as `IUnitOfWork` (scoped).
- **Areas** – `Admin`, `Customer`, `Identity` separate back-office, storefront and authentication.
- **Options pattern** – payment / WhatsApp settings bound from `appsettings.json` (`Geidea`, `Tappy`, `Tamara`, `WhatsApp` sections).
- **Hosted services** for background work.

---

## 2. Application Startup (`Program.cs`)

- **Logging**: custom file logger writing to `/logs`.
- **Upload limits**: 50 MB (`FormOptions`, `IISServerOptions`) for video banner uploads.
- **MVC** with cache profiles `DefaultCache` (5 min) and `LongCache` (1 h), plus `EncryptedIdModelBinderProvider`.
- **Response caching** and in-memory caching.
- **Localization**: Arabic (default) and English; culture resolved from `?culture=` query string then cookie.
- **EF Core / SQL Server**: retry on failure, batch size 100, 60s timeout, `NoTracking` by default.
- **Identity** with `ApplicationUser` + `IdentityRole`, custom login/logout/access-denied paths.
- **Google external login**.
- **HSTS** (1 year, preload, subdomains) and HTTPS redirect in production.
- **Data Protection** keys persisted to file system (90-day lifetime) to avoid correlation failures after restarts.
- **Session** (100 min idle) used for cart count and guest cart.
- **SignalR** hub mapped at `/notificationHub`.
- **Static files** with long-lived immutable caching for CSS/JS/images/fonts, special handling for `.well-known` (Apple Pay domain verification) and the video banner poster (no-cache).
- **Database initializer** runs on startup (roles, admin user, seed data).
- **Routes**: SEO-friendly product slug route `Customer/Home/Details/{slug}`, Tamara webhook route, default `{area=Customer}/{controller=Home}/{action=Index}/{id?}`.

---

## 3. Authentication, Authorization & Users

**Roles** (`SD.cs`): `Customer`, `Company`, `Admin`, `Employee`.

| Feature | Implementation |
|---|---|
| Register / Login / Logout | Scaffolded Identity Razor Pages in `Areas/Identity/Pages/Account` (`Register.cshtml.cs`, `Login.cshtml.cs`) |
| Google login | `AddGoogle(...)` in `Program.cs`, handled by `ExternalLogin.cshtml.cs` |
| Two-factor auth | `Account/Manage/TwoFactorAuthentication.cshtml.cs` |
| OTP verification | `Utility/OtpHelper.cs` + `Customer/Controllers/AccountController.cs` (OTP generation/validation, delivered via email/WhatsApp) |
| Role seeding & admin user | `DataAccess/DbInitializer/DbInitializer.cs` – applies pending migrations, creates roles and default admin |
| User management | `Admin/Controllers/UserController.cs` (list users, change role, lock/unlock) |
| Company (B2B) accounts | `Admin/Controllers/CompaniesController.cs`, `Company` entity linked to `ApplicationUser.CompanyId` |
| Encrypted IDs in URLs | `ModelBinders/EncryptedIdModelBinder.cs` decrypts IDs bound from route/query |

---

## 4. Catalog

| Feature | Implementation |
|---|---|
| Categories | `Admin/Controllers/CategriesController.cs`, entity `Categry` (soft delete via `IsDeleted`) |
| Brands | `Admin/Controllers/BrandController.cs`, entity `Brand` |
| Products CRUD | `Admin/Controllers/ProductController.cs`, entity `Product` (+ `ProductImage` gallery) |
| Product variants & options | `ProductOption`, `ProductOptionValue`, `ProductVariant`, `ProductVariantOptionValue` (e.g. flavor/size with own stock & price) |
| Excel import/export | EPPlus package used in admin product management |
| Storefront listing / details | `Customer/Controllers/HomeController.cs`, `ShopController.cs`; product details resolved by slug |
| Soft delete & indexes | `ApplicationDBContext.OnModelCreating` – indexes on `IsDeleted`, `CategryId`, `StockQuantity` |
| Wishlist | `Wishlist` entity, customer actions in `HomeController` |
| Reviews & ratings | `Customer/Controllers/ReviewController.cs` (submit), `Admin/Controllers/ReviewController.cs` (moderate) |

---

## 5. Inventory

| Feature | Implementation |
|---|---|
| Stock management | `Services/StockService.cs` (`IStockService`) – checks availability, reserves/decrements stock on order, restores on cancel/return, supports variants |
| Back-in-stock alerts | `StockNotification` entity; customers subscribe, notified when stock returns |
| Expiring products | `Services/ExpiringProductsBackgroundService.cs` – periodic hosted service scanning products near expiry and notifying admins |

---

## 6. Shopping Cart & Checkout

| Feature | Implementation |
|---|---|
| Logged-in cart | `ShoppingCart` entity, `Customer/Controllers/CartController.cs` |
| Guest cart | `Utility/GuestCartHelper.cs` – cart stored in session (`SD.SessionCart`), merged on login |
| Cart badge | `ViewComponents/ShoppingCartViewComponent.cs` rendered in `_Layout.cshtml` |
| Shipping by city / remote areas | `City` and `RemoteArea` entities, managed via `Admin/CityController.cs` and `Admin/RemoteAreaController.cs`; shipping fee calculated in checkout |
| Checkout summary | `CartController` builds `OrderHeader` + `OrderDetail` from cart items |
| Invoices | `InvoiceService` (scoped) generates order invoices |

---

## 7. Payments

Payment methods (`SD.cs`): **Geidea**, **Tabby** (`TappyHelper`), **Tamara**.

| Gateway | Implementation |
|---|---|
| Geidea (cards / Apple Pay) | `Utility/GeideaHelper.cs`, settings section `Geidea`; Apple Pay domain file served from `wwwroot/.well-known` |
| Tabby (BNPL) | `Utility/TappyHelper.cs`, settings section `Tappy` |
| Tamara (BNPL) | `Utility/TamaraHelper.cs` + `Tamara.SDK`, settings section `Tamara`; webhook at `customer/cart/TamaraWebhook` |
| Callbacks / confirmation | Success/failure/cancel actions in `CartController` update `PaymentStatus` and `OrderStatus` |
| Payment reconciliation | `Services/PaymentVerificationBackgroundService.cs` – periodically re-checks pending payments with gateways and updates orders |

**Payment statuses**: Pending, Paid, ApprovedForDelayedPayment, Authorized, Rejected, Cancelled, Refunded, PartiallyRefunded.

---

## 8. Orders

| Feature | Implementation |
|---|---|
| Order entities | `OrderHeader`, `OrderDetail` |
| Admin order management | `Admin/Controllers/OrderController.cs` – list/filter, details, status changes, cancel, refund |
| Order lifecycle | Pending → Approved/Paid → Processing → Preparing Shipping → Shipped → OutForDelivery → Delivered (or Cancelled/Returned/Refunded) |
| Audit trail | `OrderAuditLog` entity records every status/payment change |
| Customer order history | Customer area order views |

---

## 9. Returns & Refunds

| Feature | Implementation |
|---|---|
| Return requests | `ReturnRequest`, `ReturnRequestItem` entities; customer submits per order item |
| Admin processing | `Admin/Controllers/ReturnRequestController.cs` – approve / reject / process / complete |
| Statuses | Return: Pending, Approved, Rejected, Processing, Completed, Cancelled. Refund: Pending, Processed, Failed |
| Stock restore | Via `StockService` on completed returns |

---

## 10. Marketing & Promotions

| Feature | Implementation |
|---|---|
| Promo codes | `PromoCode`, `PromoCodeUsage`; exclusions per product/combo/subscription (`PromoCodeExcluded*`); admin in `PromoCodeController.cs` |
| Flash sales | `FlashSale`, `FlashSaleItem`; admin `Admin/FlashSaleController.cs`, storefront `Customer/FlashSaleController.cs` (time-bound discounted prices) |
| Combo offers (bundles) | `ComboOffer`, `ComboOfferItem`, `ComboOfferImage`; admin + customer `ComboOfferController.cs` |
| Newsletter | `NewsletterSubscription`; admin `NewsletterSubscriptionController.cs` |
| Video banner | `Admin/Controllers/VideoBannerController.cs` – upload homepage video + poster (up to 50 MB) |
| Blog | `BlogPost`; admin `Admin/BlogController.cs`, public `Customer/BlogController.cs` |

---

## 11. Services & Subscriptions (Nutrition Plans)

| Feature | Implementation |
|---|---|
| Service offers | `ServiceOffer`, `ServiceImage` |
| Subscriptions | `ServiceSubscription`, `ServicePurchase`; admin `Admin/ServiceSubscriptionController.cs`, customer `Customer/ServiceSubscriptionController.cs` |
| Payment | Purchased through the same gateway flow as products |

---

## 12. Notifications

| Feature | Implementation |
|---|---|
| Real-time admin notifications | `Hubs/NotificationHub.cs` (SignalR, `/notificationHub`) |
| Notification service | `Services/NotificationService.cs` (`INotificationService`) – persists `Notification` and pushes via SignalR |
| Notification API | `Admin/Controllers/NotificationApiController.cs` – list, mark as read |
| Email | `Utility/EmailSender.cs` (`IEmailSender`) – SMTP for confirmations, order updates, OTP |
| WhatsApp | `WhatsAppSettings` (section `WhatsApp`) for OTP/order messages |

---

## 13. Localization

- Cultures: `ar` (default, RTL) and `en`.
- Resources in `SharedResources.resx` injected via `SharedResources` singleton / `IViewLocalizer`.
- Language switch: `Controllers/LanguageController.cs` sets the culture cookie.

---

## 14. SEO

- Slug-based product URLs.
- `Controllers/SitemapController.cs` – dynamic `sitemap.xml`.
- `Controllers/RobotsController.cs` – dynamic `robots.txt`.

---

## 15. Performance & Security

- Precompiled Razor views (`RazorCompileOnBuild/Publish`).
- Response caching, memory cache, static file long-term caching with ETags.
- `NoTracking` queries by default, DB indexes, retry-on-failure.
- HSTS, HTTPS redirection, `SameSite=Lax` cookies, HttpOnly session cookies.
- Persisted Data Protection keys.
- Encrypted IDs in URLs.
- Centralized error page `/Customer/Home/Error` with status-code re-execution.

---

## 16. Configuration (`appsettings.json` keys)

| Section | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `Authentication:Google:ClientId/ClientSecret` | Google OAuth |
| `Geidea` | Geidea gateway settings |
| `Tappy` | Tabby gateway settings |
| `Tamara` | Tamara gateway settings |
| `WhatsApp` | WhatsApp messaging settings |
| Email / SMTP settings | Used by `EmailSender` |

> Keep secrets out of source control (use User Secrets / environment variables / Azure Key Vault).

---

## 17. Main NuGet Packages

- `Microsoft.EntityFrameworkCore.SqlServer` / `Tools` 7.0.8
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` / `Identity.UI` 7.0.7
- `Microsoft.AspNetCore.Authentication.Google` 7.0.0
- `EPPlus` 6.2.10 (Excel)
- `Tamara.SDK` 1.0.3

---

## 18. Running Locally

1. Set `ConnectionStrings:DefaultConnection` in `appsettings.json` or User Secrets.
2. `dotnet ef database update --project IdealWeightNutrition.DataAccess --startup-project IdealWeightNutrition` (the `DbInitializer` also applies pending migrations at startup).
3. Run the `IdealWeightNutrition` project. Roles and the default admin user are seeded on first run.
