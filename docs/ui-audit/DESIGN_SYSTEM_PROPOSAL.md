# Design system proposal — Ideal Weight Nutrition

A premium nutrition store: forest for trust and action, cream so photographs read clearly, lime only as a small spark, orange only for savings. This replaces the current token values in [frontend/src/styles/_tokens.scss](../../frontend/src/styles/_tokens.scss). Tailwind already reads those channels in [frontend/tailwind.config.js](../../frontend/tailwind.config.js), so components that use `bg-brand-solid`, `text-citrus-fg`, and `bg-surface` pick the new values up without a second palette.

Do not paint heroes, page backgrounds, or table rows in lime or orange.

## Color

Channels in `_tokens.scss` are space-separated RGB. Hex is the source of truth below. Alpha uses the existing `rgb(var(--rgb-*) / <alpha>)` pattern.

### Brand and accent

| Role | Token | Hex | RGB channel | Use |
|---|---|---|---|---|
| Primary | `--rgb-brand-solid` | `#0B5F45` | `11 95 69` | Primary buttons, links, selected filter, active nav, wordmark. |
| Primary hover | `--rgb-brand-solid-hover` | `#084C37` | `8 76 55` | Hover and pressed primary. |
| Primary soft | `--rgb-brand-50` | `#E5F4EC` | `229 244 236` | Selected chip fill, active nav pill, success soft. |
| Brand 700 | `--rgb-brand-700` | `#0B5F45` | `11 95 69` | Same as primary so older `bg-brand-700` buttons match. |
| Brand 900 | `--rgb-brand-900` | `#12352B` | `18 53 43` | Optional deep forest scrim on hero photos. Keep the current 900 if a darker text green is required on pale fills. |
| Secondary lime | `--rgb-lime` | `#C6E34A` | `198 227 74` | 4px rules, icon wells on forest, progress, hero eyebrow on a forest scrim. |
| Lime foreground | `--rgb-lime-fg` | `#1C1917` | `28 25 23` | Text on lime. Lime is never the text color on cream. |
| Lime deep | `--rgb-lime-deep` | `#5C7A16` | `92 122 22` | Small lime-colored icons on cream, if a green icon is required. Do not use bright lime as a 14px label on white. |
| Promo | `--rgb-citrus` | `#C2410C` | `194 65 12` | Sale price text. |
| Promo soft | `--rgb-citrus-soft` | `#FFF4EC` | `255 244 236` | Discount badge background. |
| Promo ink | `--rgb-citrus-fg` | `#9A3412` | `154 52 18` | Badge text on promo soft. |

### Surfaces and text

| Role | Token | Hex | RGB channel | Use |
|---|---|---|---|---|
| Background | `--rgb-ink-50` / page | `#F6F3EC` | `246 243 236` | `body` background. |
| Surface | `--rgb-surface` | `#FFFFFF` | `255 255 255` | Cards, header, buy box, dialogs. |
| Surface muted | `--rgb-surface-muted` | `#F3EFE6` | `243 239 230` | Inset panels, table header, accordion body. |
| Image stage | `--rgb-stage` | `#F7F4EF` | `247 244 239` | Behind product photos. |
| Headings | `--rgb-ink-900` | `#1C1917` | `28 25 23` | `h1`–`h3`, prices that are not on sale. |
| Body | `--rgb-ink-700` | `#3F3A36` | `63 58 54` | Paragraphs, form labels. |
| Muted text | `--rgb-ink-600` | `#5E584F` | `94 88 79` | Meta, breadcrumbs, struck-price companion. Must stay at least 4.5:1 on white and on `#F6F3EC`. |
| Disabled decoration | `--rgb-ink-400` | `#A8A299` | `168 162 153` | Borders and placeholders only. Not for sentences or prices. |
| Border | `--rgb-ink-200` | `#E6DFD4` | `230 223 212` | Card and input borders. |
| Border strong | `--rgb-ink-300` | `#D5CDBF` | `213 205 191` | Hover borders, segmented controls. |

### Status

