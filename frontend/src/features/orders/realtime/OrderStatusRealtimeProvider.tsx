import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState, type PropsWithChildren } from 'react';
import { getApiUrl } from '../../../shared/api/httpClient';
import type { Order } from '../api/ordersApi';
import { RealtimeAvailabilityContext } from './realtimeStatusContext';
import { parseOrderStatusChangedEvent, updateOrderStatus } from './orderStatusEvents';

export function OrderStatusRealtimeProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient();
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const source = new EventSource(getApiUrl('/orders/events'));

    source.onopen = () => setIsConnected(true);
    source.onerror = () => setIsConnected(false);
    source.addEventListener('order.status.changed', (event) => {
      const statusChanged = parseOrderStatusChangedEvent(event);
      if (!statusChanged) return;

      void queryClient.invalidateQueries({ queryKey: ['orders'] });
      queryClient.setQueryData<Order>(['orders', statusChanged.order_id], (order) =>
        order ? updateOrderStatus(order, statusChanged) : order
      );
      void queryClient.invalidateQueries({ queryKey: ['orders', statusChanged.order_id, 'history'] });
    });

    return () => source.close();
  }, [queryClient]);

  return <RealtimeAvailabilityContext.Provider value={isConnected}>{children}</RealtimeAvailabilityContext.Provider>;
}
