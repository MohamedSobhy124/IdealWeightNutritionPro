# Plan: Payments, Combo Delivery & SEO (gap-based)

This plan compares the migration analysis (Parts 1–3) with what the modernized repo (.NET 9 API + Angular 19) already does.
It lists only the work that is still missing or needs fixing.
The audit was done on branch `feature/ui-modernization-home-redesign`, including uncommitted changes, on 2026-11-24.

Status key: ✅ done · 🟡 partial · ❌ missing

---

## 0. Already in place (no work needed)

- The server recalculates totals from the server-side cart. Geidea and Tamara amounts are checked against the order total (`PaymentService.cs:568-605`).
- `PaymentVerificationBackgroundService` runs with configurable options (`appsettings.json` → `PaymentVerification`).
- One shared `MarkPaidAndFulfillAsync` handles the return, callback, webhook and background job paths, with an "already paid" guard.
- Online orders record promo usage only when payment is confirmed. Online orders deduct stock only when payment is confirmed.
- Admin actions exist: ship (with Tamara capture), gateway cancel, re-check payment, partial refund. Most of this is uncommitted work in `AdminOrderService.cs`.
- Company orders use delayed payment (`ApprovedForDelayedPayment`). Company users can't choose online payment.
- `checkout/payment-methods` sends only the enabled gateways to Angular.
- Geidea popup, with a fallback to the hosted page (`core/utils/geidea-checkout.ts`).
- Delivery: cities and remote areas are looked up by **ID** (not by name) and only active ones are used. One `CalculateShippingAsync` is used by both the quote and order creation. Product free-delivery rules work: all lines must allow it, and the highest minimum wins.
- SSR is set up (`@angular/ssr`, `server.ts`, `RenderMode.Server` for storefront routes). Product and blog pages use slug routes. Legacy MVC URLs get 301s from the SSR server (`legacy-redirects.ts`). Sitemap and robots come from the API. The Apple Pay `.well-known` file is in place.

---

## Phase 1 — Security fixes (do first, small, high risk) — ✅ done 2026-11-24

> **What changed:**
> - Tabby returns are always verified with Tabby, and the payment must have this order's reference and amount. `CREATED` no longer counts as paid.
> - Tamara webhooks need a token signed with `NotificationToken` (an HS256 JWT). Anything else gets a 401. Cancellations only happen when Tamara's own API says so.
> - Geidea callbacks cancel an order only when Geidea reports a definite failure. Anything else stays pending for the background job to settle.
> - Gateway, DB, SMTP and Google secrets moved to `appsettings.{Development,Production}.local.json`, which git ignores.
>
> The rules are in `Infrastructure/Payments/PaymentVerificationRules.cs`, with unit tests in `tests/.../Payments/`.
> 1.3: the HMAC signature check on Geidea callbacks was not added. Its exact format still needs confirming against Geidea's documentation. Until then, the check with Geidea's API plus the "definite failure only" rule keeps forged callbacks harmless.

| # | Gap | Fix | Files |
|---|---|---|---|
| 1.1 | ❌ **The Tabby return trusts `?status=` from the URL.** `authorized/created/approved/success/paid` marks the order paid without asking Tabby. | Always call `VerifyPaymentAsync(paymentId)` and compare the amount to `order.OrderTotal`. Ignore the query status except for logging. | `PaymentService.cs:892-928` |
| 1.2 | 🟡 **Tamara webhook check:** it compares the Bearer value as plain text with `NotificationToken`. Tamara actually sends an HS256 JWT signed with that token (in the header and/or the `tamaraToken` query parameter). An empty token lets every request through. | Validate the JWT signature and expiry with `NotificationToken` (`System.IdentityModel.Tokens.Jwt`). Reject with 401. Fail closed when the token isn't configured in Production. | `PaymentService.cs:549-566`, `PaymentEndpoints.cs:74` |
| 1.3 | 🟡 Geidea callback: no signature check. It re-verifies server-to-server, which is acceptable. | Verify the callback signature (HMAC over the documented fields with `MerchantApiPassword`). Keep the server re-verify. | `PaymentEndpoints.cs:19`, `GeideaHelper.cs` |
| 1.4 | ⚠️ `appsettings.json:95` (`Tappy:MerchantId`) holds an `sk_…` key in the **uncommitted** working copy. | Move it to user-secrets or the git-ignored local config before committing. Rotate it if it is live. | `appsettings.json` |

