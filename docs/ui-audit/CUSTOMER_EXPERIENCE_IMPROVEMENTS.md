# Customer experience improvements — Ideal Weight Nutrition

What a shopper can and cannot do on the Arabic storefront, from the 23 Nov 2026 inspection. Recommendations avoid fake scarcity, fake countdowns, and hidden costs. Nothing here changes prices, stock rules, or payment.

Priority follows the audit: mobile purchase first, then the path from discovery to checkout, then how the store looks.

## Can a shopper find products?

Partly.

The header search is a labeled field ("ابحث عن المنتجات…") from the `md` breakpoint up, and a search icon below that. The desktop nav lists home, shop, offers, flash sales, bundles, services, and the blog. The homepage adds goal tiles (weight, muscle, daily wellness, energy) and a category row.

What gets in the way:

- The hero does not show a product. It is a green panel and two buttons. A new visitor does not see the range.
- Brand tiles repeat one shop icon, so brand browsing does not look like the brands people already know.
- Category art is cropped into a portrait hole. Landscape photos lose the product.
- Shop sort is duplicated at desktop (sidebar select and a radio row), and a fixed "الفلاتر" button still floats on the 1440px layout. Filters themselves did load: four categories and four brands.
- Offers first says there are no discounted products, then shows ten. That flash teaches the shopper the wrong thing.

Improvements:

- Hero: one photograph, one sentence, one "تسوق الآن". Secondary action is a real button to offers.
- Category and goal tiles use the catalogue image at its own aspect, with the name always visible.
- One sort control. Filters in the sidebar from 1024px up, and in a drawer below that. The drawer was not opened in this test. When it is built, it needs a labeled close button, a sticky Apply, and a clear-all action.
- Keep the skeleton on offers until products arrive.

## Are prices and discounts obvious?

On cards that have a discount, the current price is heavier than the struck price, which is the right structure. The percent badge is the part that fails: in Arabic it reads `17%-` instead of `-17%`. Shoppers can still see two prices, and they should not have to decode the badge.

Sale price color should be the promo orange `#C2410C` on white, with the old price in `#5E584F`. The badge is a small soft-orange chip with `dir="ltr"`. Do not add flashing banners or a second percent in the title.

Out-of-stock cards still show a price and a disabled Add to cart under a dark bar across the photo. Show the price, mark the photo with a small "غير متوفر" badge, and disable the button without a second banner. If a variant can be purchased, the card should say "اختر الخيار" and go to the product, which several cards already do.

## Is Add to cart obvious?

On the desktop product page, yes. "أضف إلى السلة" is a solid green button, 275×44, next to the price and the flavor chips.

On a phone, no. At 375px wide the button is 22×82. At 320px it starts at x=−7, off the screen. Buy now is squeezed to 60px. The quantity field takes about 218px of a single row that also tries to dodge the WhatsApp button.

That is the conversion bug to fix first.

The sticky bar on small screens should be:

1. Price, including the struck price when there is a discount.
2. One Add to cart button, full width, at least 48px tall, label fully visible.

Quantity stays in the product panel above the bar, with a 44px stepper and a 2.75rem input. Buy now can live there as an outline button. Wishlist stays an icon. WhatsApp and phone stay available and must not cover this bar.

Desktop can keep Add to cart and Buy now side by side. Add to cart remains the only solid button.

## Variants, nutrition, and reviews

The beef protein page (`nxt-nutrition-beef-isolate`) shows seven flavors, a quantity stepper, stock ("متوفر"), same-day delivery, and secure payment. Reviews are real quotes with a verified-purchase line. The main photo is sharp (1000×1000 drawn at 660).

Improvements:

- Flavor chips at least 44px tall, wrapping in a grid. The long name "آيس بلاست بلوراسبيري وكرز" needs the full chip width instead of a crushed row.
- Changing flavor should update the photo when the variant has one. Confirm this when the next pass clicks a flavor. This inspection did not click through flavors.
- One `h1`. Specs, suggested use, and delivery can stay accordions. Style them as surfaces, not gray `#f0f0f0` bars.
- Related products must not upscale 30px thumbnails. A soft stage and a smaller contained image is more trustworthy than a blurry tub.

Direct visits to the product URL painted "المنتج غير موجود" even though the API returned the product. Shared links and refresh are part of shopping. The page should retry on the client before the empty state. This is a presentation bug. The API response can stay as it is.

## Search

The header field is present and named. This pass did not submit a query, so result layout, empty search, and Arabic stemming were not tested. When they are tested, check:

- Results use the same card as the shop.
- An empty query does not navigate.
- No-results offers a clear filters action and a link back to the shop.
- The mobile search panel traps focus and closes on Escape.

## Filters on mobile

Not opened. The shop template has a filter button that is meant to be mobile-only, and at 1440 it is still fixed on the page. On a phone the same control is the entry to filters. The drawer must be the next thing tested at 320, 375, and 430: no horizontal scroll, Apply visible without covering the bottom nav, and the contact buttons moved out of the way.

## Cart and checkout

Only the empty states were opened. The cart had no items, so line layout, quantity edits, removal, promo codes, and shipping choices were not seen.

Empty cart: heading "سلة التسوق", "سلتك فارغة.", link "متابعة التسوق".

Empty checkout: breadcrumb back to the cart, "سلتك فارغة.", same idea. The first snapshot of checkout was only the title. The sentence appeared after hydration. Use a skeleton or the empty state immediately so the page does not look blank.

When a filled cart is tested later, the mobile checkout bar should follow the same rule as the product bar: one primary button, fully visible, above the bottom nav and the contact stack. Do not put Back, Next, and Pay on one crushed row at 320px.

Guest checkout should stay available. Login can be offered as a shorter repeat purchase, not a wall. This inspection saw login as a normal step with Google, register, and password recovery. The submit button stays disabled until the form is valid, which is fine if the disabled reason is visible (the missing fields).

## Trust

The store already says original products, UAE delivery, and expert support, on the hero, the trust row, the login column, and the product panel. That is enough. Repeating it in every section makes the page feel like a banner stack.

Keep trust next to the decision:

- Product page: in stock, delivery window, returns link.
- Checkout: order total before the pay button, and the payment method name.
- Footer: shipping, returns, track order.

Do not add fake reviewer counts, countdown timers that reset, or pre-checked add-ons.

The homepage also asks for an email twice (section and footer). Keep the footer form.

## Loading, errors, and empty states

| Moment | What the shopper sees now | What they should see |
|---|---|---|
| Shop first paint | Error, then products | Skeleton, then products |
| Offers first paint | "No discounted products", then products | Skeleton, then products |
| Services first paint | "Could not load services", then cards | Skeleton, then cards |
| Product full load | "Product not found" while the API has the product | Skeleton, then the product, or a real not-found if the client request fails |
| Admin while signed out | Blank page, then login | Login immediately |
| Cart empty | Clear sentence and a link | Keep this |

If a request actually fails, keep the current retry button. The bug is showing that state before the request finishes.

## Account, wishlist, orders

Not tested. Wishlist from a card was not clicked, because it needs a session and this browser had none. `/admin` redirected to login. Account, orders, and returns were not opened.

When a session exists, the same visual system applies: forest primary actions, cream page, readable muted text, 44px targets. Order tracking should show status in words, not color alone.

## Services

After the error flash, service cards render long Arabic descriptions and a price (the vitamin panel was 99 AED) with "عرض التفاصيل". Descriptions should clamp to three lines on the list, with the full text on the detail page. Service photos that were still loading during the homepage pass need the same stage treatment as products.

Service detail, service checkout, flash sales, and combos were not opened.

## Accessibility notes from this pass

Checked by measurement and the accessibility tree, not by a screen reader session.

- Skip link exists.
- Icon buttons that were inspected expose Arabic names (menu, search, theme, language, wishlist, cart).
- Discount badges are not isolated for RTL, so the accessible name may also read the hyphen in the wrong place. Fixing `dir="ltr"` fixes both.
- Header icons at 40px miss the 44px target.
- Two `h1` elements on the product page.
- `#A8A299` muted text fails AA on white. Move muted copy to `#5E584F`.
- Contact pulse animations need a reduced-motion alternative.
- Keyboard order, focus rings, and screen reader output were not walked. The next pass should tab from the skip link through header, shop filters, a product card, and the mobile buy bar.

## Suggested order of experience work

1. Make Add to cart fully visible at 320 and 375.
2. Move contact buttons off the trust cards and the buy bar.
3. Stop the false empty states on shop, offers, services, and product URLs.
4. Fix the percent badge and the out-of-stock card.
5. Put a product photo in the hero and retune color using [DESIGN_SYSTEM_PROPOSAL.md](DESIGN_SYSTEM_PROPOSAL.md).
6. Retest filters, search, a filled cart, and checkout with a real session before changing those flows further.
