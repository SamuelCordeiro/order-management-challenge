import { ArrowBack } from '@mui/icons-material';
import { Alert, Box, Button, Card, CardContent, CircularProgress, Container, Divider, Grid, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppSettings } from '../../../app/AppSettings';
import { OrderStatusChip } from '../components/OrderStatusChip';
import { OrderWorkflow } from '../components/OrderWorkflow';
import { OrderStatusHistory } from '../components/OrderStatusHistory';
import { useOrder } from '../hooks/useOrder';
import { useOrderStatusHistory } from '../hooks/useOrderStatusHistory';
import { formatCurrency, formatDate } from '../utils/formatters';

export function OrderDetailsPage() {
  const { i18n, t } = useTranslation();
  const { id = '' } = useParams();
  const { data: order, isError, isFetching, isPending, refetch } = useOrder(id);
  const history = useOrderStatusHistory(id);

  return (
    <Box component="main" minHeight="100vh" py={{ xs: 4, md: 6 }}>
      <Container maxWidth="md">
        <Stack alignItems="center" direction="row" justifyContent="space-between">
          <Button component={RouterLink} startIcon={<ArrowBack />} to="/orders">{t('navigation.backToOrders')}</Button>
          <AppSettings />
        </Stack>
        {isPending && <Box display="flex" justifyContent="center" py={12}><CircularProgress aria-label={t('orderDetails.loading')} /></Box>}
        {isError && (
          <Alert action={<Button color="inherit" onClick={() => void refetch()} size="small">{t('orders.retry')}</Button>} severity="error" sx={{ mt: 3 }}>
            {t('errors.requestFailed')}
          </Alert>
        )}
        {order && (
          <>
            <Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" mt={3} spacing={2}>
              <div>
                <Typography component="h1" variant="h4">{t('orderDetails.title')}</Typography>
                <Typography color="text.secondary" mt={0.5}>{t('orderDetails.createdAt', { date: formatDate(order.data_criacao, i18n.language) })}</Typography>
              </div>
              <OrderStatusChip status={order.status} />
            </Stack>

            <Card sx={{ mt: 4 }} variant="outlined">
              <CardContent>
                <Typography component="h2" variant="h6">{t('orderDetails.workflow')}</Typography>
                <Typography color="text.secondary" mt={0.5} variant="body2">
                  {order.status === 'finalizado' ? t('orderDetails.completed') : t('orderDetails.updating')}
                </Typography>
                <Box mt={4}><OrderWorkflow status={order.status} /></Box>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }} variant="outlined">
              <CardContent>
                <Typography component="h2" variant="h6">{t('orderDetails.information')}</Typography>
                <Divider sx={{ my: 3 }} />
                <Grid container spacing={3}>
                  <InfoItem label={t('orders.customer')} value={order.cliente} />
                  <InfoItem label={t('orders.product')} value={order.produto} />
                  <InfoItem label={t('orders.amount')} value={formatCurrency(order.valor, i18n.language)} />
                  <InfoItem label={t('orders.currentStatus')} value={<OrderStatusChip status={order.status} />} />
                </Grid>
              </CardContent>
            </Card>
            <OrderStatusHistory history={history.data} isError={history.isError} isPending={history.isPending} />
            {isFetching && <Typography color="text.secondary" display="block" mt={2} variant="caption">{t('orderDetails.refreshing')}</Typography>}
          </>
        )}
      </Container>
    </Box>
  );
}

function InfoItem({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Grid size={{ xs: 12, sm: 6 }}>
      <Typography color="text.secondary" variant="body2">{label}</Typography>
      <Box mt={0.5}><Typography component="div" fontWeight={600}>{value}</Typography></Box>
    </Grid>
  );
}
