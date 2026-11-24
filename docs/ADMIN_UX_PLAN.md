# Admin UX Redesign Plan

Date: 24 Nov 2026. Scope: all 32 admin screens under `frontend/src/app/features/admin`, plus the shared `ui-*` kit and admin styles. Business logic, routes and API contracts stay the same. This builds on `UI_DESIGN_AUDIT.md` (palette and tokens), which is about colour. This plan is about layout, flow and how clear each screen is to use.

## 1. Where we are

The base is good. The problems are inconsistency and a few rough flows, not missing infrastructure.

**Already in place (keep):**
- Admin shell with grouped sidebar, collapse, breadcrumbs, command palette (Ctrl+K), density toggle, theme, AR/EN, notifications.
- Shared kit: `ui-page-header` (32/32 screens), `ui-data-table` (20 lists, with server paging, row selection and saved columns), `ui-badge`, `ui-card`, `ui-form-field`, `ui-modal`, `ui-timeline`, `ui-toast`, `ui-confirm`.
- `.admin-form` grid with a sticky `.admin-form-bar`; `unsavedChangesGuard` on 13 form routes.

**What gets in the way:**

| # | Problem | Evidence |
|---|---|---|
| P1 | Success and error feedback is a plain `<p>` at the top of the page. It scrolls out of view and every screen places it differently | `message()` in 19 templates, `error()` in 31; `UiToastService` exists but is barely used |
| P2 | Filters work differently on every list | `ui-filter-bar` used on 7 of 20 lists; others have hand-built inputs, a manual **Search** button, or the same filter twice (products: stat chips plus a select) |
| P3 | Row actions are full buttons in every row, including **Delete** in red | 22 templates use `ui-btn-danger` inline; tables get wide and noisy, and it is easy to click Delete by mistake |
| P4 | Bulk work isn't possible even though the table supports selection | `selectionChange` exists on `ui-data-table` but no screen shows a bulk action bar |
| P5 | Long forms have no map: no section jump, no Cancel, no "unsaved" hint, and save errors appear at the bottom, far from the field | Product detail (357 lines), promo form (284), order detail (275), combo form (215) |
| P6 | Detail pages bury the next step | Order detail **Actions** card stacks up to 7 equal-width buttons; normal next steps and force-cancel look alike |
| P7 | Two tables bypass the shared table | Variants in product detail, rules in promo form use raw `<table>` |
| P8 | Small copy and label bugs | Regenerate-slugs button shows "Exporting…" while running; dashboard chart titled with the nav group label `groupOverview` |
| P9 | Sidebar shows no counts, so staff must open each page to see if there's work | Orders, returns, stock notifications and reviews have no pending badge |

## 2. Design principles

1. **One pattern per job.** Every list, form and detail page uses the same template, so learning one screen means knowing them all.
2. **The next action is obvious.** One primary button per view. Destructive actions are secondary, behind a menu or a confirm.
3. **Feedback where the eye is.** Toasts for results, inline messages next to the field for validation, a banner only when the page itself fails to load.
4. **Scan first, detail on demand.** Status as a badge, numbers right-aligned and tabular, secondary info in muted text, details in a drawer or detail page.
5. **Works the same in AR/RTL, dark mode, compact density and at 375px.**

## 2b. Visual style: clean and calm

Mockups of four key screens: [`docs/admin-mockups.html`](admin-mockups.html). Open it in a browser, with a light/dark switch and an AR/RTL switch.

The admin is a work tool used for hours. It should feel quiet: neutral surfaces, plenty of white space, and brand colour only where it means something (the primary action, the active nav item, a "good" status). Everything uses the existing tokens in `_tokens.scss`; only the admin-specific values below are new.

