import { describe, expect, it } from 'vitest';
import type { Order } from '../api/ordersApi';
import { sortOrders } from './orderSorting';

const orders: Order[] = [
  { id: '1', cliente: 'Zeta', produto: 'Mouse', valor: 50, status: 'pendente', data_criacao: '2026-10-04T10:00:00Z' },
  { id: '2', cliente: 'Alpha', produto: 'Keyboard', valor: 100, status: 'finalizado', data_criacao: '2026-10-05T10:00:00Z' }
];

describe('sortOrders', () => {
  it('sorts by newest creation date by default', () => {
    expect(sortOrders(orders, 'data_criacao', 'desc', 'pt-BR', (status) => status).map((order) => order.id)).toEqual(['2', '1']);
  });

  it('sorts text columns in both directions without mutating the source list', () => {
    expect(sortOrders(orders, 'cliente', 'asc', 'pt-BR', (status) => status).map((order) => order.cliente)).toEqual(['Alpha', 'Zeta']);
    expect(sortOrders(orders, 'cliente', 'desc', 'pt-BR', (status) => status).map((order) => order.cliente)).toEqual(['Zeta', 'Alpha']);
    expect(orders[0].cliente).toBe('Zeta');
  });
});
