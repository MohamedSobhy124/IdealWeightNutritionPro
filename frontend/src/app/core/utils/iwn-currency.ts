export interface IwnCurrencyLocale {
  isArabic(): boolean;
  locale(): string;
}

export function formatIwnCurrency(
  value: number | null | undefined,
  locale: IwnCurrencyLocale,
): string {
  if (value == null || Number.isNaN(value)) {
    return '';
  }

  const symbol = locale.isArabic() ? 'د.إ' : 'AED';
  const formatted = new Intl.NumberFormat(locale.locale() === 'ar' ? 'ar-AE' : 'en-AE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

  return `${symbol} ${formatted}`;
}
