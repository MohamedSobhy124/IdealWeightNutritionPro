# Ideal Weight Nutrition – UI/UX Modernization Plan

The plan is based on the current `frontend/` codebase as of Nov 20, 2026.

**Stack:** Angular 19 (standalone components, signals, `@if`/`@for` control flow, SSR) · Angular Material/CDK 19 · Tailwind 3 (preflight disabled) · Chart.js / ng2-charts · SignalR · Quill. Bilingual EN/AR with RTL, plus a light/dark theme toggle.

**Starting point:** the app is already partly modernized:

- 30 shared `ui-*` components in `shared/ui`.
- 21 admin lists built on `ui-data-table`.
- A customer bottom nav and drawer.
- An admin sidebar shell.
- A 3-step checkout.

This plan finishes and hardens that work. It is not a rewrite.

---

## 1. Audit of the current UI

### 1.1 Screens

**Customer (mobile-first), about 32 screens**

| Area | Screens |
|---|---|
| Shopping | Home, Shop, Product detail (+ reviews, quick view), Cart, Checkout, Order confirmation, Order tracking, Wishlist |
| Promotions | Offers, Flash sales list/detail, Combos list/detail |
| Services | Service list/detail/checkout/confirmation |
| Content | Blog list/detail, Static pages (about, privacy, terms, shipping, return policy, help) |
| Auth | Login, Register, Forgot password, Reset password, OAuth callback |
| Account | Account dashboard, My orders, My subscriptions, My returns, Request return, Return detail, Newsletter unsubscribe |

**Admin (desktop-first), about 33 screens under `/admin`**

| Group | Screens | Access |
|---|---|---|
| Overview | Dashboard | Staff |
| Sales | Orders + detail, Returns + detail, Stock notifications, Reviews | Staff |
| Catalogue | Products list/create/detail, Categories, Brands | Admin only |
| Marketing | Promo codes, Flash sales, Combo offers, Newsletter | Admin only |
| Services | Services, Service offers, Service purchases + detail | Admin only |
| Content | Blog, Video banner | Admin only |
| Settings | Cities + areas, Companies, Users | Admin only |

**Roles and guards**

- `staffGuard` lets Admin and Employee into `/admin`.
- `adminOnlyGuard` sends Employees to `/admin/orders`.
- `authGuard` protects account pages and the wishlist.

These guards stay unchanged.

### 1.2 Problems found

1. **Two styling systems run at once.**
   - Tailwind runs alongside about 1,340 lines of legacy global SCSS (`_components.scss` 678 lines, `_layout.scss` 253 lines).
   - Several pages carry heavy CSS of their own: home 263 lines, product detail 241, `app.component` 117, contact-floating 115, static page 98, flash-sale banner 77, checkout 69, shop 63.
2. **Design tokens are duplicated by hand** between `_tokens.scss` and `tailwind.config.js`. The success, warning, danger, and info colors exist only in Tailwind, so they have no dark-mode values.
3. **Dark mode breaks in places.** `bg-white` is hard-coded in `ui-modal` and in the admin shell's dropdowns.
4. **The same markup is copy-pasted.** The bottom-nav item, with a long class string, is repeated 5 times in `app.component.html`, and the count badge 4 times.
5. **The customer user menu shows an "Admin" link to every logged-in user.** The guard blocks access, but it is still a role leak in the UI.
6. **There are 19 native `confirm()`/`alert()` calls,** including the irreversible "delete account" action. There is no toast service, so success and error feedback is inconsistent.
7. **No admin form has unsaved-changes protection.** There is no `canDeactivate` anywhere.
8. **Admin shell gaps:**
   - The breadcrumb only goes two levels deep (`/admin/orders/123` shows "Orders").
   - Global search only matches menu labels.
   - The role check in `ngOnInit` duplicates the guard.
   - The drawer has no focus trap.
   - The mobile drawer's slide transform in RTL still needs checking.
9. **`ui-data-table` limits:**
   - Paging and sorting are client-side only, and the page size is fixed.
   - There is no column visibility and no standard filter slot.
   - The sticky header sits inside an `overflow-x-auto` wrapper, so it cannot stick to the page.
   - Mobile cards are generic label/value lists with no title, subtitle, or primary action.
