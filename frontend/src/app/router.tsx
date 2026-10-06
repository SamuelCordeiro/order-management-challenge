import { Navigate, createBrowserRouter } from 'react-router-dom';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate replace to="/orders" />
  },
  {
    path: '/orders',
    lazy: async () => {
      const { OrdersPage } = await import('../features/orders/pages/OrdersPage');
      return { Component: OrdersPage };
    }
  },
  {
    path: '/orders/new',
    lazy: async () => {
      const { NewOrderPage } = await import('../features/orders/pages/NewOrderPage');
      return { Component: NewOrderPage };
    }
  },
  {
    path: '/orders/:id',
    lazy: async () => {
      const { OrderDetailsPage } = await import('../features/orders/pages/OrderDetailsPage');
      return { Component: OrderDetailsPage };
    }
  },
  {
    path: '*',
    element: <Navigate replace to="/orders" />
  }
]);
