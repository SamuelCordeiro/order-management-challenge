import { Card, CardContent, Grid, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { Order } from '../api/ordersApi';

const summaryItems = [
  { key: 'total', status: undefined },
  { key: 'pending', status: 'pendente' },
  { key: 'processing', status: 'processando' },
  { key: 'completed', status: 'finalizado' }
] as const;

export function OrderSummary({ orders }: { orders: Order[] }) {
  const { t } = useTranslation();
  return (
    <Grid container spacing={2}>
      {summaryItems.map((item) => {
        const value = item.status ? orders.filter((order) => order.status === item.status).length : orders.length;

        return (
          <Grid key={item.key} size={{ xs: 6, md: 3 }}>
            <Card variant="outlined">
              <CardContent>
                <Typography color="text.secondary" variant="body2">
                  {t(`orders.summary.${item.key}`)}
                </Typography>
                <Typography component="p" mt={0.5} variant="h4">
                  {value}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        );
      })}
    </Grid>
  );
}