10. **`ui-modal` gaps:** no focus trap, focus is not returned on close, the body still scrolls behind it, and there is no side-drawer variant.
11. **Dashboard issues:**
    - The page subtitle reuses the `recentOrders` text key.
    - "Revenue today" is drawn with `trendDirection="up"` hard-coded, which shows a trend that is never calculated.
12. **Tests:** only `app.component.spec.ts` exists.

---

## 2. Design system

### 2.1 Tokens (single source in `_tokens.scss`, consumed by Tailwind)

- **Brand:** emerald 50–900 (primary 600 `#059669`, hover 700).
- **Neutral:** slate "ink" 50–900. The scale is inverted in dark mode.
- **Semantic colors:** success, warning, danger, and info, each with `bg`/`fg`/`border` values for light and dark.
- **Surfaces:** `surface`, `surface-muted`, `surface-raised`, `surface-overlay`, `border`, `border-strong`, `text`, `text-muted`.
- **Typography:** Inter for Latin text and IBM Plex Sans Arabic for Arabic. Sizes are 12 / 13 / 14 / 16 / 18 / 20 / 24 / 30 / 36, with line heights from 1.2 (headings) to 1.5 (body).
- **Spacing:** a 4px base scale (4, 8, 12, 16, 20, 24, 32, 40, 48, 64).
- **Radius:** sm 8, md 10, lg 14, xl 20, full.
- **Shadows:** xs, sm, card, elevated, overlay, focus ring.
- **Z-index layers:** header 30, drawer 40/50, modal 100, toast 110.
- **Motion:** 150 / 200 / 300 ms, `cubic-bezier(0.32, 0.72, 0, 1)`, with a `prefers-reduced-motion` reset.
- **Breakpoints:** `xs` 376 · `sm` 481 · `md` 768 · `lg` 1024 · `xl` 1440 · `2xl` 1920.

### 2.2 Component styles (`@layer components`)

- **Buttons:** primary, secondary, ghost, danger, icon, and link, in sm / md / lg sizes, with loading and disabled states.
- **Inputs:** text, select, textarea, checkbox, radio, switch, and OTP, with error, hint, and required states.
- **Surfaces:** card, stat card, badge (status variants), chip, tabs, segmented control, stepper, timeline.
- **Overlays:** modal (centered on desktop, bottom sheet on mobile), side drawer, confirm dialog, toast, dropdown menu, tooltip.
- **Data:** data table (desktop) and structured cards (mobile), pagination, empty state, skeleton.

---

## 3. Implementation steps

Each step is a separate, reviewable change.

### Step 0 – Baseline (no UI change)
- Run the production build and the SSR build, and record bundle sizes.
- Take screenshots at 320 / 375 / 768 / 1024 / 1440 / 1920 px wide, in both LTR and RTL.

### Step 1 – One source of design tokens
- Expand `_tokens.scss` as described in section 2.1.
- Point `tailwind.config.js` at the CSS variables (`rgb(var(--brand-600) / <alpha-value>)`).
- Add the global `:focus-visible` style and the reduced-motion reset.

### Step 2 – Retire the legacy CSS
- Move the `_components.scss` and `_layout.scss` rules that are still used into Tailwind component classes, and delete the dead ones.
- Shrink the page CSS for home, product detail, checkout, shop, static page, and the flash-sale banner.
- Done when no hard-coded colors remain and `!important` is close to zero.

### Step 3 – Shared building blocks (`shared/ui`)
- `UiToastService` + `ui-toast-host`, using `aria-live`, with queued, success, error, and undo toasts.
- `UiConfirmService` + `ui-confirm-dialog`, returning a promise that resolves to true or false. It replaces all 19 `confirm()`/`alert()` calls; destructive actions get a danger style.
- `ui-modal` upgrades: CDK `FocusTrap`, focus returned on close, body scroll lock, theme-aware surfaces, and a `side="end"` drawer variant.
- `ui-drawer`: a side panel for admin quick-edit.
- `unsavedChangesGuard` (`CanDeactivateFn`) + a `HasUnsavedChanges` interface + a `beforeunload` hook.
- `ui-data-table` v2. Existing inputs stay compatible.
  - Column visibility menu, saved per table in `localStorage`.
  - Page-size selector.
  - A `[table-filters]` slot with filter chips.
  - A sticky header.
  - A row-click output.
  - Mobile cards with `primary`, `secondary`, `meta`, and `actions` roles.
  - An optional server mode (`totalCount`, `pageChange`, `sortChange`).
