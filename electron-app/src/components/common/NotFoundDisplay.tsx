import { Button } from '@mantine/core';
import { IconError404 } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import EmptyState from './EmptyState.tsx';

/** Shown for a route that doesn't exist. */
function NotFoundDisplay() {
  const navigate = useNavigate();

  return (
    <EmptyState
      fullHeight
      icon={IconError404}
      title="Page not found"
      description="This page doesn't exist in Mapset Verifier."
      action={
        <Button variant="default" onClick={() => navigate('/')}>
          Go to Home
        </Button>
      }
    />
  );
}

export default NotFoundDisplay;
