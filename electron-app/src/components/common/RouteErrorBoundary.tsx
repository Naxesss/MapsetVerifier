import { ReactNode, useState } from 'react';
import ErrorBoundary from './ErrorBoundary';

interface Props {
  children: ReactNode;
}

export default function RouteErrorBoundary({ children }: Props) {
  const [resetKey, setResetKey] = useState(0);

  return (
    <ErrorBoundary key={resetKey} showHomeLink onReset={() => setResetKey((key) => key + 1)}>
      {children}
    </ErrorBoundary>
  );
}
