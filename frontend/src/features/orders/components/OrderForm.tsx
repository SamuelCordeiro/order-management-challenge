import { Alert, Box, Button, Stack, TextField } from '@mui/material';
import type { FormEvent } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../../../shared/api/ApiError';
import type { CreateOrderInput } from '../api/ordersApi';
import { parseCurrencyInput } from '../utils/formatters';

interface OrderFormProps {
  isSubmitting: boolean;
  onSubmit: (input: CreateOrderInput) => Promise<void>;
}

type FormValues = {
  cliente: string;
  produto: string;
  valor: string;
};

type FieldName = keyof FormValues;
type FieldErrors = Partial<Record<FieldName, string>>;

const fieldNames: FieldName[] = ['cliente', 'produto', 'valor'];
const apiFieldErrorKeys: Record<FieldName, string> = {
  cliente: 'createOrder.errors.customerRequired',
  produto: 'createOrder.errors.productRequired',
  valor: 'createOrder.errors.amountInvalid'
};

export function OrderForm({ isSubmitting, onSubmit }: Readonly<OrderFormProps>) {
  const { i18n, t } = useTranslation();
  const [values, setValues] = useState<FormValues>({
    cliente: '',
    produto: '',
    valor: ''
  });
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string>();

  function validateField(field: FieldName, value: string): string | undefined {
    if (field === 'cliente' && !value.trim()) {
      return t(apiFieldErrorKeys.cliente);
    }

    if (field === 'produto' && !value.trim()) {
      return t(apiFieldErrorKeys.produto);
    }

    if (field === 'valor') {
      const parsedValue = parseCurrencyInput(value, i18n.language);
      if (!value.trim() || !Number.isFinite(parsedValue) || parsedValue <= 0) {
        return t(apiFieldErrorKeys.valor);
      }
    }
  }

  function validate(valuesToValidate: FormValues): FieldErrors {
    return fieldNames.reduce<FieldErrors>(
      (errors, field) => {
        const error = validateField(field, valuesToValidate[field]);
        return error ? { ...errors, [field]: error } : errors;
      },
      {}
    );
  }

  function handleChange(field: FieldName, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setSubmitError(undefined);
    setFieldErrors((current) => ({
      ...current,
      [field]:
        field === 'valor' && value.trim()
          ? validateField(field, value)
          : undefined,
    }));
  }

  function handleBlur(field: FieldName) {
    setFieldErrors((current) => ({
      ...current,
      [field]: validateField(field, values[field])
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(undefined);

    const errors = validate(values);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    const valor = parseCurrencyInput(values.valor, i18n.language);

    try {
      await onSubmit({
        cliente: values.cliente.trim(),
        produto: values.produto.trim(),
        valor,
      });
    } catch (exception) {
      if (exception instanceof ApiError) {
        const apiFieldErrors = toFieldErrors(exception.validationErrors, t);
        if (Object.keys(apiFieldErrors).length > 0) {
          setFieldErrors(apiFieldErrors);
          return;
        }

        setSubmitError(t('createOrder.errors.requestFailed'));
        return;
      }

      setSubmitError(t('createOrder.errors.requestFailed'));
    }
  }

  return (
    <Box component="form" noValidate onSubmit={handleSubmit}>
      <Stack spacing={3}>
        {submitError && <Alert severity="error">{submitError}</Alert>}
        <TextField
          autoComplete="name"
          disabled={isSubmitting}
          error={Boolean(fieldErrors.cliente)}
          helperText={fieldErrors.cliente}
          label={t('orders.customer')}
          onBlur={() => handleBlur('cliente')}
          onChange={(event) => handleChange('cliente', event.target.value)}
          required
          value={values.cliente}
        />
        <TextField
          disabled={isSubmitting}
          error={Boolean(fieldErrors.produto)}
          helperText={fieldErrors.produto}
          label={t('orders.product')}
          onBlur={() => handleBlur('produto')}
          onChange={(event) => handleChange('produto', event.target.value)}
          required
          value={values.produto}
        />
        <TextField
          disabled={isSubmitting}
          error={Boolean(fieldErrors.valor)}
          helperText={fieldErrors.valor ?? t('createOrder.amountHint')}
          inputProps={{ inputMode: 'decimal', placeholder: t('createOrder.amountPlaceholder') }}
          label={t('orders.amount')}
          onBlur={() => handleBlur('valor')}
          onChange={(event) => handleChange('valor', event.target.value)}
          required
          type="text"
          value={values.valor}
        />
        <Button
          disabled={isSubmitting}
          size="large"
          type="submit"
          variant="contained"
        >
          {isSubmitting ? t('createOrder.submitting') : t('createOrder.submit')}
        </Button>
      </Stack>
    </Box>
  );
}

function toFieldErrors(
  validationErrors: Record<string, string[]>,
  t: (key: string) => string
): FieldErrors {
  return Object.entries(validationErrors).reduce<FieldErrors>(
    (errors, [field]) => {
      const normalizedField = field.toLowerCase();
      if (isFieldName(normalizedField)) {
        errors[normalizedField] = t(apiFieldErrorKeys[normalizedField]);
      }

      return errors;
    },
    {}
  );
}

function isFieldName(value: string): value is FieldName {
  return value in apiFieldErrorKeys;
}