| Element | Rule |
|---|---|
| Page canvas | New `--rgb-admin-canvas: 247 249 248` (lighter than `surface-muted`, which is too green for large areas). Cards are white on top of it |
| Sidebar | White, a 1px `ink-200` border on the inner edge, no fill colour. Active item: `brand-50` fill, `brand-700` text, 3px brand bar on the inner edge. Group labels 11px uppercase `ink-400` |
| Topbar | White at 90% with blur, 1px bottom border, 64px tall. Search field in the middle, icons on the end |
| Cards | `--radius-lg` (14px), 1px `ink-200` border, `--shadow-card` only on hover or for floating items. 20px padding (16px compact). Heading 15px semibold with an optional muted 13px description |
| Typography | Manrope for UI; Outfit only for the page title and KPI numbers. Page title 24px/700, card heading 15px/600, body 14px, meta 13px `ink-500`, table header 12px/600 uppercase `ink-500` |
| Colour use | Brand emerald: primary button, active nav, links, success. Citrus: sale prices only. Lime: not used in admin. Warning/danger/info: badges and alerts only, never large fills |
| Buttons | Primary: solid brand, white text, 36px tall (40px on touch). Secondary: white with `ink-200` border. Ghost: text only for low-priority actions. Danger: outline red by default, solid red only inside a confirm dialog |
| Tables | No vertical lines; 1px `ink-100` row dividers; row hover `ink-50`; 52px rows (40px compact); 40px rounded thumbnails; numbers end-aligned, `tabular-nums` |
| Badges | Soft fill and dark text (`success-soft` / `success-fg`, etc.), 6px dot for live states, 12px/600, fully rounded |
| Status tabs | Text tabs with a count pill; active tab gets a 2px brand underline, not a filled pill |
| KPI cards | Label 13px muted, value 28px Outfit, delta as a small badge (▲ 12% green / ▼ red), an optional sparkline in `brand-300` |
| Charts | One brand series plus `ink-300` for comparison; status charts use the semantic colours. No gradients, no 3D, light grid lines |
| Icons | Material Icons Outlined (switch from filled) at 20px in nav and buttons, 18px in tables, `ink-500` unless active |
| Spacing | 4px base. 24px between cards and sections, 16px inside groups, 32px above a new page section |
| Motion | 150ms ease-out for hover/menus, 200ms for drawers. Respect `prefers-reduced-motion` |
| Dark mode | Canvas `ink-900`-ish (`20 28 24`), cards one step lighter, borders `white / 8%`; brand text switches to the light brand tone for contrast |

## 3. Standard page templates

Every admin screen maps to one of four templates.

### A. List page
```
┌ Page header: Title · count            [Secondary] [+ Primary] ┐
├ Status tabs: All 120 | Active 98 | Low stock 7 | Out 3 | Deleted ┤
├ Toolbar: [🔍 search, debounced]  [Filter ▾]  active chips ✕  Clear all ┤
├ Bulk bar (when rows selected): 3 selected · [Action] [Action] [Delete] ┤
├ Table: sticky header · thumbnail + title + muted ID · badges · ⋮ menu ┤
└ Pagination · rows per page ┘
```
- Status tabs replace both the stat chips and the duplicate status select.
- Search runs as you type (300 ms), so there is no **Search** button.
- Each row: the whole row opens the record; a ⋮ menu holds Edit, Duplicate, Delete/Restore.
- Empty state says why it's empty and offers the primary action ("No products yet → Add product"; "No results → Clear filters").

### B. Form page (create/edit)
```
┌ Header: ← Back · Title · status badge          [Preview] ┐
├ Section nav (≥1280px, sticky)  │  Card: Basic info        ┤
│   • Basic info                 │  Card: Pricing           │
│   • Pricing                    │  Card: Variants          │
│   • Variants                   │  ...                     │
├ Sticky bar: ● Unsaved changes        [Cancel] [Save]  ┘
```
- Error summary at the top on failed save, with links that jump to each field; field errors shown via `ui-form-field [error]`.
- Ctrl+S saves; the bar shows "Unsaved changes" only when the form is dirty.
- Save success → toast; on create, go to the new record's edit page.

### C. Record view (orders, returns, service purchases)
```
┌ Header: ← Orders · #1042 · [Processing] · placed 2h ago   [Next step ▸] [⋮] ┐
├ Main (2/3): Items · Timeline · Audit log (tabs)    │ Side (1/3): Customer · Delivery · Payment ┤
```
- The header shows the single next step for the current status (Mark processing → Ship → Deliver).
- Other actions (Recheck payment, Cancel) go in the ⋮ menu. Force actions sit in a separate "Danger zone" section and always confirm.

