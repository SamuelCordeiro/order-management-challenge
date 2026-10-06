import { describe, expect, it } from 'vitest';
import { formatCurrency, formatDate, parseCurrencyInput } from './formatters';

describe('formatters', () => {
  it('formats currency and dates according to the selected locale', () => {
    expect(formatCurrency(1234.56, 'pt-BR')).toContain('1.234,56');
    expect(formatCurrency(1234.56, 'en-US')).toContain('1,234.56');
    expect(formatDate('2026-10-06T12:30:00Z', 'pt-BR')).toContain('06/10/2026');
    expect(formatDate('2026-10-06T12:30:00Z', 'en-US')).toContain('10/6/26');
  });

  it('normalizes local currency input to the API numeric contract', () => {
    expect(parseCurrencyInput('1.234,56', 'pt-BR')).toBe(1234.56);
    expect(parseCurrencyInput('1,234.56', 'en-US')).toBe(1234.56);
  });
});
