# UI/UX audit — Ideal Weight Nutrition

Live inspection of the Angular storefront at `http://localhost:4200` on 23 Nov 2026. The store was Arabic (`lang="ar"`, `dir="rtl"`). There was no access token in `localStorage`.

Measurements come from the rendered DOM (`getBoundingClientRect` and computed style) after client hydration. A screenshot is cited only when it matched that DOM. Frames that showed a different viewport than the measured page are omitted.

This audit does not change CSS, APIs, data, or authentication.

## How to read an issue

| Field | Meaning |
|---|---|
| Severity | Critical blocks purchase or hides the primary action. High breaks a main shopping task or the brand impression. Medium is confusing or inconsistent. Low is polish. |
| Priority | P1 customer mobile. P2 shopping and checkout. P3 visual identity. P4 listing and product detail. P5 customer desktop. P6 admin desktop. P7 admin responsive. P8 polish. |

## Coverage

### Opened and inspected

| Screen | Route | Viewports measured | Result |
|---|---|---|---|
| Homepage | `/` | 1440×900, 320×568 | Rendered. Hero, trust row, goals, categories, product carousels, reviews, two newsletter forms, footer, bottom nav on the small viewport. |
| Shop | `/shop` | 1440×900 | First paint showed skeletons, then an error heading, then 40 products (24 on page 1). |
| Product detail | `/product/nxt-nutrition-beef-isolate` | 1440×900, 375×812, 320×568 | Direct load painted "المنتج غير موجود". In-app navigation from the shop then rendered the product. API `GET /api/products/nxt-nutrition-beef-isolate` returned 200. |
| Login | `/auth/login` | 1440×900 | Email, password, show-password, disabled submit, Google, register, forgot password. |
| Cart | `/cart` | 1440×900 | Empty state: "سلتك فارغة" and "متابعة التسوق". |
| Checkout | `/checkout` | 1440×900 | Empty cart: "سلتك فارغة" and a link back to the cart. No checkout steps were reached. |
| Offers | `/offers` | 1440×900 | First paint: "لا توجد منتجات مخفضة حالياً". After the client request: 10 discounted cards. |
| Services | `/services` | 1440×900 | First paint: "تعذر تحميل الخدمات". After the client request: service cards with prices. |
| Admin | `/admin` | 1440×900 | Blank document, then redirect to `/auth/login`. |

### Not opened

Do not treat these as tested. They need a signed-in session or a later pass:

Account dashboard, orders, subscriptions, wishlist, returns, a cart with items, checkout steps, payment, order confirmation, order tracking, register, forgot password, reset password, blog, flash-sale list and detail, combo list and detail, static pages (about, privacy, terms, shipping, returns, help), newsletter unsubscribe, quick-view modal, image zoom, mobile filter drawer, and every admin screen.

English and dark mode were not toggled. Switching them writes a preference.

Viewports not measured: 430×932, 768×1024, 1024×1366, 1366×768, 1920×1080, 2560×1440. See [RESPONSIVE_TEST_REPORT.md](RESPONSIVE_TEST_REPORT.md).

## Design system as rendered

Tokens live in [frontend/src/styles/_tokens.scss](../../frontend/src/styles/_tokens.scss). Tailwind maps them in [frontend/tailwind.config.js](../../frontend/tailwind.config.js).

| Token | Current value | What the page does with it |
|---|---|---|
| Brand solid | `#145C3A` (`rgb(20 92 58)`) | Primary buttons, active nav, in-stock text. |
| Lime | `#D6E8D8` | Too close to the cream page. It does not read as a fresh accent. |
| Citrus | `#B45309` | Sale prices. Correct role, easy to miss next to charcoal prices. |
| Page background | `#F7F6F3` | Warm off-white. Sound base. |
| Ink 900 | `#1C1917` | Headings. Strong. |
| Ink 400 | `#A8A299` | Used for struck prices and review counts. Fails WCAG 2.2 AA for normal text on white. |
| Focus ring | 3px brand at 35% | Defined. Keyboard focus was not walked. |

Hardcoded `#f0f0f0` still paints the product quantity stepper and the description accordions (`rgb(240, 240, 240)` on the product page). Those controls look like a different product from the forest buttons.

