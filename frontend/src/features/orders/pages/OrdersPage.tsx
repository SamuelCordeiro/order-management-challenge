import { Add, Refresh } from '@mui/icons-material';
import { Alert, Box, Button, Container, Skeleton, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppSettings } from '../../../app/AppSettings';
import { OrderList } from '../components/OrderList';
import { OrderSummary } from '../components/OrderSummary';
import { useOrders } from '../hooks/useOrders';

export function OrdersPage() {
  const { t } = useTranslation();
  const { data: orders = [], isError, isFetching, isPending, refetch } = useOrders();

  return (
    <Box component="main" minHeight="100vh" py={{ xs: 4, md: 6 }}>
      <Container maxWidth="lg">
        <Stack alignItems={{ sm: 'center' }} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2}>
          <div>
            <Typography component="h1" variant="h4">{t('orders.title')}</Typography>
            <Typography color="text.secondary" mt={0.5}>{t('orders.subtitle')}</Typography>
          </div>
          <Stack alignItems="center" direction="row" spacing={1}>
            <AppSettings />
            <Button component={RouterLink} startIcon={<Add />} to="/orders/new" variant="contained">{t('orders.newOrder')}</Button>
          </Stack>
        </Stack>

        <Box mt={4}>{isPending ? <SummarySkeleton /> : <OrderSummary orders={orders} />}</Box>

        <Stack alignItems="center" direction="row" justifyContent="space-between" mt={5} spacing={2}>
          <Typography component="h2" variant="h5">{t('orders.recent')}</Typography>
          <Button disabled={isFetching} onClick={() => void refetch()} startIcon={<Refresh />} variant="text">{t('orders.refresh')}</Button>
        </Stack>

        <Box mt={2}>
          {isError && (
            <Alert action={<Button color="inherit" onClick={() => void refetch()} size="small">{t('orders.retry')}</Button>} severity="error">
              {t('errors.loadOrders')}
            </Alert>
          )}
          {isPending && <ListSkeleton />}
          {!isPending && !isError && orders.length === 0 && (
            <Alert severity="info">{t('orders.empty')}</Alert>
          )}
          {!isPending && !isError && orders.length > 0 && <OrderList orders={orders} />}
        </Box>
      </Container>
    </Box>
  );
}

function SummarySkeleton() {
  return <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>{[0, 1, 2, 3].map((item) => <Skeleton height={112} key={item} variant="rounded" width="100%" />)}</Stack>;
}

function ListSkeleton() {
  return <Skeleton height={260} variant="rounded" width="100%" />;
}
