import { Add, Refresh } from '@mui/icons-material';
import { Alert, Box, Button, Container, Skeleton, Stack, Typography } from '@mui/material';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppSettings } from '../../../app/AppSettings';
import { OrderList } from '../components/OrderList';
import { OrderSummary } from '../components/OrderSummary';
import { useOrders } from '../hooks/useOrders';
import type { OrderSortKey, SortDirection } from '../api/ordersApi';

const sortKeys = new Set<OrderSortKey>(['cliente', 'produto', 'valor', 'status', 'data_criacao']);
const pageSizes = new Set([5, 10, 25, 50]);

export function OrdersPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Math.max(1, Number(searchParams.get('page')) || 1);
  const pageSize = pageSizes.has(Number(searchParams.get('pageSize'))) ? Number(searchParams.get('pageSize')) : 5;
  const requestedSort = searchParams.get('sortBy') as OrderSortKey;
  const sortBy = sortKeys.has(requestedSort) ? requestedSort : 'data_criacao';
  const sortDirection: SortDirection = searchParams.get('sortDirection') === 'asc' ? 'asc' : 'desc';
  const { data, isError, isFetching, isPending, refetch } = useOrders({ page, pageSize, sortBy, sortDirection });

  function updateQuery(next: Partial<{ page: number; pageSize: number; sortBy: OrderSortKey; sortDirection: SortDirection }>) {
    const query = new URLSearchParams(searchParams);
    Object.entries(next).forEach(([key, value]) => query.set(key, String(value)));
    setSearchParams(query);
  }

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

        <Box mt={4}>{isPending ? <SummarySkeleton /> : data && <OrderSummary summary={data.summary} />}</Box>

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
          {!isPending && !isError && data?.total_count === 0 && (
            <Alert severity="info">{t('orders.empty')}</Alert>
          )}
          {!isPending && !isError && data && data.total_count > 0 && <OrderList data={data} sortBy={sortBy} sortDirection={sortDirection} onPageChange={(nextPage) => updateQuery({ page: nextPage })} onPageSizeChange={(nextPageSize) => updateQuery({ page: 1, pageSize: nextPageSize })} onSortChange={(nextSortBy, nextSortDirection) => updateQuery({ page: 1, sortBy: nextSortBy, sortDirection: nextSortDirection })} />}
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