Fonts in use: IBM Plex Sans Arabic and Tajawal for the Arabic UI. Manrope and Outfit are configured for Latin.

## Issues

### PDP-MOBILE-BAR — Add to cart is clipped on phones

- Screen: Product detail, sticky purchase bar (`.product-mobile-actions`).
- Viewports: 375×812 and 320×568.
- Severity: Critical. Priority: P1.
- Screenshot: none. The phone screenshot canvas was wider than the emulated layout, so the frame is not cited. The boxes below were measured on the live page.

The bar in [product-detail.component.html](../../frontend/src/app/features/catalogue/product-detail.component.html) (about line 285) is one flex row: quantity, hidden price meta, Buy now, Add to cart. It is `position: fixed`, 106px tall, and sits on `bottom: calc(3.5rem + safe area)`.

| Control | 375×812 | 320×568 |
|---|---|---|
| Quantity input | 138×43 | 138×43 |
| Buy now | 60×44 | 60×44 at x=22 |
| Add to cart | **22×82** | **22×82 at x=−7** (off the left edge) |

The number input is classed `w-11` but renders at 138px, so the quantity group is about 218px. `min-width: 0` and `flex-shrink` then crush Add to cart. Extra `padding-inline-end: 4.35rem` on `.product-mobile-actions` reserves space for the contact buttons and leaves even less room.

At 375 the bar bottom is 756px and the bottom nav top is 755px, so they overlap by 1px.

Recommended solution: under 640px, the sticky bar shows the price and one Add to cart button at least 48px tall and full width of the bar. Keep quantity and Buy now in the product panel. Set the quantity input to `width: 2.75rem; min-width: 0; flex: 0 0 2.75rem`. Do not pad the bar by 4.35rem if that padding collides with the primary button. Files: [product-detail.component.css](../../frontend/src/app/features/catalogue/product-detail.component.css) (`.product-mobile-actions` around line 290) and [frontend/src/styles/_components.scss](../../frontend/src/styles/_components.scss) (`.checkout-mobile-bar` around line 489).

### FAB-OVERLAP — Phone and WhatsApp cover content

- Screen: Homepage trust row at 320×568. The same fixed stack is on every customer page.
- Component: [contact-floating.component.css](../../frontend/src/app/shared/contact-floating/contact-floating.component.css). Host is `position: fixed`, `z-index: 40`, 48×48 buttons, `bottom: calc(5.25rem + safe area)` below 1024px.
- Severity: High. Priority: P1.
- What was seen: the purple phone button and green WhatsApp button sit on top of the "توصيل في نفس اليوم" and "دفع آمن" cards. On the product page the same stack shares the corner the buy bar is trying to avoid.

Recommended solution: keep the buttons, and add bottom padding on the homepage trust and product sections equal to the stack height (two 48px buttons plus gap plus the bottom nav). On product, cart, checkout, and service pages, place the sticky action bar above the stack, or move the stack above the bar without covering the button label. Respect `prefers-reduced-motion` for `call-pulse` and `whatsapp-pulse`.

### SHOP-FILTER-DESKTOP — Mobile filter button stays fixed on desktop

- Screen: Shop. Viewport: 1440×900 (layout width 1425).
- Severity: High. Priority: P5.
- What was seen: `.shop-mobile-filter-btn` computed `display: flex` and `position: fixed` (83×40). The template adds `lg:hidden`, but [shop-page.component.css](../../frontend/src/app/features/catalogue/shop-page.component.css) sets `.shop-filter-btn { display: inline-flex }` and `.shop-mobile-filter-btn { position: fixed }` (lines 49–81). Encapsulated component CSS wins over the Tailwind utility. The sticky sidebar was also present at x=1105, width 280, height 432.

Recommended solution: in the component stylesheet, `@media (min-width: 1024px) { .shop-mobile-filter-btn { display: none; } }`. Leave the sidebar as the only filter surface from the `lg` breakpoint up.

### SHOP-SORT-DUPLICATE — Sort exists twice on desktop

- Screen: Shop. Viewport: 1440×900.
- Severity: Medium. Priority: P5.
- What was seen: the filter sidebar sort control (featured, best sellers, new, price, name) and a second row of the same options as radios above the grid. [shop-page.component.html](../../frontend/src/app/features/catalogue/shop-page.component.html) marks `ui-segmented` with `lg:hidden` (line 38). The radios were still in the accessibility tree and visible in the results area.