### D. Simple CRUD (categories, brands, cities, areas, companies)
List page (template A) plus a side drawer for create/edit, so the list stays visible. Same form field and bar rules as template B, scaled down.

## 4. Phase 1: shared foundations

These changes fix most problems across all screens at once.

**Status (24 Nov 2026): built.** New: `ui-alert`, `ui-error-summary`, `ui-form-section-nav`, `ui-action-menu`, `ui-form-bar`, plus `_admin.scss`. Extended: `ui-data-table` (`tabs`/`tabChange`, `activeFilters`/`filterRemove`/`filtersClear`, `serverSearch`/`searchChange`, bulk bar, filtered empty state, `[table-empty-actions]`) and `ui-page-header` (`backLink`, `[header-status]`). The admin shell now has a white sidebar, a light canvas and outlined icons. Screens start using these in Phase 3; moving each screen's inline messages to toasts happens when that screen is redesigned.

| Item | Change | Fixes |
|---|---|---|
| Feedback | Move all success messages to `UiToastService`. Keep inline `ui-alert` only for page-load failure (with **Retry**). Add `ui-alert` (info/warning/danger) to the kit | P1 |
| `ui-data-table` | Add a `rowActions` slot rendered as a ⋮ menu (CDK menu); a bulk bar that appears with `selectionChange`; a sticky header; a status-tabs input (`label`, `count`, `value`); debounced search output; active-filter chips with **Clear all** | P2 P3 P4 |
| `ui-filter-bar` | One standard toolbar (search plus "Filters" popover/drawer) used by all 20 lists | P2 |
| `ui-page-header` | Add a `backLink`, a status badge slot and a meta line (e.g. "Updated 2h ago") | P5 P6 |
| `admin-form-bar` | Add Cancel, a dirty indicator, Ctrl+S, and a saving spinner on the button | P5 |
| `ui-form-section-nav` (new) | Sticky anchor list built from card headings, highlights the section in view | P5 |
| `ui-error-summary` (new) | Lists invalid fields with jump links after a failed submit | P5 |
| `ui-empty-state` | Standard copy pattern: icon, reason, primary action, "Clear filters" when filtered | — |
| Confirm dialogs | All destructive actions through `UiConfirmService` with the record name and the consequence ("Delete *Whey Gold 2kg*? Customers will no longer see it.") | P3 P6 |
| Typography and spacing | Page title 24/32 semibold, card heading 16 semibold, body 14, muted 13; 24px between cards; numbers `tabular-nums` and end-aligned | — |

## 5. Phase 2: shell

- Pending-count badges on Orders, Returns, Stock notifications and Reviews (from the existing dashboard data).
- Command palette: add "Recently visited" and record search (order number, product name).
- User menu: close on Esc and outside click; arrow-key navigation.
- Collapsed sidebar: tooltips instead of `title`; remember open/closed per user.
- Mobile: keep the drawer; make the topbar show page title plus one primary action.

## 6. Phase 3: screen by screen

**Status (24 Nov 2026): all groups done.** Services, Content and Settings got a lighter pass: back links, toasts for success, red alerts with Retry, ⋮ row menus, and the shared form bar on the service, service-offer and blog forms. Not done: drag-and-drop images, promo live summary, dashboard date range, sidebar pending counts (Phase 2).

**Marketing done.** Promo codes, flash sales and combo offers (lists and forms) and the newsletter use the new patterns. The lists share Live / Scheduled / Ended / Off tabs from `core/utils/campaign-status.ts`.

**Sales done.** Dashboard, orders, order detail, returns, return detail, reviews and stock alerts are on the new templates. The dashboard date-range selector is not built because the dashboard API has no date parameter.

