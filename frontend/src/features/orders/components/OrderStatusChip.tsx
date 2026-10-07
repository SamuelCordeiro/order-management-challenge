import { Chip } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { OrderStatus } from '../api/ordersApi';

const statusOptions: Record<OrderStatus, { color: 'warning' | 'info' | 'success' }> = {
  pendente: { color: 'warning' },
  processando: { color: 'info' },
  finalizado: { color: 'success' }
};

export function OrderStatusChip({ status }: Readonly<{ status: OrderStatus }>) {
  const { t } = useTranslation();
  const option = statusOptions[status];
  return <Chip color={option.color} label={t(`orders.statuses.${status}`)} size="small" />;
}
