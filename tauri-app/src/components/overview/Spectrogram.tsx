import {Box, Button, Center, Flex, Group, Loader, Modal, Paper, Text, useMantineTheme} from '@mantine/core';
import {useCallback, useRef, useState} from 'react';
import {SpectralAnalysisResult} from '../../Types';
import TimeAxis from '../common/TimeAxis';
import {IconZoomIn} from "@tabler/icons-react";
import {SpectrogramCanvas} from "./SpectrogramCanvas.tsx";

interface SpectrogramProps {
  data: SpectralAnalysisResult | undefined;
  isLoading: boolean;
}

function Spectrogram({ data, isLoading }: SpectrogramProps) {
  const magnitudeThreshold = -60;
  const theme = useMantineTheme();
  const containerRef = useRef<HTMLDivElement>(null);
  const [mousePos, setMousePos] = useState<{ x: number; y: number } | null>(null);
  const [hoverInfo, setHoverInfo] = useState<{ time: string; nyquistFreq: string } | null>(null);
  const [modalOpened, setModalOpened] = useState(false);

  // Throttle state for mouse move to reduce re-renders
  const lastUpdateRef = useRef<number>(0);
  const THROTTLE_MS = 16; // ~60fps

  // Calculate values that depend on data (with safe defaults)
  const duration = data?.timePositions?.length ? data.timePositions[data.timePositions.length - 1] : 0;
  const nyquistFreq = data ? data.sampleRate / 2 : 0;

  // Generate dynamic Y-axis labels based on Nyquist frequency
  const formatFreq = (hz: number) => hz >= 1000 ? `${(hz / 1000).toFixed(hz >= 10000 ? 0 : 1)}kHz` : `${hz}Hz`;
  const yAxisLabels = nyquistFreq > 0 ? [
    nyquistFreq,
    Math.min(20000, nyquistFreq * 0.9),
    nyquistFreq * 0.5,
    nyquistFreq * 0.2,
    nyquistFreq * 0.05,
    20
  ].filter(f => f >= 20 && f <= nyquistFreq) : [];

  // Format time as mm:ss for hover tooltip - memoized
  const formatTime = useCallback((seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }, []);

  // Mouse move handler for hover interaction - optimized with throttling
  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (!containerRef.current || !data?.spectrogramData?.length) return;

    // Throttle updates to reduce re-renders
    const now = Date.now();
    if (now - lastUpdateRef.current < THROTTLE_MS) return;
    lastUpdateRef.current = now;

    const rect = containerRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    // Calculate time position based on mouse X
    const timeRatio = x / rect.width;
    const timeMs = timeRatio * duration;
    const timeSec = timeMs / 1000;

    // Find the closest frame index
    const frameIndex = Math.floor(timeRatio * data.spectrogramData.length);
    if (frameIndex >= 0 && frameIndex < data.spectrogramData.length) {
      const frame = data.spectrogramData[frameIndex];

      // Find the observed Nyquist frequency (highest frequency bin with significant energy)
      let observedNyquistIndex = -1;

      // Iterate from highest frequency bin down to find the first bin with significant energy
      for (let idx = frame.magnitudes.length - 1; idx >= 0; idx--) {
        if (frame.magnitudes[idx] > magnitudeThreshold) {
          observedNyquistIndex = idx;
          break;
        }
      }

      // Calculate the frequency from the bin index
      let nyquistFreqHz = 0;
      if (observedNyquistIndex >= 0) {
        nyquistFreqHz = data.frequencyBins?.[observedNyquistIndex] || (observedNyquistIndex * (data.sampleRate / 2) / frame.magnitudes.length);
      }
      const nyquistFreqKHz = (nyquistFreqHz / 1000).toFixed(2);

      // Batch state updates to reduce re-renders
      setMousePos({ x, y });
      setHoverInfo({
        time: formatTime(timeSec),
        nyquistFreq: `${nyquistFreqKHz} kHz`
      });
    }
  }, [data, duration, magnitudeThreshold, formatTime]);

  const handleMouseLeave = useCallback(() => {
    setMousePos(null);
    setHoverInfo(null);
  }, []);

  if (isLoading) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Loader size="lg" />
        </Center>
      </Paper>
    );
  }

  if (!data) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Text c="dimmed">No spectrogram data available</Text>
        </Center>
      </Paper>
    );
  }

  const spectrogramContent = (
    <Box>
      <Group justify="space-between" mb="sm">
        <Group gap="lg">
          <Text size="sm" c="dimmed">FFT Size: <Text span fw={500} c="white">{data.fftSize}</Text></Text>
          <Text size="sm" c="dimmed">Sample Rate: <Text span fw={500} c="white">{data.sampleRate} Hz</Text></Text>
        </Group>
      </Group>
      <Box
        ref={containerRef}
        style={{ position: 'relative', width: '100%', height: 250, cursor: 'crosshair', overflow: 'hidden' }}
        onMouseMove={handleMouseMove}
        onMouseLeave={handleMouseLeave}
      >
        <SpectrogramCanvas
          data={data}
          width="100%"
          height={400}
          magnitudeThreshold={magnitudeThreshold}
        />
        {/* Y-axis labels - dynamically positioned based on Nyquist frequency */}
        <Box style={{ position: 'absolute', left: 0, top: 0, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', padding: '4px 0', pointerEvents: 'none' }}>
          {yAxisLabels.map((freq, idx) => (
            <Text key={idx} size="xs" c="dimmed">{formatFreq(freq)}</Text>
          ))}
        </Box>
        {/* Hover crosshair and tooltip */}
        {mousePos && hoverInfo && containerRef.current && (
          <>
            {/* Vertical line */}
            <Box style={{
              position: 'absolute',
              left: mousePos.x,
              top: 0,
              width: 1,
              height: '100%',
              backgroundColor: theme.colors.blue[5],
              opacity: 0.7,
              pointerEvents: 'none'
            }} />
            {/* Tooltip with smart positioning to prevent overflow */}
            <Box style={{
              position: 'absolute',
              left: mousePos.x + 10 + 120 > containerRef.current.clientWidth
                ? mousePos.x - 130  // Position to the left if would overflow right
                : mousePos.x + 10,  // Position to the right normally
              top: mousePos.y - 40 < 0
                ? mousePos.y + 10   // Position below if would overflow top
                : mousePos.y - 40,  // Position above normally
              backgroundColor: theme.colors.dark[6],
              border: `1px solid ${theme.colors.dark[4]}`,
              borderRadius: theme.radius.sm,
              padding: '4px 8px',
              pointerEvents: 'none',
              zIndex: 10,
              whiteSpace: 'nowrap'
            }}>
              <Text size="xs" c="white">Time: {hoverInfo.time}</Text>
              <Text size="xs" c="white">Nyquist: {hoverInfo.nyquistFreq}</Text>
            </Box>
          </>
        )}
      </Box>
      {/* X-axis time labels with adaptive spacing */}
      <TimeAxis durationMs={duration} />
    </Box>
  );

  return (
    <>
      <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
        <Flex direction="column" gap="md">
          <Group justify="space-between" align="center">
            <Text fw={600}>Spectrogram</Text>
            <Button
              leftSection={<IconZoomIn size={16} />}
              variant="light"
              size="sm"
              onClick={() => setModalOpened(true)}
            >
              Zoom
            </Button>
          </Group>
          {spectrogramContent}
        </Flex>
      </Paper>

      <Modal
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        title="Spectrogram Analysis"
        size="xl"
        centered
      >
        {spectrogramContent}
      </Modal>
    </>
  );
}

export default Spectrogram;

