import {
  MantineProvider,
  Alert,
  Button,
  Text,
  Container,
  Flex,
  Progress,
  Code,
  CopyButton,
  ScrollArea,
  Tooltip,
} from '@mantine/core';
import {
  IconAlertCircle,
  IconCheck,
  IconCopy,
  IconServer,
  IconTerminal2,
} from '@tabler/icons-react';
import React, { useEffect, useState, useRef, useCallback, ReactNode } from 'react';
import { cssVarResolver } from '../../App';
import { BACKEND_BASE_URL } from '../../Constants.ts';
import { useSettings } from '../../context/SettingsContext';
import { theme } from '../../theme/Theme';

interface BackendGateProps {
  children: ReactNode;
  /** Timeout in ms to wait for health */
  healthTimeoutMs?: number;
  /** Interval between probes ms */
  probeIntervalMs?: number;
}

declare global {
  interface Window {
    __BACKEND_GATE__?: { ready: boolean };
  }
}

/** Waits for the main-process-spawned backend sidecar to respond, and gates rendering until ready. */
const BackendGate: React.FC<BackendGateProps> = ({
  children,
  healthTimeoutMs = 5000,
  probeIntervalMs = 500,
}) => {
  const { settings } = useSettings();
  const [status, setStatus] = useState<'idle' | 'starting' | 'ready' | 'error'>('idle');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [stage, setStage] = useState<'init' | 'health' | 'ready'>('init');
  const [progress, setProgress] = useState<number>(0);
  const [sidecarLogs, setSidecarLogs] = useState<string[]>([]);
  const startingRef = useRef<boolean>(false);
  const gateEnabledRef = useRef<boolean>(false);

  const appendLog = useCallback((line: string) => {
    setSidecarLogs((prev) => [...prev, line]);
  }, []);

  const isProd = import.meta.env.PROD;
  const effectiveGateInDev = settings?.gateInDev;
  const shouldGate = isProd || effectiveGateInDev;
  gateEnabledRef.current = !!shouldGate;

  async function isHealthy(baseUrl: string, timeoutMs: number) {
    try {
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), Math.min(3000, timeoutMs));
      const res = await fetch(baseUrl + '/health', { signal: controller.signal });
      clearTimeout(id);
      return res.ok;
    } catch {
      return false;
    }
  }

  const attemptStart = useCallback(
    async (manual = false) => {
      if (startingRef.current) return;
      if (window.__BACKEND_GATE__?.ready && !manual) {
        setStatus('ready');
        setStage('ready');
        setProgress(100);
        return;
      }
      if (!gateEnabledRef.current) {
        setStatus('ready');
        setStage('ready');
        setProgress(100);
        return;
      }
      startingRef.current = true;
      setStatus('starting');
      setStage('init');
      setProgress(10);
      try {
        if (manual) {
          await window.electronAPI?.backend.restart();
          setSidecarLogs([]);
        }
        const pre = await isHealthy(BACKEND_BASE_URL, 2500);
        if (pre) {
          window.__BACKEND_GATE__ = { ready: true };
          setStage('ready');
          setProgress(100);
          setStatus('ready');
          setErrorMsg(null);
          return;
        }
        setStage('health');
        setProgress(40);
        const start = Date.now();
        let healthy = false;
        while (Date.now() - start < healthTimeoutMs) {
          if (!gateEnabledRef.current) {
            setStatus('ready');
            setStage('ready');
            setProgress(100);
            healthy = true;
            break;
          }
          if (await isHealthy(BACKEND_BASE_URL, 2500)) {
            healthy = true;
            break;
          }
          const elapsed = Date.now() - start;
          setProgress(40 + Math.min(50, Math.floor(50 * (elapsed / healthTimeoutMs))));
          await new Promise((r) => setTimeout(r, probeIntervalMs));
        }
        if (!healthy) {
          throw new Error(
            `Backend did not respond on ${BACKEND_BASE_URL}/health within ${Math.round(healthTimeoutMs / 1000)}s`
          );
        }
        window.__BACKEND_GATE__ = { ready: true };
        setStage('ready');
        setProgress(100);
        setStatus('ready');
        setErrorMsg(null);
      } catch (e: any) {
        setStage('init');
        setProgress(0);
        const msg = e?.message || String(e);
        setErrorMsg(msg);
        setStatus('error');
        console.error(e);
        window.__BACKEND_GATE__ = { ready: false };
      } finally {
        startingRef.current = false;
      }
    },
    [healthTimeoutMs, probeIntervalMs]
  );

  // Subscribe to sidecar log events from the main process.
  useEffect(() => {
    const off = window.electronAPI?.backend.onLog((line) => appendLog(line));
    return () => {
      if (off) off();
    };
  }, [appendLog]);

  useEffect(() => {
    setStatus('idle');
    setStage('init');
    setProgress(0);
    if (!window.__BACKEND_GATE__?.ready) {
      attemptStart(false);
    } else {
      setStatus('ready');
      setStage('ready');
      setProgress(100);
    }
    /* eslint-disable-next-line react-hooks/exhaustive-deps */
  }, []);

  useEffect(() => {
    if (!isProd && !effectiveGateInDev) {
      setStatus('ready');
      setStage('ready');
      setProgress(100);
      return;
    }
    if (!isProd && effectiveGateInDev) {
      setStatus('idle');
      setStage('init');
      setProgress(0);
      attemptStart(false);
    }
  }, [effectiveGateInDev, isProd, attemptStart]);

  if (!shouldGate || status === 'ready') return <>{children}</>;

  const starting = status === 'starting' || status === 'idle';
  const stageText = { init: 'Initializing...', health: 'Waiting for health...', ready: 'Ready!' };
  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>
      <Container size="sm" pt={80}>
        {starting && (
          <Alert icon={<IconServer />} title="Starting backend" color="blue" variant="light">
            <Flex direction="column" gap="sm">
              <Text size="sm">{stageText[stage]}</Text>
              <Progress value={progress} size="md" color="blue" radius="md" />
            </Flex>
          </Alert>
        )}
        {status === 'error' && (
          <Flex direction="column" gap="md">
            <Alert
              icon={<IconAlertCircle />}
              title="Backend failed to start"
              color="red"
              variant="filled"
            >
              <Text size="sm" mb="xs" style={{ whiteSpace: 'pre-wrap' }}>
                {errorMsg}
              </Text>
            </Alert>
            {sidecarLogs.length > 0 && (
              <Alert icon={<IconTerminal2 />} title="Sidecar output" color="gray" variant="light">
                <Flex direction="column" gap="xs">
                  <ScrollArea.Autosize mah={200}>
                    <Code
                      block
                      style={{ whiteSpace: 'pre-wrap', fontSize: 'var(--mantine-font-size-xs)' }}
                    >
                      {sidecarLogs.join('\n')}
                    </Code>
                  </ScrollArea.Autosize>
                  <CopyButton value={`Error: ${errorMsg ?? ''}\n\n${sidecarLogs.join('\n')}`}>
                    {({ copied, copy }) => (
                      <Tooltip label={copied ? 'Copied' : 'Copy log output'} withArrow>
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
              <Button size="xs" onClick={() => attemptStart(true)}>
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