Recommended solution: confirm computed `display` of `ui-segmented` at `min-width: 1024px`. If the host sets `display`, hide it in component CSS the same way as the filter button. One sort control per breakpoint.

### BADGE-BIDI — Discount percent reads backwards

- Screen: Homepage product carousel and shop cards. Viewport: 1440×900.
- Screenshot: `home-desktop-1440-products.png`, `shop-desktop-1440.png` (local inspection captures).
- Severity: High. Priority: P4.
- What was seen: badges painted `20%-`, `21%-`, and `17%-` instead of `-20%`. The template is `-{{ discountPercent() }}%` in [ui-product-card.component.ts](../../frontend/src/app/shared/ui/ui-product-card.component.ts) (line 25). Arabic RTL reorders the hyphen.

Recommended solution: wrap the badge text in `<span dir="ltr">-{{ discountPercent() }}%</span>` or set `unicode-bidi: isolate` and `direction: ltr` on `.ui-badge-citrus` when it contains a percent. Apply the same pattern anywhere a leading minus is rendered in an RTL tree.

### CARD-OOS — Out-of-stock bars fight the buy action

- Screen: Shop cards. Viewport: 1440×900.
- Screenshot: `shop-desktop-1440.png`.
- Severity: High. Priority: P4.
- What was seen: a full-width bar `غير متوفر` (`position: absolute; inset-x: 0; bottom: 0; height: 29px; background: rgba(28, 25, 23, 0.7)`) covers the bottom of the photo. The same cards still show a price and a disabled "أضف إلى السلة". Several of the first products were out of stock, so the grid reads as unavailable even when later cards can be purchased. The bar style is in [ui-product-card.component.ts](../../frontend/src/app/shared/ui/ui-product-card.component.ts) (lines 29–32).

Recommended solution: a small corner badge for stock, not a stripe through the photo. Keep the button disabled and visually quiet. Do not repeat "out of stock" in both a bar and a disabled button label. In-stock cards keep a solid Add to cart.

### CARD-IMAGE-SCALE — Tiny sources are stretched

- Screen: Shop and product related products. Viewport: 1440×900.
- Severity: Medium. Priority: P4.
- What was seen:

| Product | Drawn size | Natural size |
|---|---|---|
| NXT Beef Isolate card | 249×249 | 54×54 |
| Tongkat card | 249×249 | 65×65 |
| Related Adonis cookie | 206×206 | 43×43 |
| Related beta-alanine | 206×206 | 29×30 |

The product page hero image for the same beef protein was 660×660 from a 1000×1000 source, so the PDP photo is fine and the card thumbnail is not. Category tile "التحكم بالوزن" was 214×286 from an 856×695 source, so the crop is portrait while the photo is landscape. Image URLs contain backslashes (`\\Images\\Products\\...`). The browser normalizes them and many still load from `idealweightnutrition.ae`.

Recommended solution: `object-contain` on the cream stage is already the right fit. Skip `scale` on hover when `naturalWidth < 200`. Prefer the largest stored rendition for cards. Category tiles should use the image aspect (or a consistent 4:5 crop with `object-position: center`) instead of stretching a landscape file into a portrait hole.

### HERO-FLAT — Homepage hero is a green panel, not a store

- Screen: Homepage. Viewports: 1440×900 and 320×568.
- Severity: High. Priority: P3.
- What was seen: `.hero-banner` is 1345×560 at 1440, headline 60px / weight 800, white. The primary CTA "تسوق الآن" is a white pill with green text (107×50). The secondary CTA "عروض خاطفة" is white at 8% opacity with a 45% white border. No product photograph was visible in the hero; `.hero-default-gradient` fills the slide. At 320 the hero is 320×230 and the trust pills ("علامات عالمية أصلية", "توصيل سريع داخل الإمارات", "دعم فعلي من خبراء") sit above the hero box (y=40 to y=137 while the hero starts at y=96), so they collide with the header. CTAs at 320 are full width, 280×50, which is good.

