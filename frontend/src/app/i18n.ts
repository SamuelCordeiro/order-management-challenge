import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

export const supportedLanguages = ['pt-BR', 'en-US'] as const;
export type SupportedLanguage = (typeof supportedLanguages)[number];

const languageStorageKey = 'order-management.language';

const resources = {
  'pt-BR': {
    translation: {
      language: 'Idioma',
      theme: { light: 'Ativar tema escuro', dark: 'Ativar tema claro' },
      navigation: { backToOrders: 'Voltar para pedidos' },
      orders: {
        title: 'Pedidos',
        subtitle: 'Acompanhe a criação e o processamento dos pedidos.',
        newOrder: 'Novo pedido',
        recent: 'Pedidos recentes',
        refresh: 'Atualizar',
        retry: 'Tentar novamente',
        empty: 'Ainda não há pedidos. Crie o primeiro para iniciar o processamento.',
        listAriaLabel: 'Lista de pedidos',
        details: 'Detalhes',
        viewDetails: 'Ver detalhes',
        customer: 'Cliente',
        product: 'Produto',
        amount: 'Valor',
        status: 'Status',
        currentStatus: 'Status atual',
        createdAt: 'Criação',
        actions: 'Ações',
        sortBy: 'Ordenar por',
        ascending: 'Ordem crescente',
        descending: 'Ordem decrescente',
        summary: { total: 'Total', pending: 'Pendentes', processing: 'Processando', completed: 'Finalizados' },
        statuses: { pendente: 'Pendente', processando: 'Processando', finalizado: 'Finalizado' }
      },
      createOrder: {
        title: 'Novo pedido',
        subtitle: 'O pedido será criado como pendente e processado de forma assíncrona.',
        submit: 'Criar pedido',
        submitting: 'Criando pedido…',
        amountHint: 'Use vírgula ou ponto como separador decimal.',
        amountPlaceholder: '0,00',
        errors: {
          customerRequired: 'Informe o cliente.',
          productRequired: 'Informe o produto.',
          amountInvalid: 'Informe um valor maior que zero.',
          requestFailed: 'Não foi possível criar o pedido. Tente novamente.'
        }
      },
      orderDetails: {
        title: 'Detalhe do pedido',
        createdAt: 'Criado em {{date}}',
        loading: 'Carregando pedido',
        workflow: 'Fluxo de processamento',
        completed: 'Processamento concluído.',
        updating: 'Atualização automática ativa enquanto o pedido estiver em processamento.',
        information: 'Informações',
        refreshing: 'Atualizando dados…'
      },
      errors: { requestFailed: 'Não foi possível concluir a solicitação.', loadOrders: 'Não foi possível carregar os pedidos.' }
    }
  },
  'en-US': {
    translation: {
      language: 'Language',
      theme: { light: 'Enable dark theme', dark: 'Enable light theme' },
      navigation: { backToOrders: 'Back to orders' },
      orders: {
        title: 'Orders',
        subtitle: 'Track order creation and processing.',
        newOrder: 'New order',
        recent: 'Recent orders',
        refresh: 'Refresh',
        retry: 'Try again',
        empty: 'There are no orders yet. Create the first one to start processing.',
        listAriaLabel: 'Order list',
        details: 'Details',
        viewDetails: 'View details',
        customer: 'Customer',
        product: 'Product',
        amount: 'Amount',
        status: 'Status',
        currentStatus: 'Current status',
        createdAt: 'Created',
        actions: 'Actions',
        sortBy: 'Sort by',
        ascending: 'Ascending order',
        descending: 'Descending order',
        summary: { total: 'Total', pending: 'Pending', processing: 'Processing', completed: 'Completed' },
        statuses: { pendente: 'Pending', processando: 'Processing', finalizado: 'Completed' }
      },
      createOrder: {
        title: 'New order',
        subtitle: 'The order will be created as pending and processed asynchronously.',
        submit: 'Create order',
        submitting: 'Creating order…',
        amountHint: 'Use a dot as the decimal separator.',
        amountPlaceholder: '0.00',
        errors: {
          customerRequired: 'Enter the customer.',
          productRequired: 'Enter the product.',
          amountInvalid: 'Enter an amount greater than zero.',
          requestFailed: 'Unable to create the order. Please try again.'
        }
      },
      orderDetails: {
        title: 'Order details',
        createdAt: 'Created on {{date}}',
        loading: 'Loading order',
        workflow: 'Processing workflow',
        completed: 'Processing completed.',
        updating: 'Automatic updates are active while the order is processing.',
        information: 'Information',
        refreshing: 'Updating data…'
      },
      errors: { requestFailed: 'Unable to complete the request.', loadOrders: 'Unable to load orders.' }
    }
  }
} as const;

function getInitialLanguage(): SupportedLanguage {
  const savedLanguage = localStorage.getItem(languageStorageKey);
  return supportedLanguages.includes(savedLanguage as SupportedLanguage) ? savedLanguage as SupportedLanguage : 'pt-BR';
}

void i18n.use(initReactI18next).init({
  resources,
  lng: getInitialLanguage(),
  fallbackLng: 'pt-BR',
  interpolation: { escapeValue: false }
});

i18n.on('languageChanged', (language) => localStorage.setItem(languageStorageKey, language));

export default i18n;
