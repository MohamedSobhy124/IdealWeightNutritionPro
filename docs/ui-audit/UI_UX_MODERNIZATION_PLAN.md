# UI/UX modernization plan — Ideal Weight Nutrition

Implementation order for the issues in [UI_UX_AUDIT.md](UI_UX_AUDIT.md). Do this in the frontend only. Do not change API contracts, database tables, authentication, payment, or stock rules.

An older plan lives at [docs/UI-MODERNIZATION-PLAN.md](../UI-MODERNIZATION-PLAN.md) (20 Nov 2026). This document is the 23 Nov 2026 pass against the running Arabic UI. Where they disagree, follow the measured bugs here.

## Guardrails

- Arabic RTL is the default. Any new CSS uses logical properties (`inline`, `start`, `end`).
- Percent and signed numbers that must read left to right get `dir="ltr"` or `unicode-bidi: isolate`.
- One solid primary button per action cluster.
- No new green or orange fills on large regions. Tokens are defined in [DESIGN_SYSTEM_PROPOSAL.md](DESIGN_SYSTEM_PROPOSAL.md).
- Do not hide out-of-stock products. Do not enable Add to cart when `inStock` is false.
- Admin screens were not seen. Skip admin visual edits until a staff session is available. Customer fixes must not restyle the admin shell by accident. Prefer customer component CSS and token values over broad `.ui-btn` changes. Token updates will affect admin, so keep admin contrast the same or better: white text on `#0B5F45`, charcoal text on white.
- After each step, check 320×568, 375×812, and 1440×900 on the screen that step touches. The other six viewports in [RESPONSIVE_TEST_REPORT.md](RESPONSIVE_TEST_REPORT.md) are still unmeasured. Check 768 and 1024 before calling a breakpoint done, because `lg` is 1024 and several bugs are "hidden from `lg` but still displayed".

## P1 — Customer mobile

### 1. Repair the product purchase bar

Files:

- [frontend/src/app/features/catalogue/product-detail.component.html](../../frontend/src/app/features/catalogue/product-detail.component.html) around the `.product-mobile-actions` block (line 285).
- [frontend/src/app/features/catalogue/product-detail.component.css](../../frontend/src/app/features/catalogue/product-detail.component.css) `.product-mobile-actions` (about line 290) and the `@media (max-width: 639px)` overrides (about line 487).
- [frontend/src/styles/_components.scss](../../frontend/src/styles/_components.scss) `.checkout-mobile-bar` (about line 489).

Done when, at 320 and 375:

- Add to cart is at least 48px tall, its box is fully inside the viewport (`x >= 8` and `right <= innerWidth - 8`), and the Arabic label is not clipped.
- The quantity input is about 44px wide, not 138px (`width: 2.75rem; min-width: 0; flex: 0 0 2.75rem` on the input, and `min-width: 0` on the stepper).
- The bar does not overlap `.app-bottom-nav`. Its `bottom` is at least the nav height (`3.5rem` plus safe area) plus any gap. Measured overlap was 1px at 375 (bar bottom 756, nav top 755).
- Price is visible in the bar. Buy now and quantity can sit in `.product-buy-panel` instead of the bar.

Suggested structure under 640px: a column, price row on top, primary button `flex: 1 1 auto; width: 100%` underneath. Drop `padding-inline-end: 4.35rem` on this bar if the contact buttons are moved (step 2). If they stay in the corner, put them above the bar (`bottom` greater than the bar height plus the nav) rather than squeezing the button.

The same bar class is used on cart, checkout, services, combos, and returns. After the shared `.checkout-mobile-bar` change, open those templates and make sure a single primary button can still share the row with a short secondary button at 320px. Cart and checkout were only seen empty, so verify with items before shipping that shared CSS.

### 2. Stop the contact buttons covering content

File: [frontend/src/app/shared/contact-floating/contact-floating.component.css](../../frontend/src/app/shared/contact-floating/contact-floating.component.css).

