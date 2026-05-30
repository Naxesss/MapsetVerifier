import { Alert, Button, Collapse, Flex, Stack, Text, Title, UnstyledButton } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import {
  IconAlertTriangle,
  IconBug,
  IconChevronDown,
  IconHome,
  IconRefresh,
} from '@tabler/icons-react';
import { Component, ErrorInfo, ReactNode } from 'react';
import { Link } from 'react-router-dom';
import StackTraceMessage from './StackTraceMessage';
import { useOpenExternal } from '../../hooks/useOpenExternal';

const GITHUB_ISSUES_URL = 'https://github.com/Naxesss/MapsetVerifier/issues';

interface ErrorBoundaryProps {
  children: ReactNode;
  title?: string;
  showHomeLink?: boolean;
  onReset?: () => void;
  fallback?: (error: Error, errorInfo: ErrorInfo | null, retry: () => void) => ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

function buildErrorReport(error: Error, errorInfo: ErrorInfo | null): string {
  const parts = [
    'Mapset Verifier — unexpected error',
    '',
    `Message: ${error.message}`,
    '',
    'Stack trace:',
    error.stack ?? '(no stack trace)',
  ];

  if (errorInfo?.componentStack) {
    parts.push('', 'Component stack:', errorInfo.componentStack.trim());
  }

  return parts.join('\n');
}

interface ErrorFallbackProps {
  error: Error;
  errorInfo: ErrorInfo | null;
  onRetry: () => void;
  title?: string;
  showHomeLink?: boolean;
}

function ErrorFallback({ error, errorInfo, onRetry, title, showHomeLink }: ErrorFallbackProps) {
  const [detailsOpened, { toggle: toggleDetails }] = useDisclosure(false);
  const openExternal = useOpenExternal();
  const report = buildErrorReport(error, errorInfo);

  return (
    <Stack gap="md" py="xl" maw={640} mx="auto">
      <Flex direction="column" align="center" gap="xs" ta="center">
        <IconAlertTriangle size={48} stroke={1.5} color="var(--mantine-color-red-6)" />
        <Title order={3}>{title ?? 'Something went wrong'}</Title>
        <Text size="sm" c="dimmed">
          An unexpected error occurred. You can try again, go home, or report the issue if it keeps
          happening.
        </Text>
      </Flex>

      <Alert color="red" variant="light" title={error.message || 'Unknown error'}>
        <Text size="sm" c="dimmed">
          Your beatmap data and settings are unaffected. If this happened on a specific page, try
          navigating elsewhere.
        </Text>
      </Alert>

      <Flex gap="sm" wrap="wrap" justify="center">
        <Button leftSection={<IconRefresh size={16} />} onClick={onRetry}>
          Try again
        </Button>
        {showHomeLink && (
          <Button
            component={Link}
            to="/"
            variant="light"
            leftSection={<IconHome size={16} />}
            onClick={onRetry}
          >
            Go to home
          </Button>
        )}
        <Button
          variant="light"
          color="green"
          leftSection={<IconBug size={16} />}
          onClick={() => {
            void openExternal(GITHUB_ISSUES_URL);
          }}
        >
          Report on GitHub
        </Button>
      </Flex>

      <Stack gap="xs" align="center" w="100%">
        <UnstyledButton onClick={toggleDetails} c="dimmed" fz="sm">
          <Flex align="center" gap={4}>
            <IconChevronDown
              size={14}
              style={{
                transform: detailsOpened ? 'rotate(180deg)' : undefined,
                transition: 'transform 150ms ease',
              }}
            />
            {detailsOpened ? 'Hide' : 'Show'} technical details
          </Flex>
        </UnstyledButton>
        <Collapse in={detailsOpened} w="100%">
          <StackTraceMessage stackTrace={report} />
        </Collapse>
      </Stack>
    </Stack>
  );
}

interface RootErrorFallbackProps {
  error: Error;
  onRetry: () => void;
}

function RootErrorFallback({ error, onRetry }: RootErrorFallbackProps) {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2rem',
        background: '#1a1b1e',
        color: '#fff',
        fontFamily: 'system-ui, -apple-system, Segoe UI, Roboto, sans-serif',
      }}
    >
      <div style={{ maxWidth: '32rem', textAlign: 'center' }}>
        <h1 style={{ margin: '0 0 0.75rem', fontSize: '1.5rem' }}>Something went wrong</h1>
        <p style={{ margin: '0 0 1rem', color: '#9e9e9e', lineHeight: 1.5 }}>
          Mapset Verifier hit an unexpected error while loading. Try reloading the app, or report
          the issue on GitHub if it keeps happening.
        </p>
        {error.message && (
          <pre
            style={{
              margin: '0 0 1rem',
              padding: '0.75rem 1rem',
              background: '#25262b',
              borderRadius: '0.5rem',
              color: '#ffa8a8',
              fontSize: '0.8125rem',
              whiteSpace: 'pre-wrap',
              textAlign: 'left',
              overflow: 'auto',
            }}
          >
            {error.message}
          </pre>
        )}
        <div
          style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}
        >
          <button
            type="button"
            onClick={onRetry}
            style={{
              border: 'none',
              borderRadius: '0.375rem',
              padding: '0.5rem 1rem',
              background: '#339af0',
              color: '#fff',
              cursor: 'pointer',
              fontSize: '0.875rem',
            }}
          >
            Try again
          </button>
          <button
            type="button"
            onClick={() => window.location.reload()}
            style={{
              border: '1px solid #373a40',
              borderRadius: '0.375rem',
              padding: '0.5rem 1rem',
              background: 'transparent',
              color: '#fff',
              cursor: 'pointer',
              fontSize: '0.875rem',
            }}
          >
            Reload app
          </button>
        </div>
      </div>
    </div>
  );
}

export default class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null, errorInfo: null };

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.setState({ errorInfo });
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  handleRetry = () => {
    this.setState({ error: null, errorInfo: null });
    this.props.onReset?.();
  };

  render() {
    const { error, errorInfo } = this.state;

    if (error) {
      if (this.props.fallback) {
        return this.props.fallback(error, errorInfo, this.handleRetry);
      }

      return (
        <ErrorFallback
          error={error}
          errorInfo={errorInfo}
          onRetry={this.handleRetry}
          title={this.props.title}
          showHomeLink={this.props.showHomeLink}
        />
      );
    }

    return this.props.children;
  }
}

export function RootErrorBoundary({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary
      fallback={(error, _errorInfo, retry) => <RootErrorFallback error={error} onRetry={retry} />}
    >
      {children}
    </ErrorBoundary>
  );
}