- New shared components:
  - `ui-breadcrumbs`, driven by route `data.breadcrumb`.
  - `ui-segmented`, for status tabs.
  - `ui-stepper`, pulled out of checkout.
  - `ui-bottom-sheet-select`.
  - `ui-nav-item`.

### Step 4 – Customer shell (mobile-first)
- Split `app.component.html` into `customer-header`, `customer-bottom-nav`, `customer-drawer`, and `customer-footer`.
- Use a compact 56px mobile header, with a back button driven by route data (`showBack`).
- Show the Admin entry only when the user has the Admin or Employee role.
- Add an account shell (`/account` parent layout):
  - On mobile, a hub list with segmented sub-navigation.
  - On desktop, a left sub-navigation.
  - URLs and guards stay unchanged.
- Pin key actions to the bottom on mobile (Add to cart, Checkout, Continue/Pay), respecting the device safe-area insets.

### Step 5 – Customer screens (highest traffic first)
1. **Home, Shop, Product detail.**
   - Home: horizontal rails with scroll snap.
   - Shop: filters in a bottom sheet on mobile, segmented sort, a grid that adapts from 2 to 5 columns.
   - Product detail: gallery swipe, sticky buy bar, reviews in a bottom sheet.
2. **Cart, Checkout, Order confirmation.**
   - Cart: remove with undo.
   - Checkout: shared stepper, mobile keyboard `inputmode`/`autocomplete`, inline validation.
3. **Tracking timeline, My orders as cards, Return request as steps, Return detail.**
4. **Services:** list, detail, checkout, and confirmation get the same patterns.
5. **Promotions:** offers, flash sales with countdown, combos.
6. **Content and auth:** blog and static pages, plus single-column auth forms with password visibility toggles and OTP input.
7. **Account:** the delete-account action moves into a danger dialog with typed confirmation.

### Step 6 – Admin shell
- Breadcrumbs with multiple levels, and a page header with title, description, and an actions slot.
- Collapsible sidebar, fixed for RTL. It becomes a focus-trapped drawer below 1024px.
- A ⌘K command palette covering menu items and quick actions ("New product", "New promo", and so on).
- Fix dark mode in the dropdowns, remove the duplicate role check, and add a density toggle.
- Full-width content, capped at `max-w-[1600px]` on ultra-wide screens.

### Step 7 – Admin dashboard (real data only)
- Keep the existing KPIs from `AdminDashboardService`, and remove the fake trend.
- Group the page into three areas:
  - **Needs attention:** pending orders, pending returns, low/out of stock, pending service purchases.
  - **Sales overview:** the existing charts.
  - **Recent orders.**
- Show each card according to role.
- Add trend charts only if the API already returns time series.

### Step 8 – Admin management screens
- **Lists (all 21):**
  - Standard toolbar: search, filter chips, status tabs, column menu, export.
  - Bulk actions only where an endpoint already exists.
  - Row click opens the record, and deletes go through the confirm service.
- **Simple lookup entities** (categories, brands, cities, areas, companies): create and edit in a side drawer.
- **Complex entities** (products, services, blog, promo codes, flash sales, combos, service offers):
  - Dedicated pages with grouped sections and a sticky save/cancel bar.
  - Required-field markers and hints.
  - The unsaved-changes guard.
  - Two columns at 1440px and wider.
- **Detail pages** (order, return, service purchase): a summary header with status and actions, a timeline, and side cards for customer, payment, and delivery.

### Step 9 – Accessibility and performance
- Contrast checks for both themes.
- 44px minimum touch targets on customer pages.
- `aria-label` on every icon-only button.
- Keyboard checks for menus, tables, dialogs, and the command palette.
- `NgOptimizedImage` with fixed dimensions to avoid layout shift.
- OnPush change detection on all page components.