Recommended solution: put a real product photograph in the hero, with the green used as a scrim on one side so the type stays readable. Keep one primary action. Make the secondary action a solid cream or white button, not a ghost on green. On narrow screens, stack the trust points inside the hero and allow the hero to grow instead of clipping them. Component: [home-page.component.html](../../frontend/src/app/features/home/home-page.component.html) (`.hero-banner`).

### BRAND-ICONS — Brand row uses a generic shop icon

- Screen: Homepage, above the discounted carousel. Viewport: 1440×900.
- Screenshot: `home-desktop-1440-products.png`.
- Severity: Medium. Priority: P3.
- What was seen: brand names such as Optimum Nutrition, Atlecs, and Dymatize render with the same Material store icon on a pale green circle. They do not look like brand marks.

Recommended solution: use the brand image from the catalogue when it exists. If it is missing, show the name in a quiet type tile. Do not repeat one icon for every brand.

### NEWSLETTER-DUPLICATE — Two email forms on the homepage

- Screen: Homepage.
- Severity: Low. Priority: P8.
- What was seen: a newsletter section ("ابقَ على اطلاع") and a second email field in the footer. Both submit "اشترك".

Recommended solution: keep the footer form. Let the homepage section be a single, quieter prompt or remove it.

### FALSE-EMPTY — Error and empty states flash before data

- Screens: Shop, offers, services, and a direct product URL. Viewport: 1440×900.
- Severity: Medium. Priority: P2.
- What was seen:

| Screen | First paint | After the client request |
|---|---|---|
| Shop | "تعذر تحميل المنتجات…" plus "حاول مرة أخرى", then skeletons | 24 product cards |
| Offers | "لا توجد منتجات مخفضة حالياً." | 10 discounted cards |
| Services | "تعذر تحميل الخدمات." | Service cards, including "فحص الفيتامينات والمعادن" at 99 AED |
| Product, full page load | "المنتج غير موجود" | Stayed on the empty state until navigation happened inside the app |

Recommended solution: while `loading()` is true, show the skeleton only. Do not render the error or empty heading from the server payload when the browser is about to refetch. For product routes, if the document request fails and the browser request can succeed, retry on the client before showing "المنتج غير موجود". This is a presentation fix. Do not change the product API contract.

### PDP-DUAL-H1 — Two level-one headings

- Screen: Product detail. Viewports: 1440×900 and 375×812.
- Severity: Medium. Priority: P4.
- What was seen: the page header and the buy box both render "ان اكس تي بيف بروتين ايزوليت" as `h1` (28px and 32px at 1440). At 375 both fit (343px and 230px).

Recommended solution: one `h1` in the buy box. The breadcrumb trail stays a nav, not a second heading.

### PDP-BUYBOX — Desktop purchase panel is crowded but usable

- Screen: Product detail. Viewport: 1440×900.
- Screenshot: `pdp-desktop-1440.png`.
- Severity: Medium. Priority: P4.
- What was seen: price `د.إ 320.00` at 28.8px, charcoal, because this item is not discounted. Rating 4.8 with reviews. Seven flavor chips, selected chip pale green (`#F3FAF6`, text `#145C3A`). Quantity buttons use `#F0F0F0`. Add to cart is 275×44, `#145C3A` with white text. Buy now, wishlist, and WhatsApp sit beside it. Description, specs, use, notes, and delivery are accordions on the same gray. Reviews are readable, with a verified-purchase line in green.

The long flavor "آيس بلاست بلوراسبيري وكرز" wraps onto its own row. Chips are 13px text and about 39px tall.

Recommended solution: restyle the stepper and accordions with surface and border tokens. Let flavor chips grow to at least 44px tall and wrap as a tidy chip group. Keep Add to cart as the only solid button in the row. Buy now can be the outline button. Wishlist can be an icon.

### TOUCH-TARGETS — Header and chips are under 44px

- Screen: Product detail. Viewport: 375×812.
- Severity: Medium. Priority: P1.
- What was seen: menu, back, search, theme, and language buttons are 40×40. The zoom control is 40×40. Flavor chips are about 39px tall. Breadcrumb links are 16px tall. Bottom nav items at 320×568 are 64×56, which is acceptable. Login inputs at 1440 are 440×43.

Recommended solution: 44×44 minimum for icon buttons in the header and for the quantity stepper. Breadcrumbs can stay smaller if the tap area is padded to 44px.

