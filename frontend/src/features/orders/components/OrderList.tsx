import { ArrowDownward, ArrowForward, ArrowUpward } from '@mui/icons-material';
import { Button, Card, CardActions, CardContent, FormControl, IconButton, InputLabel, MenuItem, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TableSortLabel, Tooltip, Typography, useMediaQuery } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { OrderSortKey, PagedOrders, SortDirection } from '../api/ordersApi';
import { formatCurrency, formatDate } from '../utils/formatters';
import { OrderStatusChip } from './OrderStatusChip';

const sortableColumns: ReadonlyArray<{ key: OrderSortKey; labelKey: string }> = [
  { key: 'cliente', labelKey: 'orders.customer' }, { key: 'produto', labelKey: 'orders.product' },
  { key: 'valor', labelKey: 'orders.amount' }, { key: 'status', labelKey: 'orders.status' },
  { key: 'data_criacao', labelKey: 'orders.createdAt' }
];

interface OrderListProps {
  data: PagedOrders;
  sortBy: OrderSortKey;
  sortDirection: SortDirection;
  onSortChange: (sortBy: OrderSortKey, sortDirection: SortDirection) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export function OrderList({ data, sortBy, sortDirection, onSortChange, onPageChange, onPageSizeChange }: Readonly<OrderListProps>) {
  const { i18n, t } = useTranslation();
  const compact = useMediaQuery(useTheme().breakpoints.down('sm'));
  const requestSort = (nextKey: OrderSortKey) => {
    if (nextKey !== sortBy) {
      onSortChange(nextKey, nextKey === 'data_criacao' ? 'desc' : 'asc');
      return;
    }

    onSortChange(nextKey, sortDirection === 'asc' ? 'desc' : 'asc');
  };
  const pagination = <TablePagination component="div" count={data.total_count} labelDisplayedRows={({ from, to, count }) => t('orders.pagination.displayedRows', { from, to, count })} labelRowsPerPage={t('orders.pagination.rowsPerPage')} onPageChange={(_, nextPage) => onPageChange(nextPage + 1)} onRowsPerPageChange={(event) => onPageSizeChange(Number(event.target.value))} page={data.page - 1} rowsPerPage={data.page_size} rowsPerPageOptions={[5, 10, 25, 50]} />;

  if (compact) return <Stack spacing={2}>
    <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={1}>
      <FormControl size="small" sx={{ minWidth: 180 }}><InputLabel id="order-sort-label">{t('orders.sortBy')}</InputLabel><Select label={t('orders.sortBy')} labelId="order-sort-label" onChange={(event) => requestSort(event.target.value as OrderSortKey)} value={sortBy}>{sortableColumns.map((column) => <MenuItem key={column.key} value={column.key}>{t(column.labelKey)}</MenuItem>)}</Select></FormControl>
      <Tooltip title={t(`orders.${sortDirection === 'asc' ? 'descending' : 'ascending'}`)}><IconButton aria-label={t(`orders.${sortDirection === 'asc' ? 'descending' : 'ascending'}`)} onClick={() => requestSort(sortBy)}>{sortDirection === 'asc' ? <ArrowUpward /> : <ArrowDownward />}</IconButton></Tooltip>
    </Stack>
    {data.items.map((order) => <Card key={order.id} variant="outlined"><CardContent><Stack alignItems="flex-start" direction="row" justifyContent="space-between" spacing={2}><div><Typography fontWeight={700}>{order.cliente}</Typography><Typography color="text.secondary" variant="body2">{order.produto}</Typography></div><OrderStatusChip status={order.status} /></Stack><Typography mt={2} variant="body2">{formatCurrency(order.valor, i18n.language)} · {formatDate(order.data_criacao, i18n.language)}</Typography></CardContent><CardActions><Button component={RouterLink} endIcon={<ArrowForward />} size="small" to={`/orders/${order.id}`}>{t('orders.viewDetails')}</Button></CardActions></Card>)}
    {pagination}
  </Stack>;

  return <Card variant="outlined"><TableContainer><Table aria-label={t('orders.listAriaLabel')}><TableHead><TableRow>{sortableColumns.map((column) => <TableCell key={column.key} sortDirection={sortBy === column.key ? sortDirection : false}><TableSortLabel active={sortBy === column.key} direction={sortBy === column.key ? sortDirection : 'asc'} onClick={() => requestSort(column.key)}>{t(column.labelKey)}</TableSortLabel></TableCell>)}<TableCell align="right">{t('orders.actions')}</TableCell></TableRow></TableHead><TableBody>{data.items.map((order) => <TableRow hover key={order.id}><TableCell>{order.cliente}</TableCell><TableCell>{order.produto}</TableCell><TableCell>{formatCurrency(order.valor, i18n.language)}</TableCell><TableCell><OrderStatusChip status={order.status} /></TableCell><TableCell>{formatDate(order.data_criacao, i18n.language)}</TableCell><TableCell align="right"><Button component={RouterLink} size="small" to={`/orders/${order.id}`}>{t('orders.details')}</Button></TableCell></TableRow>)}</TableBody></Table></TableContainer>{pagination}</Card>;
}
