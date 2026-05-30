import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { RootErrorBoundary } from './components/common/ErrorBoundary.tsx';
import { router } from './router';

const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <RootErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </RootErrorBoundary>
);
