import { LocaleService } from '../services/locale.service';
import { UI } from '../i18n/ui-text';
import { formatIwnCurrency } from './iwn-currency';

/**
 * "Free delivery" / "Free delivery on orders over AED X" for a product or combo that allows
 * free delivery; null when it does not. The whole cart must qualify at checkout, so this is
 * a promise about this item only.
 */
export function freeDeliveryLabel(
  item: { allowFreeDelivery?: boolean; freeDeliveryMinimumAmount?: number } | null | undefined,
  locale: LocaleService,
): string | null {
  if (!item?.allowFreeDelivery) return null;
  const minimum = item.freeDeliveryMinimumAmount ?? 0;
  if (minimum <= 0) return locale.pick(UI.freeDeliveryAlways.en, UI.freeDeliveryAlways.ar);
  return locale
    .pick(UI.freeDeliveryOver.en, UI.freeDeliveryOver.ar)
    .replace('{amount}', formatIwnCurrency(minimum, locale));
}
