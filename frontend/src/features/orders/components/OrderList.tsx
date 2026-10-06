import { ArrowDownward, ArrowForward, ArrowUpward } from '@mui/icons-material';
import {
  Button,
  Card,
  CardActions,
  CardContent,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Tooltip,
  Typography,
  useMediaQuery
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { Order } from '../api/ordersApi';
import { sortOrders, type OrderSortKey, type SortDirection } from '../utils/orderSorting';
import { formatCurrency, formatDate } from '../utils/formatters';
import { OrderStatusChip } from './OrderStatusChip';

const sortableColumns: { key: OrderSortKey; labelKey: string }[] = [
  { key: 'cliente', labelKey: 'orders.customer' },
  { key: 'produto', labelKey: 'orders.product' },
  { key: 'valor', labelKey: 'orders.amount' },
  { key: 'status', labelKey: 'orders.status' },
  { key: 'data_criacao', labelKey: 'orders.createdAt' }
];

export function OrderList({ orders }: { orders: Order[] }) {
  const { i18n, t } = useTranslation();
  const theme = useTheme();
  const compact = useMediaQuery(theme.breakpoints.down('sm'));
  const [sortKey, setSortKey] = useState<OrderSortKey>('data_criacao');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const sortedOrders = useMemo(
    () => sortOrders(orders, sortKey, sortDirection, i18n.language, (status) => t(`orders.statuses.${status}`)),
    [orders, sortDirection, sortKey, i18n.language, t]
  );

  function requestSort(nextKey: OrderSortKey) {
    if (nextKey === sortKey) {
      setSortDirection((currentDirection) => currentDirection === 'asc' ? 'desc' : 'asc');
      return;
    }

    setSortKey(nextKey);
    setSortDirection(nextKey === 'data_criacao' ? 'desc' : 'asc');
  }

  if (compact) {
    return (
      <Stack spacing={2}>
        <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={1}>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="order-sort-label">{t('orders.sortBy')}</InputLabel>
            <Select
              label={t('orders.sortBy')}
              labelId="order-sort-label"
              onChange={(event) => requestSort(event.target.value as OrderSortKey)}
              value={sortKey}
            >
              {sortableColumns.map((column) => <MenuItem key={column.key} value={column.key}>{t(column.labelKey)}</MenuItem>)}
            </Select>
          </FormControl>
          <Tooltip title={t(`orders.${sortDirection === 'asc' ? 'descending' : 'ascending'}`)}>
            <IconButton aria-label={t(`orders.${sortDirection === 'asc' ? 'descending' : 'ascending'}`)} onClick={() => requestSort(sortKey)}>
              {sortDirection === 'asc' ? <ArrowUpward /> : <ArrowDownward />}
            </IconButton>
          </Tooltip>
        </Stack>
        {sortedOrders.map((order) => (
          <Card key={order.id} variant="outlined">
            <CardContent>
              <Stack alignItems="flex-start" direction="row" justifyContent="space-between" spacing={2}>
                <div>
                  <Typography fontWeight={700}>{order.cliente}</Typography>
                  <Typography color="text.secondary" variant="body2">
                    {order.produto}
                  </Typography>
                </div>
                <OrderStatusChip status={order.status} />
              </Stack>
              <Typography mt={2} variant="body2">
                {formatCurrency(order.valor, i18n.language)} · {formatDate(order.data_criacao, i18n.language)}
              </Typography>
            </CardContent>
            <CardActions>
              <Button component={RouterLink} endIcon={<ArrowForward />} size="small" to={`/orders/${order.id}`}>
                {t('orders.viewDetails')}
              </Button>
            </CardActions>
          </Card>
        ))}
      </Stack>
    );
  }

  return (
    <TableContainer component={Card} variant="outlined">
      <Table aria-label={t('orders.listAriaLabel')}>
        <TableHead>
          <TableRow>
            {sortableColumns.map((column) => (
              <TableCell key={column.key} sortDirection={sortKey === column.key ? sortDirection : false}>
                <TableSortLabel
                  active={sortKey === column.key}
                  direction={sortKey === column.key ? sortDirection : 'asc'}
                  onClick={() => requestSort(column.key)}
                >
                  {t(column.labelKey)}
                </TableSortLabel>
              </TableCell>
            ))}
            <TableCell align="right">{t('orders.actions')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {sortedOrders.map((order) => (
            <TableRow hover key={order.id}>
              <TableCell>{order.cliente}</TableCell>
              <TableCell>{order.produto}</TableCell>
              <TableCell>{formatCurrency(order.valor, i18n.language)}</TableCell>
              <TableCell><OrderStatusChip status={order.status} /></TableCell>
              <TableCell>{formatDate(order.data_criacao, i18n.language)}</TableCell>
              <TableCell align="right">
                <Button component={RouterLink} size="small" to={`/orders/${order.id}`}>
                  {t('orders.details')}
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
