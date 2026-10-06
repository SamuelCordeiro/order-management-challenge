import { useQuery } from '@tanstack/react-query';
import { getOrder } from '../api/ordersApi';

const processingStatuses = new Set(['pendente', 'processando']);

export function useOrder(id: string) {
  return useQuery({
    queryKey: ['orders', id],
    queryFn: ({ signal }) => getOrder(id, signal),
    enabled: Boolean(id),
    refetchInterval: (query) =>
      query.state.data && processingStatuses.has(query.state.data.status) ? 2_000 : false
  });
}
