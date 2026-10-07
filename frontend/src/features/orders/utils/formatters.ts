export function formatCurrency(value: number, locale: string): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: 'BRL' }).format(value);
}

export function formatDate(value: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

export function parseCurrencyInput(value: string, locale: string): number {
  if (locale !== 'pt-BR') {
    return Number(value.replaceAll(',', ''));
  }

  const normalizedValue = value.includes(',') && value.includes('.')
    ? value.replaceAll('.', '').replace(',', '.')
    : value.replace(',', '.');

  return Number(normalizedValue);
}
