import { Circle } from '@mui/icons-material';
import { Alert, Box, CircularProgress, List, ListItem, ListItemIcon, ListItemText, Paper, Stack, Typography } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { OrderStatusHistoryItem } from '../api/ordersApi';
import { formatDate } from '../utils/formatters';
import { OrderStatusChip } from './OrderStatusChip';

interface OrderStatusHistoryProps {
  history?: OrderStatusHistoryItem[];
  isError: boolean;
  isPending: boolean;
}

export function OrderStatusHistory({ history, isError, isPending }: OrderStatusHistoryProps) {
  const { i18n, t } = useTranslation();

  return (
    <Paper component="section" sx={{ mt: 3 }} variant="outlined">
      <Box px={3} pt={3}>
        <Typography component="h2" variant="h6">{t('orderHistory.title')}</Typography>
        <Typography color="text.secondary" mt={0.5} variant="body2">{t('orderHistory.subtitle')}</Typography>
      </Box>
      {isPending && <Stack alignItems="center" py={4}><CircularProgress aria-label={t('orderHistory.loading')} size={28} /></Stack>}
      {isError && <Alert severity="warning" sx={{ m: 3 }}>{t('orderHistory.error')}</Alert>}
      {!isPending && !isError && (
        <List aria-label={t('orderHistory.listAriaLabel')} sx={{ px: 3, pb: 2 }}>
          {history?.map((item) => <HistoryItem item={item} key={`${item.status}-${item.ocorrido_em}`} />)}
        </List>
      )}
    </Paper>
  );

  function HistoryItem({ item }: { item: OrderStatusHistoryItem }) {
    return (
      <ListItem alignItems="flex-start" disableGutters>
        <ListItemIcon sx={{ minWidth: 32, mt: 0.75 }}><Circle color="primary" fontSize="small" /></ListItemIcon>
        <ListItemText
          primary={<OrderStatusChip status={item.status} />}
          secondary={t('orderHistory.entry', { date: formatDate(item.ocorrido_em, i18n.language), source: t(`orderHistory.sources.${item.origem}`) })}
        />
      </ListItem>
    );
  }
}