The host is fixed, `z-index: 40`, 48px buttons, `bottom: calc(5.25rem + safe-area)` below 1024px. On the homepage at 320 they cover the trust cards.

Done when:

- At 320, the phone and WhatsApp boxes do not intersect the trust cards or Add to cart.
- `prefers-reduced-motion: reduce` sets `animation: none` on `.floating-call-btn` and `.floating-whatsapp-btn`.
- Tooltip text still has a name for assistive tech (`aria-label` already expected on the links).

### 3. Header targets on a 320px screen

Files: [frontend/src/app/layout/customer-header.component.html](../../frontend/src/app/layout/customer-header.component.html), [frontend/src/app/layout/customer-header.component.css](../../frontend/src/app/layout/customer-header.component.css).

Icon buttons measured 40×40. At 320 the product header shows language, theme, search, back, and menu, and the wordmark width is 0.

Done when icon buttons are at least 44×44, and under 360px the theme toggle is not in the header (it can stay in the drawer). Logo remains visible.

## P2 — Shopping path and false states

### 4. Skeleton instead of a false error

Screens that painted a failure and then showed data:

- Shop: "تعذر تحميل المنتجات…" then 24 cards.
- Offers: "لا توجد منتجات مخفضة حالياً." then 10 cards.
- Services: "تعذر تحميل الخدمات." then cards.
- Product full document load: "المنتج غير موجود" even though `GET /api/products/nxt-nutrition-beef-isolate` returned 200. In-app routing then worked.

Files to inspect, without changing the HTTP calls:

- [frontend/src/app/features/catalogue/shop-page.component.html](../../frontend/src/app/features/catalogue/shop-page.component.html) (the `@if (loading())` / `@else if (error())` branches, about lines 44–63).
- The offers and services templates, same pattern.
- [frontend/src/app/features/catalogue/product-detail.component.ts](../../frontend/src/app/features/catalogue/product-detail.component.ts) `ngOnInit` (about lines 206–254), where any error sets `productNotFound`.

Done when a reload of `/shop`, `/offers`, `/services`, and `/product/nxt-nutrition-beef-isolate` never shows the error or empty heading unless the browser request has finished with a real failure. A server render may send a shell. The client should keep `loading` true until its own request settles, and a transfer of a server error must not stick if the browser request succeeds.

Do not change the product endpoint.

## P3 — Identity

### 5. Retune tokens

File: [frontend/src/styles/_tokens.scss](../../frontend/src/styles/_tokens.scss).

Apply the hex values in [DESIGN_SYSTEM_PROPOSAL.md](DESIGN_SYSTEM_PROPOSAL.md) to the existing `--rgb-*` channels, including the `.dark` block. Do not add a parallel set of hex variables.

Then replace hardcoded `rgb(240, 240, 240)` and one-off whites in the product stepper and accordions with `var(--surface)` and `var(--border)`.

Check white on `#0B5F45`, `#1C1917` on `#C6E34A`, and `#5E584F` on `#F6F3EC` before merging.

### 6. Hero and homepage

File: [frontend/src/app/features/home/home-page.component.html](../../frontend/src/app/features/home/home-page.component.html) and its CSS.

- Use a product image in `.hero-banner` when the CMS or the first featured product has one. The forest color is a scrim, not the whole slide.
- Trust pills must sit inside the hero at 320px. Measured pills started at y=40 while the hero started at y=96.
- Secondary CTA is a solid light button, not `rgba(255, 255, 255, 0.08)`.
- Brand row: brand image or the brand name. Remove the repeated Material store icon.
- Remove the duplicate newsletter block (`.home-newsletter`, about line 410) and keep the footer form.

## P4 — Cards and product detail

### 7. Discount badge direction

File: [frontend/src/app/shared/ui/ui-product-card.component.ts](../../frontend/src/app/shared/ui/ui-product-card.component.ts) line 25.

