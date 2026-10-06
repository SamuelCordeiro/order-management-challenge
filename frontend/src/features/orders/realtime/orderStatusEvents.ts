import type { Order, OrderStatus } from '../api/ordersApi';

export interface OrderStatusChangedEvent {
  order_id: string;
  status: OrderStatus;
}

const statuses = new Set<OrderStatus>(['pendente', 'processando', 'finalizado']);

export function parseOrderStatusChangedEvent(event: Event): OrderStatusChangedEvent | null {
  try {
    const payload: unknown = JSON.parse((event as MessageEvent<string>).data);
    if (!isStatusChangedEvent(payload)) return null;
    return payload;
  } catch {
    return null;
  }
}

export const updateOrderStatus = (order: Order, statusChanged: OrderStatusChangedEvent): Order =>
  order.id === statusChanged.order_id ? { ...order, status: statusChanged.status } : order;

function isStatusChangedEvent(payload: unknown): payload is OrderStatusChangedEvent {
  if (!payload || typeof payload !== 'object') return false;

  const event = payload as Record<string, unknown>;
  return typeof event.order_id === 'string' &&
    typeof event.status === 'string' &&
    statuses.has(event.status as OrderStatus);
}
