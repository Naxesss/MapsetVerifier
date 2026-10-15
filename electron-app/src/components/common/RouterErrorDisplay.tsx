import { isRouteErrorResponse, useNavigate, useRouteError } from 'react-router-dom';
import { RootErrorFallback, routeErrorToError } from './ErrorBoundary.tsx';

/** Rendered by React Router when a route element throws during render. */
export default function RouterErrorDisplay() {
  const routeError = useRouteError();
  const navigate = useNavigate();
  const error = routeErrorToError(routeError);

  return (
    <RootErrorFallback
      error={error}
      title={
        isRouteErrorResponse(routeError)
          ? `Something went wrong (${routeError.status})`
          : 'Something went wrong'
      }
      onRetry={() => {
        navigate('/', { replace: true });
      }}
    />
  );
}