### HEADER-320 — Brand name disappears on the smallest phone

- Screen: Product header. Viewport: 320×568.
- Severity: Low. Priority: P1.
- What was seen: five 40px controls (language, theme, search, back, menu). The wordmark width is 0. The logo image is the only brand mark, and on the homepage at 320 it sits as a small mark between search and the menu.

Recommended solution: keep the logo at 32–36px, drop the theme toggle into the drawer on viewports under 360px, and keep search, menu, and back.

### LOGIN-TITLE — Login title is the site name

- Screen: Login. Viewport: 1440×900.
- Severity: Low. Priority: P8.
- What was seen: the visible heading is "تسجيل الدخول". `document.title` stayed "ايديال ويت للتغذية". The submit button is disabled until the form is valid. The layout includes a "لماذا تختار ايديال ويت؟" column (authentic products, same-day delivery, secure payment).

Recommended solution: set the document title to the page heading plus the brand, the same way the shop title already does ("المتجر | ايديال ويت للتغذية").

### ADMIN-BLANK — Admin route flashes blank, then login

- Screen: `/admin`. Viewport: 1440×900.
- Severity: Medium. Priority: P6.
- What was seen: the first snapshot contained only the notifications region. About a second later the URL was `/auth/login`. No admin screen was visible. There is no session to inspect dashboard, products, orders, customers, promotions, reports, or settings.

Recommended solution: redirect before paint, or show a one-line "sign in to continue" state instead of an empty document. Admin visual QA starts after a real staff session. Do not invent credentials. Admin layout work stays desktop-first.

### INK-400 — Muted gray fails text contrast

- Screen: Struck prices and supporting text that use `text-ink-400`.
- Severity: Medium. Priority: P3.
- What was seen: `--rgb-ink-400` is `#A8A299`. On white that is below the 4.5:1 ratio WCAG 2.2 AA expects for normal text. Struck prices can be quieter than the sale price, and they still need to be readable.

Recommended solution: use `#5E584F` (or the current ink-600 `#57534E`) for muted text. Keep `#A8A299` for borders and disabled decoration only. Sale price uses the promo ink in [DESIGN_SYSTEM_PROPOSAL.md](DESIGN_SYSTEM_PROPOSAL.md).

### QTY-GRAY — Stepper and accordions ignore the token set

- Screen: Product detail. Viewport: 1440×900.
- Severity: Low. Priority: P8.
- What was seen: minus and plus buttons and the accordion headers compute `background: rgb(240, 240, 240)`. Neighboring buttons use `--color-primary` and white surfaces.

Recommended solution: `background: var(--surface); border: 1px solid var(--border);` and a 44px hit area.

## What is already working

- The page background is a warm off-white, and product photos on the homepage carousel sit in square frames with a clear sale price and a struck list price.
- Desktop Add to cart on the product page is a solid forest button, 275×44, white text on `#145C3A`.
- Out-of-stock Add to cart buttons are `disabled` in the accessibility tree.
- Shop filters at 1440 include categories (صحة-والعافية, الاغذية الصحية, مكملات الرياضين, التحكم بالوزن) and brands once the client request finishes.
- Empty cart and empty checkout explain the state and offer a way back to shopping.
- Bottom nav at 320 has five items at 64×56 with no horizontal overflow (`scrollWidth` matched the viewport on home, shop, and product at the sizes that were measured).
- Skip link "تخطي إلى المحتوى الرئيسي" is present.
- Product reviews on the beef protein page are real sentences with a verified-purchase label.

## Screenshot index

Captured during this inspection. They are local tool captures, not files in the repo.

| File | Matches the DOM | Use |
|---|---|---|
| `home-desktop-1440-products.png` | Yes | Homepage brands, discounted cards, sale price next to a struck price, RTL badge order. |
| `shop-desktop-1440.png` | Yes | Four-column grid, out-of-stock bar, sort chips, filter column. |
| `pdp-desktop-1440.png` | Yes | Buy box, flavors, gray stepper, product photo. |
| `home-desktop-1440-top.png` | Partial | Header and hero are real, but the capture crops the 1440 viewport. |
| `pdp-mobile-375.png`, `home-mobile-320.png`, `login-desktop-1440.png` | No | The capture canvas did not match the emulated viewport. Do not use them as evidence. |
