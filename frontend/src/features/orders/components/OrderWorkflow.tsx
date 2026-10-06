import { Step, StepLabel, Stepper } from '@mui/material';
import { useTranslation } from 'react-i18next';
import type { OrderStatus } from '../api/ordersApi';

const steps: OrderStatus[] = ['pendente', 'processando', 'finalizado'];

export function OrderWorkflow({ status }: { status: OrderStatus }) {
  const { t } = useTranslation();
  const activeStep = steps.findIndex((step) => step === status);

  return (
    <Stepper activeStep={activeStep} alternativeLabel>
      {steps.map((step) => <Step key={step}><StepLabel>{t(`orders.statuses.${step}`)}</StepLabel></Step>)}
    </Stepper>
  );
}
