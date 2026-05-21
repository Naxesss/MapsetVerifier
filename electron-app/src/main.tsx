import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { SettingsProvider } from './context/SettingsContext.tsx';
import { router } from './router';

const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <QueryClientProvider client={queryClient}>
    <SettingsProvider>
      <RouterProvider router={router} />
    </SettingsProvider>
  </QueryClientProvider>
);
