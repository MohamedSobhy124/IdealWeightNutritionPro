# Responsive test report — Ideal Weight Nutrition

Date: 23 Nov 2026. App: `http://localhost:4200`. Locale: Arabic, RTL.

Viewport changes used Chrome DevTools `Emulation.setDeviceMetricsOverride` in the Cursor browser. Overflow is `documentElement.scrollWidth - clientWidth`. Positions are viewport coordinates from `getBoundingClientRect` after `scrollTo(0, 0)` unless noted.

A screenshot is listed only when the image matched the measured layout. Several captures drew a strip of the page on a wider canvas. Those files are listed under "Do not cite" and were not used as evidence.

## Viewport matrix

| Device | Viewport | Measured | Screens | Horizontal overflow |
|---|---|---|---|---|
| Small mobile | 320×568 | Yes | Homepage, product detail | 0 on both |
| iPhone-sized | 375×812 | Yes | Product detail | 0 |
| Large mobile | 430×932 | No | — | — |
| Small tablet | 768×1024 | No | — | — |
| Large tablet | 1024×1366 | No | — | — |
| Laptop | 1366×768 | No | — | — |
| Desktop | 1440×900 | Yes | Home, shop, product, login, cart, checkout, offers, services, admin redirect | 0 where measured (home, shop, login) |
| Large desktop | 1920×1080 | No | — | — |
| Ultra-wide | 2560×1440 | No | — | — |

The six unmeasured sizes still need a pass before anyone claims the layout is responsive there. CSS breakpoints exist (`xs` 376, `sm` 640, `md` 768, `lg` 1024, `xl` 1280, `2xl` 1536, `3xl` 1920 in [frontend/tailwind.config.js](../../frontend/tailwind.config.js)). Breakpoints are not a test result.

## 320×568 — small mobile

### Homepage

- `innerWidth` 320. Overflow 0.
- Hero box: x=0, y=96, 320×230.
- Headline computed 28px, 280×60, line-height 30.24px. It fits.
- "تسوق الآن" and "عروض خاطفة": each 280×50 at x=20. Full-width stacked buttons. Good hit area.
- Trust pills are outside the hero box:

| Pill | x | y | right | bottom | width |
|---|---|---|---|---|---|
| علامات عالمية أصلية | 141 | 40 | 260 | 67 | 119 |
| توصيل سريع داخل الإمارات | 113 | 75 | 260 | 102 | 147 |
| دعم فعلي من خبراء | 146 | 110 | 260 | 137 | 114 |

The hero starts at y=96, so the first two pills sit in the header band and the third overlaps the top of the hero. Issue HERO-FLAT in the audit.

- Bottom nav: five items, each 64×56. 5×64 = 320. Labels fit the row. No horizontal scroll.
- Header shows language, theme, search, a small logo, and the menu. The wordmark is hidden.
- Phone and WhatsApp fixed buttons cover the trust cards under the hero. Issue FAB-OVERLAP.

Not checked at 320: shop grid, cart, checkout, login, modals, keyboard.

### Product detail

| Control | x | width | height |
|---|---|---|---|
| Minus | 266 | 40 | 40 |
| Quantity input | 128 | 138 | 43 |
| Plus | 88 | 40 | 40 |
| Buy now | 22 | 60 | 44 |
| Add to cart | **−7** | **22** | **82** |

Add to cart is off the screen. Issue PDP-MOBILE-BAR. Overflow of the document is still 0 because the button is `position: fixed` and does not expand `scrollWidth`. A zero overflow number does not mean the button is visible.

Header controls at 320 on this page: menu 40px at x=264, back 40px at x=218, search 40px at x=104, theme 40px at x=60, language 40px at x=16. Cart and login links in the header have width 0 (they belong to the bottom nav). Brand link width 0.

## 375×812 — iPhone-sized

Product detail only.

- `innerWidth` 375, `innerHeight` 812. Overflow 0. `scrollWidth` 375.
- Gallery image and frame: 343px wide at x=16. The image column is full width minus page padding. It is not a half-width column at this size.
- Two `h1` elements. Page title 343px wide at x=16. Buy-box title 230px wide at x=73. Both inside the viewport.
- Sticky bar: x=0, y=650, 375×106, `position: fixed`, bottom edge 756.
- Bottom nav: y=755, 375×57, bottom edge 812. The bar and the nav overlap by 1px.
- Children of the bar:

| Control | x | y | width | height |
|---|---|---|---|---|
| Minus | 322 | 683 | 40 | 40 |
| Quantity input | 183 | 682 | 138 | 43 |
| Plus | 143 | 683 | 40 | 40 |
| Buy now | 77 | 681 | 60 | 44 |
| Add to cart | 48 | 662 | 22 | 82 |

- Touch targets under 44px: header icon buttons 40×40, zoom 40×40, flavor chip "الاناناس المنعش" 103×39, breadcrumb links about 16px tall. Bottom nav was not remeasured at 375.

Shop, home, cart, and checkout were not remeasured at 375.

## 1440×900 — desktop

Device scale factor 1. Reported `innerWidth` 1440. `clientWidth` was 1425 when a vertical scrollbar was present. Overflow 0 on the homepage, shop, and login.

### Homepage (scrolled to y=1800 for the product row)

