import { useQuery } from '@tanstack/react-query';
import { getOrders } from '../api/ordersApi';
import { useRealtimeStatusAvailability } from '../realtime/realtimeStatusContext';

const processingStatuses = new Set(['pendente', 'processando']);

export function useOrders() {
  const isRealtimeAvailable = useRealtimeStatusAvailability();

  return useQuery({
    queryKey: ['orders'],
    queryFn: ({ signal }) => getOrders(signal),
    refetchInterval: (query) =>
      !isRealtimeAvailable && query.state.data?.some((order) => processingStatuses.has(order.status)) ? 2_000 : false
  });
}
