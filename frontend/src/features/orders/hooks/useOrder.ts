import { useQuery } from '@tanstack/react-query';
import { getOrder } from '../api/ordersApi';
import { useRealtimeStatusAvailability } from '../realtime/realtimeStatusContext';

const processingStatuses = new Set(['pendente', 'processando']);

export function useOrder(id: string) {
  const isRealtimeAvailable = useRealtimeStatusAvailability();

  return useQuery({
    queryKey: ['orders', id],
    queryFn: ({ signal }) => getOrder(id, signal),
    enabled: Boolean(id),
    refetchInterval: (query) =>
      !isRealtimeAvailable && query.state.data && processingStatuses.has(query.state.data.status) ? 2_000 : false
  });
}
