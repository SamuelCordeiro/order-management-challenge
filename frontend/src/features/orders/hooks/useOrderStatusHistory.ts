import { useQuery } from '@tanstack/react-query';
import { getOrderStatusHistory } from '../api/ordersApi';
import { useRealtimeStatusAvailability } from '../realtime/realtimeStatusContext';

export function useOrderStatusHistory(id: string) {
  const isRealtimeAvailable = useRealtimeStatusAvailability();

  return useQuery({
    queryKey: ['orders', id, 'history'],
    queryFn: ({ signal }) => getOrderStatusHistory(id, signal),
    enabled: Boolean(id),
    refetchInterval: (query) =>
      !isRealtimeAvailable && query.state.data?.at(-1)?.status !== 'finalizado' ? 2_000 : false
  });
}
