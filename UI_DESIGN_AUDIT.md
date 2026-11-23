# Ideal Weight Nutrition – UI Design Audit

Date: 20 Nov 2026. Scope: every customer and admin screen, plus shared chrome. Business logic, routes, and API contracts stay fixed.

## 1. Proposed palette (warm sage)

| Role | HEX | RGB channels |
|---|---|---|
| Brand 600 (primary) | `#0F6E56` | `15 110 86` |
| Brand 700 (hover) | `#0C5A47` | `12 90 71` |
| Brand 500 | `#148F6F` | `20 143 111` |
| Brand soft / 50 | `#E7F5F0` | `231 245 240` |
| On primary | `#FFFFFF` | `255 255 255` |
| Brass accent | `#8A6A2F` | `138 106 47` |
| Brass soft | `#F5EFE3` | `245 239 227` |
| Text | `#1C1917` | `28 25 23` |
| Text muted | `#78716C` | `120 113 108` |
| Border | `#E7E2DA` | `231 226 218` |
| Surface | `#FFFCF8` | `255 252 248` |
| Surface muted | `#F6F3EE` | `246 243 238` |
| Surface raised | `#FFFFFF` | `255 255 255` |
| Success | `#0F6E56` / soft `#E7F5F0` | same as brand |
| Warning | `#B45309` / soft `#FEF6E8` | `180 83 9` / `254 246 232` |
| Danger | `#B42318` / soft `#FEF3F2` | `180 35 24` / `254 243 242` |
| Info | `#175CD3` / soft `#EFF4FF` | `23 92 211` / `239 244 255` |
| Dark surface | `#1C1917` | `28 25 23` |
| Dark surface raised | `#292524` | `41 37 36` |
| Dark text | `#FAFAF9` | `250 250 249` |
| Dark brand text | `#7DCEA0` | `125 206 160` |
| Phone (contact) | `#175CD3` | info |
| WhatsApp (contact) | `#1FAF5B` | `31 175 91` |

Token map: all values live in `frontend/src/styles/_tokens.scss` as `--rgb-*`. Tailwind `brand` / `ink` / semantic / `surface` already read those channels.

## 2. Shared chrome

| Surface | Issues | Fix |
|---|---|---|
| Design tokens | Stock emerald `#059669`, cool slate ink, cool white surfaces | Warm sage + stone neutrals in light and dark |
| Primary button | Gradient still shadows with old emerald RGB; soft glow hard-coded | Brand channel fills and brand soft glow |
| Auth brand panel | Three-stop emerald gradient `#047857 → #10b981` | Solid sage / soft brand panel |
| Modal / drawer / toast / confirm | Mostly on tokens; close icon contrast depends on ink | Follow new ink automatically |
| Data table | Fine; density already token-driven | No structural change |
| Customer header / drawer / bottom nav | Bottom-nav badge `#fff` on brand is fine | Keep white on brand |
| Customer footer payment badges | Six unrelated pastel gradients | Soft brand / info / danger / muted fills from tokens |
| Contact FAB | Hard `#2563eb`, `#25d366`, `#111827` | Named contact tokens (phone, WhatsApp, panel) |
| Admin shell | Surfaces already token-based | Follows new palette |
| Charts (dashboard) | Hex series amber/blue/violet/emerald/red | Warning, info, brand, danger, muted series from tokens |

## 3. Customer screens

| Screen | Issues | Improvement |
|---|---|---|
| Home | Gold CTA `#d4af37`, indigo glow, animated multi-stop slate gradient, amber progress | Still brass CTA; calm brand-tinted hero; no rainbow animation |
| Shop | Slate RGB shadows and borders in page CSS | Token borders and shadows |
| Product detail | Amber/indigo “premium” locals, gold buy CTA, cool slate surfaces | Brass accent for sale emphasis; sage primary actions; token surfaces |
| Product reviews | Uses shared modal / form | Follows tokens |
| Cart | Emerald-tint shadow `5 150 105`, slate borders | Brand soft shadow + token border |
| Checkout | Amber/indigo panel accents, cool slate cards | Brass soft accent strip; token cards |
| Order confirmation / tracking | Shared timeline + badges | Follows tokens |
| Wishlist | Shared product cards | Follows tokens |
| Offers / flash sales / combos | Countdown inverse uses white alpha | Keep; brand soft elsewhere |
| Services list / detail / checkout / confirmation | Shared cards and stepper | Follows tokens |
| Blog list / detail | Shared article cards | Follows tokens |
| Static pages (about, privacy, terms, shipping, return policy, help) | Already on `ui-card` / prose | Follows tokens |
| Auth login / register / forgot / reset / OAuth | Auth panel gradient | Sage panel |
| Account dashboard / orders / subscriptions / returns | Shared shells and badges | Follows tokens |
| Request return / return detail | Sheet select + stepper | Follows tokens |
| Newsletter unsubscribe | Simple form | Follows tokens |

## 4. Admin screens

| Screen | Issues | Improvement |
|---|---|---|
| Dashboard | Chart colors hard-coded; otherwise token KPIs | Token chart series |
| Orders / returns / stock / reviews / newsletter lists | Shared table | Follows tokens |
| Order / return / service-purchase detail | Status badges + timelines | Follows tokens |
| Products list / create / detail | Form bar + sections | Follows tokens; no hex in templates |
| Categories / brands / cities / areas / companies | Drawer forms | Follows tokens |
| Promo / flash / combo / service / blog / service-offer forms | Sticky bar + cards | Follows tokens |
| Service purchases / users / video banner | Shared chrome | Follows tokens |

## 5. Visual details to enforce

- Radius: `--radius-sm` … `--radius-xl` only.
- Shadows: `--shadow-card`, `--shadow-elevated`, `--shadow-overlay`, brand soft glow via `rgb(var(--rgb-brand-600) / α)`.
- Brass only for hero CTA and sale emphasis. No indigo pairing.
- Contact phone and WhatsApp keep recognizable hues as named tokens.
- Icon buttons and segmented options stay ≥ 44px below 1024px.
- WCAG AA: sage `#0F6E56` on warm white, white on sage, muted stone on warm white, dark brand text `#7DCEA0` on dark surface.

## 6. Implementation order

1. `_tokens.scss` light + dark channels (and brass / contact tokens).
2. Shared `_components.scss` buttons, auth panel, focus rings; footer badges; contact FAB; dashboard charts.
3. Home, product detail, checkout, cart, shop feature CSS.
4. Contrast pass on sage, brass, and dark text.

## 7. Out of scope

- New UI libraries.
- Invented metrics or copy.
- Changing payment-provider logo art (only badge chrome colors).
- Rewriting layouts that already use `ui-*`.

## 8. Contrast notes (WCAG AA)

| Pair | Approx ratio | Result |
|---|---|---|
| Sage `#0F6E56` on warm white `#FFFCF8` | ~5.8:1 | Pass |
| White on sage `#0F6E56` | ~5.8:1 | Pass |
| Muted stone `#78716C` on `#FFFCF8` | ~4.6:1 | Pass (normal text) |
| White on brass `#8A6A2F` | ~5.0:1 | Pass |
| Dark brand text `#7DCEA0` on `#1C1917` | ~9:1 | Pass |
| Danger `#B42318` on `#FFFCF8` | ~5.5:1 | Pass |

## 9. Implementation status

Tokens, shared chrome, feature CSS, and contact/footer/chart colors updated to this palette (Nov 2026).
