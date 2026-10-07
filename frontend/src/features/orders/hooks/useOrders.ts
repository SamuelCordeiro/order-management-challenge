import { useQuery } from '@tanstack/react-query';
import { getOrders, type OrdersQuery } from '../api/ordersApi';
import { useRealtimeStatusAvailability } from '../realtime/realtimeStatusContext';

const processingStatuses = new Set(['pendente', 'processando']);

export function useOrders(query: OrdersQuery) {
  const isRealtimeAvailable = useRealtimeStatusAvailability();

  return useQuery({
    queryKey: ['orders', query],
    queryFn: ({ signal }) => getOrders(query, signal),
    refetchInterval: (query) =>
      !isRealtimeAvailable && query.state.data?.items.some((order) => processingStatuses.has(order.status)) ? 2_000 : false
  });
}
