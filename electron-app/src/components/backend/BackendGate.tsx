import {
  MantineProvider,
  Alert,
  Button,
  Text,
  Container,
  Flex,
  Code,
  CopyButton,
  ScrollArea,
  Tooltip,
  Loader,
  Stack,
} from '@mantine/core';
import { IconAlertCircle, IconCheck, IconCopy, IconTerminal2 } from '@tabler/icons-react';
import React, { useEffect, useState, useRef, useCallback, ReactNode } from 'react';
import { BACKEND_BASE_URL } from '../../Constants.ts';
import { useSettings } from '../../context/SettingsContext';
import { cssVarResolver } from '../../theme/cssVarResolver';
import { useAppTheme } from '../../theme/useAppTheme';

interface BackendGateProps {
  children: ReactNode;
  healthTimeoutMs?: number;
  probeIntervalMs?: number;
}

declare global {
  interface Window {
    __BACKEND_GATE__?: { ready: boolean };
  }
}

type Status = 'idle' | 'starting' | 'ready' | 'error';
type Stage = 'init' | 'health' | 'ready';

const stageText: Record<Stage, string> = {
  init: 'Initializing...',
  health: 'Loading services...',
  ready: 'Ready!',
};

async function isHealthy(baseUrl: string, timeoutMs: number): Promise<boolean> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), Math.min(3000, timeoutMs));

  try {
    const res = await fetch(`${baseUrl}/health`, {
      signal: controller.signal,
    });

    return res.ok;
  } catch {
    return false;
  } finally {
    clearTimeout(timeout);
  }
}

async function waitForHealthy(
  timeoutMs: number,
  intervalMs: number,
  check: () => Promise<boolean>
): Promise<boolean> {
  const start = Date.now();

  while (Date.now() - start < timeoutMs) {
    if (await check()) return true;
    await new Promise((resolve) => setTimeout(resolve, intervalMs));
  }

  return false;
}

const BackendGate: React.FC<BackendGateProps> = ({
  children,
  healthTimeoutMs = 5000,
  probeIntervalMs = 500,
}) => {
  const { settings, loaded } = useSettings();
  const theme = useAppTheme();

  const [status, setStatus] = useState<Status>('idle');
  const [stage, setStage] = useState<Stage>('init');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [sidecarLogs, setSidecarLogs] = useState<string[]>([]);

  const startingRef = useRef(false);

  const isProd = import.meta.env.PROD;
  const shouldGate = !loaded ? null : isProd || settings.gateInDev;

  const appendLog = useCallback((line: string) => {
    setSidecarLogs((prev) => [...prev, line]);
  }, []);

  const markReady = useCallback((persist = true) => {
    setStatus('ready');
    setStage('ready');
    setErrorMsg(null);

    if (persist) {
      window.__BACKEND_GATE__ = { ready: true };
    }
  }, []);

  const markError = useCallback((message: string) => {
    setStatus('error');
    setStage('init');
    setErrorMsg(message);
    window.__BACKEND_GATE__ = { ready: false };
  }, []);

  const reset = useCallback(() => {
    setStatus('idle');
    setStage('init');
    setErrorMsg(null);
  }, []);

  const startBackend = useCallback(async () => {
    if (startingRef.current) return;

    if (!shouldGate) {
      markReady(false);
      return;
    }

    if (window.__BACKEND_GATE__?.ready) {
      markReady();
      return;
    }

    startingRef.current = true;
    reset();

    try {
      setStatus('starting');

      await window.electronAPI?.backend.start();
      setSidecarLogs([]);

      const preHealthy = await isHealthy(BACKEND_BASE_URL, 2500);

      if (preHealthy) {
        markReady();
        return;
      }

      setStage('health');

      const healthy = await waitForHealthy(healthTimeoutMs, probeIntervalMs, () =>
        isHealthy(BACKEND_BASE_URL, 2500)
      );

      if (!healthy) {
        throw new Error('Application failed to load within the expected time.');
      }

      markReady();
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : String(e);

      console.error(e);
      markError(message);
    } finally {
      startingRef.current = false;
    }
  }, [shouldGate, healthTimeoutMs, probeIntervalMs, markReady, markError, reset]);

  useEffect(() => {
    const off = window.electronAPI?.backend.onLog(appendLog);

    return () => {
      off?.();
    };
  }, [appendLog]);

  useEffect(() => {
    if (shouldGate === null) return;

    if (!shouldGate) {
      markReady(false);
      return;
    }

    reset();
    startBackend();
  }, [shouldGate, markReady, reset, startBackend]);

  if (shouldGate === null) {
    return null;
  }

  if (!shouldGate) {
    return <>{children}</>;
  }

  if (status === 'ready' && window.__BACKEND_GATE__?.ready) {
    return <>{children}</>;
  }

  const starting = status === 'idle' || status === 'starting';

  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Container size="sm" pt={80}>
        {starting && (
          <Stack
            h="100%"
            mih={280}
            justify="center"
            align="center"
            gap="sm"
            style={{ textAlign: 'center' }}
          >
            <Loader
              size={112}
              style={{
                opacity: 0.22,
                color: 'var(--mantine-color-primary-2)',
                userSelect: 'none',
                pointerEvents: 'none',
              }}
            />
            <Text fw={700} size="lg">
              Starting Mapset Verifier
            </Text>
            <Text size="sm" c="dimmed">
              {stageText[stage]}
            </Text>
          </Stack>
        )}

        {status === 'error' && (
          <Flex direction="column" gap="md">
            <Alert
              icon={<IconAlertCircle />}
              title="Application failed to load"
              color="red"
              variant="filled"
            >
              <Text size="sm" mb="xs" style={{ whiteSpace: 'pre-wrap' }}>
                {errorMsg}
              </Text>
            </Alert>

            {sidecarLogs.length > 0 && (
              <Alert
                icon={<IconTerminal2 />}
                title="Diagnostic output"
                color="gray"
                variant="light"
              >
                <Flex direction="column" gap="xs">
                  <ScrollArea.Autosize mah={200}>
                    <Code
                      block
                      style={{
                        whiteSpace: 'pre-wrap',
                        fontSize: 'var(--mantine-font-size-xs)',
                      }}
                    >
                      {sidecarLogs.join('\n')}
                    </Code>
                  </ScrollArea.Autosize>

                  <CopyButton value={`Error: ${errorMsg ?? ''}\n\n${sidecarLogs.join('\n')}`}>
                    {({ copied, copy }) => (
                      <Tooltip label={copied ? 'Copied' : 'Copy output'} withArrow>
                        <Button
                          size="xs"
                          variant="light"
                          color={copied ? 'teal' : 'gray'}
                          onClick={copy}
                          leftSection={copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
                        >
                          {copied ? 'Copied' : 'Copy to clipboard'}
                        </Button>
                      </Tooltip>
                    )}
                  </CopyButton>
                </Flex>
              </Alert>
            )}

            <Flex gap="sm">
              <Button size="xs" onClick={startBackend}>
                Retry
              </Button>
            </Flex>
          </Flex>
        )}
      </Container>
    </MantineProvider>
  );
};

export default BackendGate;