**Catalogue done.** Products list, product edit, product create, categories and brands are on the new templates. On the product edit page, variants are still saved one row at a time (that's how the current API works).

Order: **Catalogue first** (the largest, hardest forms, and the screens that prove the list and form templates), then Sales, Marketing, Services, Content, Settings.

| Group | Screen | Template | Specific changes |
|---|---|---|---|
| Catalogue | Products list | A | Status tabs replace stat chips and select; ⋮ row menu (Edit, Duplicate, Delete/Restore); bulk delete/restore; move "Regenerate slugs" into the header ⋮ and fix its label |
| Catalogue | Product create / detail | B | Section nav; variants table on `ui-data-table` or a clean inline-edit grid; image upload with drag-and-drop, reorder and "set as main"; price preview showing discount % |
| Catalogue | Categories, Brands | D | Drawer edit; image thumbnail in row; drag to reorder if sort order exists |
| Overview | Dashboard | — | "Needs attention" cards link straight to filtered lists; give the bar chart a real title; date-range selector (Today / 7d / 30d); recent orders as a compact table with status badges |
| Sales | Orders list | A | Status tabs (Pending, Processing, Shipped, Delivered, Cancelled); filters for date, city, payment; bulk "Mark processing" and "Export" |
| Sales | Order detail | C | Next-step button in header; ⋮ for secondary; Danger zone for force actions; Items/Timeline/Audit as tabs; copy buttons for phone and address |
| Sales | Returns list / detail | A / C | Same as orders; approve/reject as the header's primary/secondary |
| Sales | Stock notifications | A | Group by product; bulk "Notify customers" |
| Sales | Reviews | A | Status tabs (Pending / Approved / Hidden); approve/hide in row and in bulk; star rating shown as icons |
| Marketing | Promo codes list / form | A / B | Status tabs (Active / Scheduled / Expired); form sections (Code, Discount, Limits, Eligibility); rules table on the shared table; live summary card ("10% off orders over 200 AED, until 31 Dec") |
| Marketing | Flash sales, Combo offers | A / B | Same as promo; schedule shown as a date range badge; product picker with search and thumbnails |
| Marketing | Newsletter | A | Subscriber count in header; export as primary |
| Services | Services, Service offers | A / B | Same list and form templates as products |
| Services | Service purchases list / detail | A / C | Same as orders |
| Content | Blog list / form | A / B | Draft/Published tabs; editor gets a full-width card with a sticky toolbar; SEO section collapsed by default; preview link |
| Content | Video banner | B | Live preview next to the form |
| Settings | Cities, City areas, Companies | D | Drawer edit; areas reached from a city row ("Manage areas") with breadcrumb back |
| Settings | Users | A | Role as badge; role change in drawer with confirm; search by name/email |

## 7. Phase 4: quality pass

Checklist for every screen before it is called done:
- [ ] Matches its template (A/B/C/D); one primary button
- [ ] No inline success `<p>`; toasts for results, field errors at the field
- [ ] Destructive actions confirm with record name
- [ ] Loading = skeleton, empty = `ui-empty-state` with action, error = alert with Retry
- [ ] Works at 375px, 768px, 1280px, 1600px
- [ ] Arabic/RTL: icons mirrored, alignment correct, no truncated labels
- [ ] Dark mode and compact density look right
- [ ] Keyboard: tab order, focus ring, Esc closes drawers/menus, Ctrl+S on forms
- [ ] WCAG AA contrast; icon-only buttons have `aria-label`
- [ ] All copy in `admin-ui-text.ts` (EN + AR), no hard-coded strings

## 8. Delivery order and size

| Step | Work | Rough size |
|---|---|---|
| 1 | Phase 1 foundations (kit, visual style tokens, outlined icons) | 4 days |
| 2 | Shell (Phase 2) | 1 day |
| 3 | Catalogue (products list/form, categories, brands) | 3 days |
| 4 | Sales screens (dashboard, orders, returns, stock, reviews) | 3 days |
| 5 | Marketing (promo, flash, combo, newsletter) | 2–3 days |
| 6 | Services + Content + Settings | 3 days |
| 7 | Quality pass, RTL/dark/mobile fixes | 2 days |

Ship each step as its own PR so it can be reviewed and tested on its own. Foundations first, because every later step depends on them.

## 9. Out of scope

- Backend or API changes. Pending-count badges and filters use data the API already returns; anything needing a new endpoint is listed separately before work starts.
- New UI libraries (Angular CDK is already installed and covers menus and overlays).
- Changes to the customer storefront.
