import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import i18n from '../../../app/i18n';
import { OrderStatusChip } from './OrderStatusChip';

describe('OrderStatusChip', () => {
  it.each([
    ['pendente', 'Pendente'],
    ['processando', 'Processando'],
    ['finalizado', 'Finalizado']
  ] as const)('shows %s as %s', (status, label) => {
    render(<OrderStatusChip status={status} />);

    expect(screen.getByText(label)).toBeInTheDocument();
  });

  it('uses the selected language', async () => {
    await i18n.changeLanguage('en-US');
    render(<OrderStatusChip status="processando" />);

    expect(screen.getByText('Processing')).toBeInTheDocument();
  });
});