| Role | Hex | Soft hex | Use |
|---|---|---|---|
| Success | `#0B5F45` | `#E5F4EC` | In stock, verified purchase, form success. Same hue as primary so success does not introduce a second green. |
| Warning | `#B45309` | `#FFF7ED` | Low stock, expiring promo. Not the sale price. |
| Error | `#B42318` | `#FEF3F2` | Form errors, payment failure, destructive buttons. |
| Info | `#175CD3` | `#EFF4FF` | Neutral notices. Keep the phone FAB on this blue (`--rgb-contact-phone`). |

Discount and promotional elements use the promo row, not error red. A percent badge is promo-soft background with promo-ink text. The sale price is promo `#C2410C`. The old price is muted `#5E584F` with a line through it.

### Where each color is allowed

| Surface | Background | Text | Accent |
|---|---|---|---|
| Header | White at 92% opacity | Charcoal wordmark, muted nav | Forest on the active item, soft forest pill |
| Hero | Product photo plus a forest scrim (`#12352B` at about 55% on the text side) | White | Lime eyebrow. One white or cream primary button with forest text |
| Product card | White card, stage behind the photo | Charcoal title, muted brand | Promo price and badge only when `listPrice > price` |
| Buy box | White | Charcoal title, promo or charcoal price | Forest Add to cart. Outline for Buy now |
| Shop sidebar | White | Charcoal labels | Forest primary on Apply |
| Footer | `#12352B` or charcoal `#1C1917` | White and white at 80% | Lime only on the small section mark |
| Admin tables | White and muted surface | Charcoal | Forest for primary actions, error for delete, promo only for discount columns |
| Toasts | White | Charcoal | 4px forest, warning, or error edge |

Contact buttons stay phone `#175CD3` and WhatsApp `#1FAF5B`. They are utilities, not brand fills.

### Contrast rules

- White on `#0B5F45` for primary buttons.
- `#1C1917` on `#C6E34A` whenever lime has a label. Bright lime fails as text on white, so it is not a text color there.
- `#9A3412` on `#FFF4EC` for badges. `#C2410C` on white for the sale price.
- `#5E584F` on `#FFFFFF` and on `#F6F3EC` for muted text. Stop using `#A8A299` for text.
- Focus: `box-shadow: 0 0 0 3px rgb(11 95 69 / 0.35)` on buttons, inputs, and chips. Do not remove the outline without this ring.

### Dark theme

Keep the `.dark` block in `_tokens.scss`. Map the same roles:

- Surface `#1C1917`, muted `#141210`, raised `#24211E`.
- Text `#F6F3EC`, muted text `#D5CDBF`.
- Primary button stays `#0B5F45` with white text. Do not invert the button to lime.
- Promo text lightens to `#FDBA74` on the dark surface so the sale price still clears 4.5:1.
- Lime highlights stay small. On dark, use `#C6E34A` for the 4px mark and `#1C1917` text if a lime chip appears.

Dark mode was not visually tested in this audit. Retune tokens, then check the homepage, a product card, and the product buy box before calling dark mode done.

## Typography

| Role | Family | Size | Weight | Line height |
|---|---|---|---|---|
| Display, Arabic | IBM Plex Sans Arabic, Tajawal | 2rem mobile, 3.25rem desktop hero | 800 | 1.15 |
| Display, Latin | Outfit, Manrope | same | 800 | 1.1 |
| Page title | The active locale family | 1.75rem | 800 | 1.2 |
| Section title | same | 1.375rem | 700 | 1.3 |
| Product title on a card | same | 0.95rem | 600 | 1.4 |
| Body | same | 1rem | 400 | 1.6 |
| Meta, badges | same | 0.75rem | 700 | 1.3 |
| Price | same, `tabular-nums` | 1.15rem on cards, 1.8rem in the buy box | 800 | 1.1 |

One `h1` per page. Card titles are `h3` or `p`. Letter-spacing stays at 0 on Arabic. Latin display can use `-0.03em`.

The current hero headline at 60px on a flat green panel should come down once a photograph is in the hero. 44px on desktop and 28px on a 320px screen is enough.

## Spacing, radius, elevation

Keep the 4px scale already in the tokens.

