import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../shared/api/ApiError';
import { OrderForm } from './OrderForm';

describe('OrderForm', () => {
  it('submits a valid order', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<OrderForm isSubmitting={false} onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText(/Cliente/), 'Ana Silva');
    await user.type(screen.getByLabelText(/Produto/), 'Notebook');
    await user.type(screen.getByLabelText(/Valor/), '4999.90');
    await user.click(screen.getByRole('button', { name: 'Criar pedido' }));

    expect(onSubmit).toHaveBeenCalledWith({ cliente: 'Ana Silva', produto: 'Notebook', valor: 4999.9 });
  });

  it('shows an error next to each invalid field', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<OrderForm isSubmitting={false} onSubmit={onSubmit} />);

    await user.click(screen.getByRole('button', { name: 'Criar pedido' }));

    expect(await screen.findByText('Informe o cliente.')).toBeInTheDocument();
    expect(screen.getByText('Informe o produto.')).toBeInTheDocument();
    expect(screen.getByText('Informe um valor maior que zero.')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('marks the value field as invalid while an invalid format is being typed', async () => {
    const user = userEvent.setup();
    render(<OrderForm isSubmitting={false} onSubmit={vi.fn()} />);

    const valueInput = screen.getByLabelText(/Valor/);
    await user.type(valueInput, 'valor inválido');

    expect(await screen.findByText('Informe um valor maior que zero.')).toBeInTheDocument();
    expect(valueInput).toHaveAttribute('aria-invalid', 'true');
  });

  it('maps API validation errors to their respective fields', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockRejectedValue(
      new ApiError('Dados inválidos.', 400, { Cliente: ['Cliente não encontrado.'] })
    );
    render(<OrderForm isSubmitting={false} onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText(/Cliente/), 'Ana Silva');
    await user.type(screen.getByLabelText(/Produto/), 'Notebook');
    await user.type(screen.getByLabelText(/Valor/), '4999,90');
    await user.click(screen.getByRole('button', { name: 'Criar pedido' }));

    expect(await screen.findByText('Informe o cliente.')).toBeInTheDocument();
    expect(screen.queryByText('Dados inválidos.')).not.toBeInTheDocument();
  });
});