**Done when:** a tampered Tabby return URL leaves the order Pending; a forged Tamara webhook gets 401; tests cover both.

## Phase 2 — Payment confirmation reliability — 🟡 2.1, 2.2 and the Tamara return fix done 2026-11-24

> **What changed:**
> - **2.1:** confirmation is now an atomic conditional `UPDATE` ("claim") inside a transaction with the stock deduction and promo recording. Only the caller whose update matches the row runs those steps. Automatic cancellation uses the same kind of atomic update, so a paid order can't be cancelled by the background job.
>   - Payment states that can be claimed as Paid: `Pending`, `Rejected` (cancelled automatically, but a late payment arrived) and `Authorized`. Admin-cancelled, refunded and paid orders are never reopened.
>   - Service purchases use the same claim.
>   - No `rowversion` column was needed.
> - **2.2:** `IPromoCodeService.ReleaseUsageForOrderAsync` is called on admin cancel, force cancel (when the order wasn't shipped) and automatic cancel. `RecordUsageAsync` now records at most once per order and increments `TimesUsed` atomically. Refunds don't release promo usage: the order was fulfilled, so the promo counts as used.
> - **Tamara return** (deferred from Phase 1): the stored Tamara order id wins. A different id in the URL is used only when none is stored, and only if Tamara reports this order as its `order_reference_id`.
> - **2.5:** dropped. A payment the gateway reports as paid is now claimed as Paid, so an expired unpaid order has nothing held to void. One edge case remains: a paid order whose amount doesn't match is still cancelled automatically when it expires. It should be flagged for an admin instead.
> - **Still open:** 2.3 (guest promo limits, Q5), 2.4 (stock re-check at confirmation, Q2), 2.6 (`Authorized` state, Q3), and admin cancel racing a webhook. The claim logic is covered by database tests (`PaymentClaimDatabaseTests`, `Category=Database`) that run against a throwaway SQL Server LocalDB database. They cover:
>   - 8 concurrent confirmations deducting stock and recording the promo exactly once;
>   - an automatic cancel holding an out-of-date copy of an order that was paid in the meantime;
>   - a late payment on an automatically cancelled order;
>   - replays on admin-cancelled and refunded orders;
>   - promo usage being recorded once and then released.
>
>   A deliberately broken claim condition makes the concurrency test fail.

| # | Gap | Fix |
|---|---|---|
| 2.1 | ❌ No concurrency token. If the callback, webhook and background job run together, stock can be deducted twice (`InventoryService.DeductStockForOrderAsync` has no guard). | Add a `RowVersion` (`[Timestamp]`) to `OrderHeader` (patch the schema in `DatabaseInitializer` plus the `backend/sql` script). In `MarkPaidAndFulfillAsync`, use one transaction: state guard → status → stock → promo → save. Catch `DbUpdateConcurrencyException` → reload → return if already Paid. Add a `StockDeducted` flag (or an inventory-movement check) so deduction is idempotent. |
| 2.2 | ❌ Promo usage is never released on cancel or refund. | Add `IPromoCodeService.ReleaseUsageAsync(orderId)`. Call it from admin cancel, `CancelUnpaidOrderAsync`, and a full refund. |
| 2.3 | 🟡 Promo usage is recorded only for signed-in users, so guest orders bypass per-user limits. | Decision: track by email/phone for guests, or keep the current behaviour (open question Q5). |
| 2.4 | 🟡 Stock is not reserved while an online payment is pending, so overselling is possible. | Decision Q2. Recommended: keep deducting on Paid, but check stock again at confirmation. If stock ran out, flag the order for admin attention or refund it. Full reservation (with a TTL) only if overselling is actually seen. |
| 2.5 | 🟡 Cancelling an expired order doesn't call the gateway (`PaymentService.cs:1904`). | Call the gateway's cancel for Tamara/Tabby when they report an authorised but abandoned payment. |
| 2.6 | 🟡 Nothing sets `PaymentStatuses.Authorized`. | Either set it on Tamara/Tabby authorisation (before capture at ship) or remove the status. |

## Phase 3 — Gateway abstraction (refactor, no behaviour change)

- Add `IPaymentGateway` (`Method`, `InitiateAsync`, `VerifyAsync`, `CaptureAsync`, `CancelAsync`, `RefundAsync`) and `IPaymentGatewayFactory`. Write `GeideaGateway`, `TabbyGateway` and `TamaraGateway` adapters that wrap the existing helpers.
- Replace the `switch` blocks in `PaymentService.cs` (181, 211, 1700, 1751) and `AdminOrderService.cs:714-768` with calls through the factory.
- Swap `new HttpClient()` in `GeideaHelper`/`TappyHelper` for typed `IHttpClientFactory` clients with `Microsoft.Extensions.Http.Resilience` (retry + timeout).
- Add `Geidea:Enabled` so all three gateways work the same way.
- Split `PaymentService.cs` (about 2,000 lines) into initiation, confirmation and reconciliation services.

Do this **after** Phases 1–2 so the security fixes ship quickly, and check it with the tests from Phase 4.

## Phase 4 — Delivery calculator & combo free delivery — ✅ done 2026-11-24

> **What changed:**
> - **4.1:** `ComboOffer` now maps `AllowFreeDelivery` and `FreeDeliveryMinimumAmount`. These columns already exist in the legacy database (MVC migration `20261020111700_combo_free_delivery`). `backend/sql/2026-11-combo-free-delivery.sql` adds them only where they are missing. A combo cart line now uses the combo's own settings instead of its first product's.
> - **4.2:** `Infrastructure/Delivery/DeliveryCalculator` is the single rule for both the quote and order creation:
>   - free delivery needs every line to allow it, and the threshold is the highest minimum among the lines;
>   - the threshold is compared with the subtotal before the promo discount (Q1 kept as it is);
>   - a missing or deleted product or combo is never free.
>
>   City and area validation is now the same for the quote and the order: the quote also rejects an invalid area, and requires one when the city has areas.
> - **4.3:** `ShippingQuoteResponse` adds `baseShipping`, `isFreeDelivery`, `freeDeliveryAvailable`, `freeDeliveryThreshold` and `amountRemainingForFreeDelivery`. The public product and combo DTOs expose `allowFreeDelivery` and `freeDeliveryMinimumAmount`.
> - **4.4:** `DeliveryCalculatorTests` (9 tests).
> - **4.5:** the admin combo form has a new Delivery card (checkbox, plus a minimum field shown when ticked, with a hint). The combo form and both product forms reject a missing or negative minimum. A minimum of 0 means "always free"; Q7 was not made a rule.
> - **4.6:** product and combo pages show a "Free delivery (on orders over AED X)" label. In the checkout summary, free shipping shows the base charge crossed out next to "Free", followed by "You get free delivery" or "Add AED X more…" with a progress bar.
>
> **Checked in the browser** against a throwaway LocalDB database (not the configured production database or API):
> - product label (AED 200) and combo label (AED 300);
> - one combo at AED 180: "Add AED 120.00 more", bar at 60%, AED 25 shipping;
> - two combos at AED 360: shipping shows "~~AED 25.00~~ Free";
> - remote area charge of AED 60; invalid or missing area returns 400;
> - admin combo form: a negative minimum is blocked; saving 250 persists.
>
> - **Cart page (added afterwards):** `CartResponse.freeDelivery` (`available`, `threshold`, `qualifies`, `amountRemaining`) uses the same calculator. The cart page shows the same message and progress bar, and the shipping row says "Free" once the order qualifies. Checked in the browser:
>   - AED 120: "Add AED 80.00 more", bar at 60%;
>   - AED 240: shipping shows "Free";
>   - after adding an item that doesn't allow free delivery: the free-delivery message and bar are hidden.

| # | Gap | Fix |
|---|---|---|
| 4.1 | ❌ `ComboOffer` has no `AllowFreeDelivery` or `FreeDeliveryMinimumAmount`. A combo cart line currently uses its **first component product's** flags (`CartService.cs:187, 214, 405`). | Add both fields to the entity, EF config, `DatabaseInitializer` schema patch and `backend/sql`. Store `ComboOfferId` on cart lines (or resolve it). Use the combo's own flags. If a combo can't be found, the line is not free. |
| 4.2 | 🟡 The logic is private inside `CheckoutService`, and the city/area lookup is written 3 times (`:190-215`, `:477-493`, `:506-518`). The quote and order creation also behave differently (the quote ignores a missing or invalid area, order creation throws). | Extract `IDeliveryCalculator.Calculate(lines, subtotal, cityId, remoteAreaId)` → `{ baseCharge, allLinesAllowFree, threshold, isFree, finalCharge, amountRemainingForFree }`. Make the quote and order creation validate areas the same way. |
| 4.3 | ❌ `ShippingQuoteResponse` has no free-delivery information. | Add `isFree`, `freeDeliveryThreshold` and `amountRemainingForFree`. |
| 4.4 | ❌ No tests. The `Application.Tests` project only has an empty `UnitTest1`. | Add tests for: all free above/below the threshold, one line not allowed, combo + product with different minimums, remote area vs. city, inactive city. Also add Phase 1/2 tests (tampered Tabby return, invalid Tamara JWT, double confirmation). This needs an Infrastructure reference or the calculator in Application. |
| 4.5 | ❌ Admin combo API and form have no free-delivery fields. | Add them to `AdminComboOfferContracts`, the service and validation (`minimum >= 0`). In the Angular combo form, add a checkbox plus a minimum field that is enabled and required only when the box is ticked. Add the same conditional validator to the product forms. |
| 4.6 | ❌ The storefront shows nothing about free delivery. | Expose the fields in the public product and combo DTOs. Show a "Free delivery over AED X" label on product and combo pages. In checkout and cart, show a free badge and a progress bar ("Add AED X more"). |

Rule to keep: the threshold is compared with the **subtotal before the promo discount** (current behaviour; confirm in Q1).

## Phase 5 — SEO

| # | Gap | Fix |
|---|---|---|
| 5.1 | ❌ **The SmarterASP build is client-only** (`angular.json` smarterasp `"ssr": false`). On that host, crawlers get an empty page and legacy URLs get no real 301s. | Decision Q4: host the Node SSR (Render or another Node host), **or** turn on build-time prerendering for static and listing routes and add IIS rewrite rules for legacy 301s in `deploy/smarterasp/web.config`. |
| 5.2 | ❌ The web.config sends `sitemap.xml`/`robots.txt` with a **302 to another host** (`ftempurl.com`). | Serve both from the storefront domain: an IIS reverse-proxy rewrite to the API, or generate them at build time. Never redirect robots.txt across hosts. |
| 5.3 | 🟡 `SeoService` (Angular) has no hreflang, robots/noindex or `og:locale`. Its canonical falls back to `window.location` (wrong under SSR). JSON-LD is used only on the home page. | Add `setRobots`, `setAlternates` and `og:locale`. Build the canonical from the base URL + route (no query string). Add Product + Offer + BreadcrumbList JSON-LD to product pages, BlogPosting to blog pages, and Product/Offer to combo pages. Set `noindex` on cart, checkout, account, auth, admin and order tracking. |
| 5.4 | 🟡 Combo, flash-sale and service detail pages set no SEO. Combos are routed by ID. | Add `Slug` + `MetaTitle(Ar)` + `MetaDescription(Ar)` to `ComboOffer` and a `combos/by-slug/{slug}` endpoint. Change the route to `combos/:slug` with an ID fallback that redirects. |
| 5.5 | ❌ No route resolvers, so under SSR the page can render before its data loads. | Add `productResolver`, `comboResolver` and `blogResolver`. Return a 404 (`RESPONSE` status) plus noindex for missing items. Use HTTP transfer cache. |
| 5.6 | ❌ Old slugs have no 301s. | Add a `SlugHistory` table. Have by-slug endpoints return `redirectTo`, and the SSR server returns a 301. |
| 5.7 | 🟡 `Product` has only `SlugEn`. `BlogPost` has no `MetaTitle`. | Add `MetaTitle(Ar)` and `MetaDescription(Ar)` to Product (and `SlugAr` if wanted), plus the admin form fields. |
| 5.8 | 🟡 Under SSR the locale is always `ar`. Language comes from `?culture=`. | Short term: make SSR read `?culture=` so `en` pages render as English with `lang`/`dir` set. Long term (Q6): `/ar` and `/en` path prefixes with 301s from `?culture=` URLs. This is a large change, so plan it separately. |
| 5.9 | 🟡 The sitemap uses ID URLs for combos, services and flash sales, uses `?culture=` alternates, and includes inactive products. | Use slugs. Exclude inactive or expired items. Update the alternates when 5.8 lands. |
| 5.10 | ❌ No Google Search Console verification. | Add the verification meta tag from config (or a `google*.html` file in `public/`). |
| 5.11 | 🟡 Performance | Load Google Fonts without blocking rendering (`preconnect` + `display=swap` / self-host). Add `clientCache` rules for hashed assets in web.config. Use `NgOptimizedImage` on product and combo detail heroes. |

## Phase 6 — Angular payment UX

- Result page (`order-confirmation.component.ts`): poll the new `GET checkout/orders/{id}/payment-status` endpoint every 2–3 seconds for up to about 60 seconds. Show Paid, Pending ("we'll confirm shortly") or Failed with a retry button.
- Route the Geidea return through the API (`/api/payments/geidea/return` → verify → 302 to the SPA), the same as Tabby and Tamara.
- Optional: Tabby and Tamara installment widgets on product and cart pages, loaded on demand.
- Optional: a Tabby webhook (register it in the Tabby dashboard) as a second confirmation path. It goes through the same idempotent confirmation.

---

## Suggested order

1. Phase 1 (security). Do it now, as a small PR.
2. Phase 2.1 and 2.2 (idempotency, promo release) + tests from 4.4.
3. Phase 4 (combo free delivery + calculator + UI).
4. Phase 5.2, 5.3, 5.10, then 5.1 once hosting is decided, then 5.4–5.7.
5. Phase 6 (result polling, Geidea return through the API).
6. Phase 3 refactor (gateway abstraction, typed clients) once the tests are in place.
7. Phase 5.8 (`/ar` and `/en` routing) as its own project.
8. Sandbox end-to-end tests for each gateway (Tamara `UseSandbox`, Tabby test keys, Geidea test env), then resubmit the sitemap in Search Console.

## Open questions

- **Q1** Is the free-delivery threshold compared with the subtotal before or after the promo? (Currently before.)
- **Q2** Reserve stock at order creation, or keep deducting on Paid with a re-check?
- **Q3** Should Tamara/Tabby use an `Authorized` state before capture at shipping?
- **Q4** SmarterASP production hosting: Node SSR elsewhere, or static prerender + IIS rewrites?
- **Q5** Should guest orders count toward promo usage limits (by email/phone)?
- **Q6** Is the move from `?culture=` to `/ar` and `/en` URLs in scope now?
- **Q7** Is "minimum amount required when free delivery is allowed" a business rule?