| Token | Value |
|---|---|
| `--space-xs` | 4px |
| `--space-sm` | 8px |
| `--space-md` | 12px |
| `--space-lg` | 16px |
| `--space-xl` | 24px |
| `--space-2xl` | 32px |
| `--space-3xl` | 48px |
| `--radius-sm` | 8px |
| `--radius-md` | 12px |
| `--radius-lg` | 16px |
| `--radius-xl` | 20px |
| `--radius-full` | 999px |

| Elevation | Value | Use |
|---|---|---|
| Card | `0 1px 2px rgb(28 25 23 / 0.04), 0 8px 24px rgb(28 25 23 / 0.04)` | Product cards at rest |
| Raised | `0 12px 32px rgb(28 25 23 / 0.08)` | Buy box, dropdown, sticky filter |
| Overlay | `0 24px 48px rgb(28 25 23 / 0.18)` | Modal |

Page content max width stays the Tailwind container: 1440px. Ultra-wide layouts center that container. They do not stretch product cards edge to edge.

## Components

### Buttons

- Primary: background `#0B5F45`, white text, radius 12px, min-height 48px, horizontal padding 20px. Hover `#084C37`.
- Secondary: white, border `#D5CDBF`, charcoal text. Hover muted surface.
- Ghost on photos: white at 92% with charcoal text, not white at 8% on green.
- Danger: error soft background, error text. Used for remove and delete.
- Disabled: 50% opacity, no shadow, `cursor: not-allowed`.
- One solid primary button in any cluster. Buy now is secondary. Add to cart is primary.

### Inputs

- Height 48px, radius 12px, border `#E6DFD4`, background white, 16px type (16px avoids iOS zoom).
- Focus ring as above.
- Error border `#B42318` and a sentence under the field in error text. Placeholder `#A8A299` is acceptable because it is not the value.

### Select and chips

- Closed select matches the input.
- Flavor and filter chips: min-height 44px, padding 8px 14px, radius full, border `#D5CDBF`.
- Selected: background `#E5F4EC`, text `#0B5F45`, border `#0B5F45`.
- Percent and currency chips that start with `-` or `+`: `direction: ltr; unicode-bidi: isolate`.

### Cards

- White, radius 16px, border `#E6DFD4`, card shadow.
- Image stage `#F7F4EF`, `object-contain`, padding 16px. No hover scale when the natural image is under 200px.
- Badge top-start. Wishlist icon button 44×44, top-end.
- Price row under the title. Sale price `#C2410C`. List price `#5E584F`, struck.
- Card action is one button. Quick view can be a text button on `sm` and up.

### Tables (admin)

- Header background `#F3EFE6`, 14px weight 600, sticky on long lists.
- Row height at least 48px. Numeric columns `tabular-nums` and end-aligned in LTR, start-aligned consistently in RTL (use logical `text-end`).
- Status pills use success, warning, and error soft pairs. Do not color the whole row.

### Navigation

- Desktop: text links, active item forest on `#E5F4EC`.
- Mobile: bottom nav, 56px plus safe area, forest on the active icon, muted `#5E584F` on the rest.
- Drawers: white surface, scrim `rgb(18 53 43 / 0.45)`.

### Feedback

- Toast: white, radius 12px, overlay shadow, 4px edge in success, warning, or error. Auto-dismiss after 4s. Errors stay until dismissed.
- Empty state: one sentence, one action. Do not show it while `loading()` is true.
- Skeleton: muted surface blocks, no error icon.
- Loading on a button: keep the label width, set `aria-busy`, disable the button.

## Motion

- Durations stay 150 / 200 / 300ms, ease-out.
- `@media (prefers-reduced-motion: reduce)` disables hero autoplay, ticker translation, card image scale, and the contact button pulse.

## Implementation note

Change token values first. Then delete hardcoded `#f0f0f0`, `#fff` backgrounds on steppers, and `#240` grays in:

- [frontend/src/app/features/catalogue/product-detail.component.css](../../frontend/src/app/features/catalogue/product-detail.component.css)
- [frontend/src/app/features/catalogue/shop-page.component.css](../../frontend/src/app/features/catalogue/shop-page.component.css)
- [frontend/src/styles/_components.scss](../../frontend/src/styles/_components.scss)
- [frontend/src/styles/_buttons.scss](../../frontend/src/styles/_buttons.scss)

Leave API models, prices, and copy alone.
