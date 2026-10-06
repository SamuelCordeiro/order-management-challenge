import { useQuery } from '@tanstack/react-query';
import { getOrders } from '../api/ordersApi';

const processingStatuses = new Set(['pendente', 'processando']);

export function useOrders() {
  return useQuery({
    queryKey: ['orders'],
    queryFn: ({ signal }) => getOrders(signal),
    refetchInterval: (query) =>
      query.state.data?.some((order) => processingStatuses.has(order.status)) ? 2_000 : false
  });
}
