import { ArrowBack } from '@mui/icons-material';
import { Box, Button, Card, CardContent, Container, Stack, Typography } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppSettings } from '../../../app/AppSettings';
import { OrderForm } from '../components/OrderForm';
import { useCreateOrder } from '../hooks/useCreateOrder';

export function NewOrderPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createOrder = useCreateOrder();

  return (
    <Box component="main" minHeight="100vh" py={{ xs: 4, md: 6 }}>
      <Container maxWidth="sm">
        <Stack alignItems="center" direction="row" justifyContent="space-between">
          <Button component={RouterLink} startIcon={<ArrowBack />} to="/orders">{t('navigation.backToOrders')}</Button>
          <AppSettings />
        </Stack>
        <Typography component="h1" mt={3} variant="h4">{t('createOrder.title')}</Typography>
        <Typography color="text.secondary" mt={1}>{t('createOrder.subtitle')}</Typography>
        <Card sx={{ mt: 4 }} variant="outlined">
          <CardContent>
            <OrderForm
              isSubmitting={createOrder.isPending}
              onSubmit={async (input) => {
                const order = await createOrder.mutateAsync(input);
                await navigate(`/orders/${order.id}`);
              }}
            />
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
}
