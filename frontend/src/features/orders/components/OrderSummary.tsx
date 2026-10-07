import { Card, CardContent, Grid, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { OrderSummary as OrderSummaryData } from '../api/ordersApi';

const summaryItems = [
  { key: 'total', valueKey: 'total' },
  { key: 'pending', valueKey: 'pendentes' },
  { key: 'processing', valueKey: 'processando' },
  { key: 'completed', valueKey: 'finalizados' }
] as const;

export function OrderSummary({ summary }: Readonly<{ summary: OrderSummaryData }>) {
  const { t } = useTranslation();
  return (
    <Grid container spacing={2}>
      {summaryItems.map((item) => {
        const value = summary[item.valueKey];

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
