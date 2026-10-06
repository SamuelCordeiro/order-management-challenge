import type { Order } from '../api/ordersApi';

export type OrderSortKey = 'cliente' | 'produto' | 'valor' | 'status' | 'data_criacao';
export type SortDirection = 'asc' | 'desc';

export function sortOrders(
  orders: Order[],
  sortKey: OrderSortKey,
  sortDirection: SortDirection,
  locale: string,
  getStatusLabel: (status: Order['status']) => string
): Order[] {
  const directionFactor = sortDirection === 'asc' ? 1 : -1;

  return [...orders].sort((left, right) => {
    const comparison = sortKey === 'valor'
      ? left.valor - right.valor
      : sortKey === 'data_criacao'
        ? new Date(left.data_criacao).getTime() - new Date(right.data_criacao).getTime()
        : sortKey === 'status'
          ? getStatusLabel(left.status).localeCompare(getStatusLabel(right.status), locale)
          : left[sortKey].localeCompare(right[sortKey], locale);

    return comparison * directionFactor;
  });
}
