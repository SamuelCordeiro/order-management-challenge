export function formatCurrency(value: number, locale: string): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: 'BRL' }).format(value);
}

export function formatDate(value: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

export function parseCurrencyInput(value: string, locale: string): number {
  const normalizedValue = locale === 'pt-BR'
    ? value.includes(',') && value.includes('.')
      ? value.replaceAll('.', '').replace(',', '.')
      : value.replace(',', '.')
    : value.replaceAll(',', '');

  return Number(normalizedValue);
}