```html
<ui-badge variant="citrus"><span dir="ltr">-{{ discountPercent() }}%</span></ui-badge>
```

Search for other leading minuses in customer templates (flash sale, combo, quick view) and isolate them the same way.

Done when the shop and the homepage carousel show `-17%` with the hyphen on the left of the digits, in Arabic mode.

### 8. Out of stock and image scale

Same product card component, lines 29–32 and the `img` classes.

- Replace the full-bleed `bg-ink-900/70` bar with a small badge that does not cover the product.
- Leave the action button disabled when there is no stock.
- If `naturalWidth` is under 200, do not apply the hover scale. The image stays `object-contain` on `--stage`.
- Category tiles should not force a landscape file into a 214×286 portrait crop. Use a consistent ratio and `object-fit: cover` with a centered position, or `contain` on the stage.

### 9. Product page hierarchy

- One `h1` (the buy box). The breadcrumb is not a heading.
- Flavor chips min-height 44px.
- Stepper and accordions use surface tokens (step 5).
- Desktop Add to cart stays the solid button. It already measures 275×44 at 1440.

## P5 — Customer desktop

### 10. Shop filter button and sort

Files:

- [frontend/src/app/features/catalogue/shop-page.component.css](../../frontend/src/app/features/catalogue/shop-page.component.css) lines 49–87.
- [frontend/src/app/features/catalogue/shop-page.component.html](../../frontend/src/app/features/catalogue/shop-page.component.html) lines 7–43.

`lg:hidden` on `.shop-mobile-filter-btn` loses to `.shop-filter-btn { display: inline-flex }`. At 1440 the button was fixed and 83×40.

Add to the component CSS:

```css
@media (min-width: 1024px) {
  .shop-mobile-filter-btn {
    display: none;
  }
}
```

Confirm `ui-segmented` (template class `lg:hidden`) is actually `display: none` at 1440. If its host sets `display`, hide it in that component's CSS from 1024px up. The sidebar sort select is the desktop control.

Done when the 1440 shop shows one filter column and one sort control, and the fixed filter button is gone.

## P6 and P7 — Admin

Blocked. `/admin` rendered an empty document and then `/auth/login`. No staff token was stored.

When someone is signed in, review dashboard, products, categories, brands, inventory, orders, customers, promotions, and settings at 1440 and 1920 only. Do not spend the first pass on admin at 320. Fix anything that fails contrast against the new tokens. Do not rebuild admin navigation in this effort.

Also replace the blank redirect with an immediate trip to login, or a single "sign in" line, so the empty document is gone. That customer-visible flash can ship before the admin screens are reviewed.

## P8 — Polish

- Login `document.title` should match the visible heading plus the brand, as the shop title already does.
- Footer and header spacing after the newsletter removal.
- Reduced motion for the hero and the promo ticker.

## Verification

For each shipped step, on the running app:

| Step | What to measure |
|---|---|
| 1 | At 320 and 375, Add to cart box is inside the viewport and at least 48px tall |
| 2 | Contact buttons do not intersect trust cards or Add to cart |
| 3 | Header icon buttons ≥ 44px. Theme toggle not in the header under 360px |
| 4 | Reload product, shop, offers, services. No error heading before data |
| 5–6 | Hero shows a photo. Tokens match the proposal. No `#f0f0f0` on the stepper |
| 7 | Badge text is `-17%` in RTL |
| 8 | Out-of-stock badge does not stripe the photo. Tiny images are not scaled up |
| 9 | One `h1` on the product page |
| 10 | At 1440, `.shop-mobile-filter-btn` is `display: none` |

Keyboard: tab from the skip link through the header and the buy button, and confirm the focus ring is visible. This was not done in the audit.

## Out of scope

- New endpoints, slug rules, image pipeline uploads, and payment providers.
- Seeding or deleting catalogue data (including the "weewew" test product). Fix how cards present bad images. Content cleanup is separate.
- Writing English or dark-mode screenshots until those modes are actually switched in a browser.