### Step 10 – Verification
- Production build and SSR build.
- Specs for the confirm and toast services, the unsaved-changes guard, `ui-data-table` (sort, filter, column visibility, pagination), breadcrumbs, and role guards.
- Repeat the screenshot matrix at all 7 sizes in both LTR and RTL, checking for horizontal overflow.

---

## 3a. Progress

| Step | Status |
|---|---|
| 1 – Tokens | Done. RGB channel tokens with light/dark values; Tailwind `brand`/`ink`/semantic/`surface`/`scrim` colors follow the theme; breakpoints xs–3xl; global `:focus-visible`. |
| 2 – Legacy CSS | Primary buttons use the brand tokens. Shop filters, cart chips, and the rich-text editor follow the surface token. Page-specific brand colors (home hero, contact shortcuts, footer tiles) stay as their own colors. |
| 3 – Shared building blocks | Done. Includes toast, confirm, modal, drawer, unsaved-changes guard, data table, breadcrumbs, segmented control, stepper, and `ui-sheet-select` (native select from 1024px, bottom sheet below that) on shop filters, checkout city and area, and return item condition. |
| 4 – Customer shell | Storefront chrome split into header, drawer, footer, and bottom nav. Account pages share `account-shell`. Mobile action bars (add to cart, checkout, pay, subscribe, returns) sit above the bottom nav and the home-indicator inset, and the page content clears them. |
| 5 – Customer screens | Through sign-in. Delete account sits in its own danger area and opens a dialog that stays closed until the customer types the confirmation word. The security card, including password and export, is bilingual. A mismatched word is explained after the field is left. |
| 6 – Admin shell | Sidebar collapses and, below 1024px, becomes a focus-trapped drawer that closes off-screen in both directions. Ctrl/Cmd+K opens a command palette of the pages the signed-in role can open, including create links for admins. A density toggle tightens admin table rows and is remembered. Main content is capped at 1600px. The shell no longer repeats the staff role check that the route guard already performs. |
| 7 – Admin dashboard | Grouped into Needs attention, Sales overview, and Recent orders using the dashboard API counts. Catalogue, marketing, and service cards, and the create shortcuts, show for admins only. Recent order statuses use the shared status labels. No trend chart, because the API has no time series. |
| 8 – Admin screens | Lookup records open in a side drawer. Detail pages use a status summary, a timeline, and side cards. Product, service, blog, promo, flash-sale, combo, and service-offer forms keep their sections and unsaved-changes guard, sit in two columns from 1440px, and keep Save and Cancel stuck to the bottom of the page. Lists already open their record on row click and use the shared table toolbar. |
| 9 – Accessibility | Buttons, icon buttons, and segmented options are at least 44px tall below 1024px. Icon buttons in the shells already have names. Motion still collapses under `prefers-reduced-motion`. OnPush on customer pages/layout, blog, newsletter, `app.component`, and all admin `*.component.ts`. `NgOptimizedImage` on product/brand/category/article/service cards with a passthrough `IMAGE_LOADER` in `app.config`. |
| 10 – Verification | `status-label.spec.ts` covers known statuses, unknown statuses, a cancelled order, a shipped order, and a rejected return. A production build still has to be run locally with `cd frontend; npm run build`. |
| Quick fixes | Done: dark-mode `bg-white` leaks, staff-only Admin link, RTL admin drawer, fake dashboard trend and wrong subtitle. |

## 4. What stays fixed

- No changes to `core/services`, models, interceptors, guard logic, route paths, or API contracts.
- No new UI library: Material/CDK (`FocusTrap`, `Overlay`, `BreakpointObserver`) and Tailwind cover everything planned.
- All new text goes into `ui-text.ts` and `admin-ui-text.ts` in both English and Arabic.

## 5. Risks and recommendations

- **Server-side paging:** most admin lists load all records and page on the client. That is fine at today's data sizes. True server paging needs backend changes, so it is a recommendation only.
- **Account shell:** a new parent layout under `/account` keeps every URL the same, but it is a structural routing change.
- **Dashboard trend charts** need time-series endpoints; this is a backend recommendation.
