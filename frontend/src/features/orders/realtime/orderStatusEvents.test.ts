import { describe, expect, it } from 'vitest';
import type { Order } from '../api/ordersApi';
import { parseOrderStatusChangedEvent, updateOrderStatus } from './orderStatusEvents';

const order: Order = {
  id: 'order-1',
  cliente: 'Cliente',
  produto: 'Produto',
  valor: 10,
  status: 'pendente',
  data_criacao: '2026-10-06T12:00:00Z'
};

describe('order status events', () => {
  it('parses the public snake_case contract', () => {
    const event = new MessageEvent('order.status.changed', {
      data: JSON.stringify({ order_id: 'order-1', status: 'processando' })
    });

    expect(parseOrderStatusChangedEvent(event)).toEqual({ order_id: 'order-1', status: 'processando' });
  });

  it('rejects malformed and unsupported status events', () => {
    expect(parseOrderStatusChangedEvent(new MessageEvent('event', { data: 'invalid-json' }))).toBeNull();
    expect(parseOrderStatusChangedEvent(new MessageEvent('event', {
      data: JSON.stringify({ order_id: 'order-1', status: 'cancelado' })
    }))).toBeNull();
  });

  it('updates only the matching cached order without mutation', () => {
    const updated = updateOrderStatus(order, { order_id: 'order-1', status: 'finalizado' });

    expect(updated).toMatchObject({ id: 'order-1', status: 'finalizado' });
    expect(updated).not.toBe(order);
    expect(order.status).toBe('pendente');
    expect(updateOrderStatus(order, { order_id: 'another-order', status: 'finalizado' })).toBe(order);
  });
});
