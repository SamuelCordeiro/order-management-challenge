import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createOrder } from '../api/ordersApi';

export function useCreateOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createOrder,
    onSuccess: (order) => {
      queryClient.setQueryData(['orders', order.id], order);
      return queryClient.invalidateQueries({ queryKey: ['orders'] });
    }
  });
}