- Header: x=0, y=35, 1425×113, background `rgba(255, 255, 255, 0.92)`.
- Desktop nav `display: flex`. Bottom nav `display: none`. Menu button `display: none`.
- Hero banner: y=175, 1345×560. Headline 60px, weight 800, white, box 576×120 at x=745.
- Primary CTA "تسوق الآن": 107×50 at y=598, white background, text `rgb(20, 92, 58)`.
- Secondary CTA "عروض خاطفة": 130×50, background `rgba(255, 255, 255, 0.08)`, white text.
- Trust strip under the hero: 1345×76, four columns.
- Quick-start chips: 1345×121.
- Goals section: 1345×240.
- Category section: 1345×342.
- Product cards in the discounted carousel: about 307×483, image 305×305.
- Promo ticker track is wider than the viewport (about 2003px) and translated negative. That is the marquee, not page overflow. `scrollWidth` of the document stayed within the client width.
- Screenshot `home-desktop-1440-products.png` shows brand tiles with a generic shop icon, square product photos, a heavier sale price beside a struck price, and percent badges with the hyphen on the wrong side.

### Shop

- 24 `.product-card` nodes on the first page. Count text "40 منتج".
- Cards about 250×448. First row y=416. Image wells 249×249.
- Filter form: x=1105, y=268, 280×432. Class `ui-sticky-filters hidden lg:block`.
- Mobile filter button still `display: flex`, `position: fixed`, about 83×40. Issue SHOP-FILTER-DESKTOP.
- Sort radios and the sidebar sort select were both present. Issue SHOP-SORT-DUPLICATE.
- Screenshot `shop-desktop-1440.png`.

### Product detail (in-app navigation)

- Add to cart in the buy box: 275×44 at y=648, background `rgb(20, 92, 58)`, white text.
- Main image 660×660 from a 1000×1000 file.
- Flavor chips about 13px type. Selected chip background `rgb(243, 250, 246)`.
- Quantity and accordion chrome `rgb(240, 240, 240)`.
- Related thumbnails upscale sources as small as 29×30 into 206×206 wells.
- Screenshot `pdp-desktop-1440.png`.

### Login

- Email and password inputs 440×43, radius 14px, 14px type.
- Show-password control 44×43.
- Header theme and language buttons 40×40.
- Overflow 0.
- Document title remained the site name.

### Cart, checkout, offers, services

These were opened at 1440×900. Overflow was not remeasured after navigation. Content notes:

- Cart and checkout empty states are a heading, a short sentence, and a link. No multi-column layout was on screen because the cart had no lines.
- Offers and services use the same page header and card patterns as the shop. Their first paint is an empty or error heading. See FALSE-EMPTY in the audit.

### Admin

`/admin` at 1440×900 did not render an admin shell. The document was empty, then the router landed on `/auth/login`.

## Checks requested, and what was verified

| Check | 320 | 375 | 1440 |
|---|---|---|---|
| Horizontal scrolling | None on home and product | None on product | None on home, shop, login |
| Vertical overflow | Hero pills escape the hero | Buy bar overlaps nav by 1px | Not a problem on the measured sections |
| Clipped elements | Add to cart at x=−7 | Add to cart 22px wide | Hero capture was cropped by the screenshot tool, not by the page (`h1` box fit) |
| Overlap | Contact buttons on trust cards | Buy bar and bottom nav | Filter button fixed over the desktop page |
| Broken grid | Not measured on shop | Not measured on shop | Shop is 4 columns of ~250px. Homepage carousel shows a partial next card, which is expected for a carousel |
| Card size | Not measured | Not measured | 250×448 shop, 307×483 homepage carousel |
| Image aspect | Not measured | Gallery is square, 343px | Square wells. Category tile is portrait from a landscape file |
| Text wrap | Hero headline fits | Product title fits | Flavor chip with a long Arabic name wraps to its own row |
| Buttons | Hero CTAs 50px tall. Add to cart unusable | Add to cart unusable | PDP Add to cart 44px tall. Shop card actions were in the tree |
| Inputs | Quantity 138px wide | Quantity 138px wide | Login inputs 43px tall |
| Navigation | Bottom nav, 5×64 | Header icons 40px | Desktop nav flex, bottom nav none |
| Modals and dropdowns | Not opened | Not opened | Not opened |
| Sticky | Buy bar fixed | Buy bar fixed, overlaps nav | Shop filters `ui-sticky-filters` |
| Footer | Present in the tree on home | Not separately measured | Present. Second newsletter form |
| Whitespace | Hero is short (230px) and the pills do not sit inside it | Gallery uses the content width | Hero is 560px of flat green |
| Touch | Bottom nav 56px. Header icons 40px | Several controls under 44px | Not a touch layout |

## Do not cite these captures

| File | Why |
|---|---|
| `pdp-mobile-375.png` | Image showed a narrow column and a large empty region. DOM width of the gallery was 343 of 375. |
| `home-mobile-320.png` | Same canvas mismatch. DOM measurements above are the record. |
| `login-desktop-1440.png` | The saved image still showed the homepage hero, while the DOM was the login form at 1440. |
| `home-desktop-1440-top.png` | Useful for the header and hero colors, but the tool cropped the 1440 frame. |

## What to run next

For each unmeasured viewport, set the device metrics, reload `/`, `/shop`, and `/product/nxt-nutrition-beef-isolate`, and record:

1. `scrollWidth - clientWidth`.
2. Bounding box of `.product-mobile-actions` and its buttons, if the bar is `display` other than `none`.
3. Computed `display` of `.shop-mobile-filter-btn`, `.app-bottom-nav`, and `.app-desktop-nav`.
4. Any fixed contact button whose box intersects a card or a primary button.
5. A screenshot only after confirming `innerWidth` equals the requested width and the image is that wide.

Do not mark 430, 768, 1024, 1366, 1920, or 2560 as passed.
