import {MantineProvider, Alert, Button, Text, Container, Flex, Progress} from '@mantine/core';
import { Command } from '@tauri-apps/plugin-shell';
import React, { useEffect, useState, useRef, ReactNode } from 'react';
import { cssVarResolver } from '../../App';
import { useSettings } from '../../context/SettingsContext';
import { theme } from '../../theme/Theme';

interface BackendGateProps {
  children: ReactNode;
  port?: number;
  /** Timeout in ms to wait for health */
  healthTimeoutMs?: number;
  /** Interval between probes ms */
  probeIntervalMs?: number;
}

// Global guard to persist backend start state across mounts
declare global {
  interface Window { __BACKEND_GATE__?: { started: boolean; pid?: number } }
}

/** Handles starting the backend sidecar in production and gates rendering until ready. */
const BackendGate: React.FC<BackendGateProps> = ({
  children,
  port = 5005,
  healthTimeoutMs = 15000,
  probeIntervalMs = 500
}) => {
  const { settings } = useSettings();
  const [status, setStatus] = useState<'idle'|'starting'|'ready'|'error'>('idle');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [stage, setStage] = useState<'init'|'spawn'|'health'|'ready'>('init');
  const [progress, setProgress] = useState<number>(0);
  const childRef = useRef<any>(null);
  // Prevent concurrent/duplicate starts across re-renders
  const startingRef = useRef<boolean>(false);
  // Track gating toggle so long-running loops can exit early
  const gateEnabledRef = useRef<boolean>(false);

  const isProd = import.meta.env.PROD;
  const effectiveGateInDev = settings?.gateInDev;
  const shouldGate = isProd || effectiveGateInDev; // gate in production or if explicitly enabled in dev
  gateEnabledRef.current = !!shouldGate;

  async function isHealthy(baseUrl: string, timeoutMs: number) {
    const healthUrl = baseUrl + "/health";
    try {
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), Math.min(3000, timeoutMs));
      const res = await fetch(healthUrl, { signal: controller.signal });
      clearTimeout(id);
      return res.ok;
    } catch {
      return false;
    }
  }

  async function attemptStart(manual = false) {
    // Guard against concurrent or repeated starts
    if (startingRef.current) {
      return;
    }
    
    // Persisted global guard across mounts: if already started, skip relaunch
    if (window.__BACKEND_GATE__?.started && !manual) {
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
      const baseUrl = `http://127.0.0.1:${port}`;

      // First: if something is already serving and healthy, don't spawn again
      const preHealthy = await isHealthy(baseUrl, 2500);
      if (preHealthy) {
        window.__BACKEND_GATE__ = { started: true };
        setStage('ready');
        setProgress(100);
        setStatus('ready');
        setErrorMsg(null);
        return;
      }

      const programName = "bin/server/dist/sidecar";
      setStage('spawn');
      setProgress(30);
      const command = Command.sidecar(programName, [`--urls=${baseUrl}`]);
      
      // Listen for child process lifecycle and output if available
      childRef.current = await command.spawn();
      window.__BACKEND_GATE__ = { started: true, pid: childRef.current?.pid };
      setProgress(50);
      
      setStage('health');
      const start = Date.now();
      let healthy = false;
      while (Date.now() - start < healthTimeoutMs) {
        // Exit early if gating was disabled during health loop
        if (!gateEnabledRef.current) {
          setStatus('ready');
          setStage('ready');
          setProgress(100);
          healthy = true; // allow render to proceed
          break;
        }
        try {
          const controller = new AbortController();
          const id = setTimeout(() => controller.abort(), 4000);
          const res = await fetch(baseUrl + "/health", { signal: controller.signal });
          clearTimeout(id);
          if (res.ok) { healthy = true; break; }
        } catch (err) {
          console.error('Health probe failed, backend not ready yet', err);
        }
        // Progressively fill bar during health checks
        const elapsed = Date.now() - start;
        const healthProgress = 50 + Math.min(40, Math.floor(40 * (elapsed / healthTimeoutMs)));
        setProgress(healthProgress);
        await new Promise(r => setTimeout(r, probeIntervalMs));
      }
      
      if (!healthy) {
        throw new Error(`Backend did not respond on ${baseUrl}/health within ${Math.round(healthTimeoutMs / 1000)}s`);
      }
      
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
      // Mark as not started so user can manually retry; don't auto-relaunch
      window.__BACKEND_GATE__ = { started: false };
    } finally {
      // Allow manual retry via button, but prevent auto relaunch
      startingRef.current = false;
    }
  }

  useEffect(() => {
    setStatus('idle');
    setStage('init');
    setProgress(0);
    
    // Only auto-start on first mount if not started; avoid auto retry loops
    if (!window.__BACKEND_GATE__?.started) {
      attemptStart(false);
    } else {
      setStatus('ready');
      setStage('ready');
      setProgress(100);
    }
    /* eslint-disable-next-line react-hooks/exhaustive-deps */
  }, []);

  // React to DEV toggle changes at runtime
  useEffect(() => {
    // If gating is disabled in dev while not in production, immediately allow rendering
    if (!isProd && !effectiveGateInDev) {
      setStatus('ready');
      setStage('ready');
      setProgress(100);
      return;
    }
    // If gating is enabled in dev, always attempt to start/gate regardless of current status
    if (!isProd && effectiveGateInDev) {
      // Reset visual state so user sees gating progress
      setStatus('idle');
      setStage('init');
      setProgress(0);
      attemptStart(false);
    }
    // In production, setting changes are ignored intentionally.
  }, [effectiveGateInDev]);

  if (!shouldGate || status === 'ready') return <>{children}</>;

  const starting = status === 'starting' || status === 'idle';
  const stageText = {
    init: 'Initializing...',
    spawn: 'Spawning process...',
    health: 'Waiting for health...',
    ready: 'Ready!'
  };
  return (
    <MantineProvider defaultColorScheme="dark" theme={theme} cssVariablesResolver={cssVarResolver}>      
      <Container size="sm" pt={80}>
        {starting && (
          <Alert title="Starting backend" color="blue" variant="light">
            <Flex direction="column" gap="sm">
              <Text size="sm">{stageText[stage]}</Text>
              <Progress value={progress} size="md" color="blue" radius="md" />
            </Flex>
          </Alert>
        )}
        {status === 'error' && (
          <Flex direction="column" gap="md">
            <Alert title="Backend failed to start" color="red" variant="filled">
              <Text size="sm" mb="xs" style={{ whiteSpace: 'pre-wrap' }}>{errorMsg}</Text>
            </Alert>
            <Flex gap="sm">
              <Button size="xs" onClick={() => attemptStart(true)}>Retry</Button>
            </Flex>
          </Flex>
        )}
      </Container>
    </MantineProvider>
  );
};

export default BackendGate;
