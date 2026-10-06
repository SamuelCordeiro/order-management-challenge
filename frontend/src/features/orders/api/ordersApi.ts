import { getJson, postJson } from '../../../shared/api/httpClient';

export type OrderStatus = 'pendente' | 'processando' | 'finalizado';

export interface Order {
  id: string;
  cliente: string;
  produto: string;
  valor: number;
  status: OrderStatus;
  data_criacao: string;
}

export interface CreateOrderInput {
  cliente: string;
  produto: string;
  valor: number;
}

export interface OrderStatusHistoryItem {
  status: OrderStatus;
  ocorrido_em: string;
  origem: string;
}

export function getOrders(signal?: AbortSignal): Promise<Order[]> {
  return getJson<Order[]>('/orders', signal);
}

export function getOrder(id: string, signal?: AbortSignal): Promise<Order> {
  return getJson<Order>(`/orders/${id}`, signal);
}

export function getOrderStatusHistory(id: string, signal?: AbortSignal): Promise<OrderStatusHistoryItem[]> {
  return getJson<OrderStatusHistoryItem[]>(`/orders/${id}/history`, signal);
}

export function createOrder(input: CreateOrderInput): Promise<Order> {
  return postJson<Order, CreateOrderInput>('/orders', input);
}
