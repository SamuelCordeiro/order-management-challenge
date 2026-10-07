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

export type OrderSortKey = 'cliente' | 'produto' | 'valor' | 'status' | 'data_criacao';
export type SortDirection = 'asc' | 'desc';

export interface OrdersQuery { page: number; pageSize: number; sortBy: OrderSortKey; sortDirection: SortDirection; }
export interface OrderSummary { total: number; pendentes: number; processando: number; finalizados: number; }
export interface PagedOrders { items: Order[]; page: number; page_size: number; total_count: number; summary: OrderSummary; }

export function getOrders(query: OrdersQuery, signal?: AbortSignal): Promise<PagedOrders> {
  const search = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize), sortBy: query.sortBy, sortDirection: query.sortDirection });
  return getJson<PagedOrders>(`/orders?${search}`, signal);
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
