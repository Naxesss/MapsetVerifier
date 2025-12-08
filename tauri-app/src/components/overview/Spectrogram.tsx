import { Box, Text, Group, Paper, useMantineTheme, Loader, Center } from '@mantine/core';
import { useEffect, useRef } from 'react';
import * as d3 from 'd3';
import { SpectralAnalysisResult } from '../../Types';

interface SpectrogramProps {
  data: SpectralAnalysisResult | undefined;
  isLoading: boolean;
}

function Spectrogram({ data, isLoading }: SpectrogramProps) {
  const theme = useMantineTheme();
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    if (!canvasRef.current || !data?.spectrogramData?.length) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = canvas.width;
    const height = canvas.height;

    // Clear canvas
    ctx.fillStyle = theme.colors.dark[8];
    ctx.fillRect(0, 0, width, height);

    const frames = data.spectrogramData;
    const numBins = frames[0]?.magnitudes?.length || 0;
    if (numBins === 0) return;

    const cellWidth = width / frames.length;
    const cellHeight = height / numBins;

    // Create color scale: white (0dB) -> yellow (-20dB) -> orange (-40dB) -> red (-60dB) -> purple (-80dB) -> black (-120dB)
    const colorScale = d3.scaleLinear<string>()
      .domain([0, -20, -40, -60, -80, -120])
      .range(['#ffffff', '#ffff00', '#ff8000', '#ff0000', '#800080', '#000000'])
      .clamp(true);

    // Draw spectrogram
    frames.forEach((frame, timeIdx) => {
      frame.magnitudes.forEach((magnitude, freqIdx) => {
        const x = timeIdx * cellWidth;
        const y = height - (freqIdx + 1) * cellHeight; // Flip Y axis (low freq at bottom)
        ctx.fillStyle = colorScale(magnitude);
        ctx.fillRect(x, y, cellWidth + 1, cellHeight + 1);
      });
    });

  }, [data, theme]);

  if (isLoading) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Loader size="lg" />
        </Center>
      </Paper>
    );
  }

  if (!data) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
        <Text fw={600} mb="sm">Spectrogram</Text>
        <Center h={250}>
          <Text c="dimmed">No spectrogram data available</Text>
        </Center>
      </Paper>
    );
  }

  const duration = data.timePositions?.length ? data.timePositions[data.timePositions.length - 1] : 0;
  const nyquistFreq = data.sampleRate / 2;

  // Generate dynamic Y-axis labels based on Nyquist frequency
  const formatFreq = (hz: number) => hz >= 1000 ? `${(hz / 1000).toFixed(hz >= 10000 ? 0 : 1)}kHz` : `${hz}Hz`;
  const yAxisLabels = [
    nyquistFreq,
    Math.min(20000, nyquistFreq * 0.9),
    nyquistFreq * 0.5,
    nyquistFreq * 0.2,
    nyquistFreq * 0.05,
    20
  ].filter(f => f >= 20 && f <= nyquistFreq);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Spectrogram</Text>
        <Group gap="lg">
          <Text size="sm" c="dimmed">FFT Size: <Text span fw={500} c="white">{data.fftSize}</Text></Text>
          <Text size="sm" c="dimmed">Sample Rate: <Text span fw={500} c="white">{data.sampleRate} Hz</Text></Text>
        </Group>
      </Group>
      <Box style={{ position: 'relative', width: '100%', height: 250 }}>
        <canvas
          ref={canvasRef}
          width={800}
          height={250}
          style={{ width: '100%', height: '100%', borderRadius: theme.radius.sm }}
        />
        {/* Y-axis labels - dynamically positioned based on Nyquist frequency */}
        <Box style={{ position: 'absolute', left: 0, top: 0, height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', padding: '4px 0' }}>
          {yAxisLabels.map((freq, idx) => (
            <Text key={idx} size="xs" c="dimmed">{formatFreq(freq)}</Text>
          ))}
        </Box>
      </Box>
      <Group justify="space-between" mt="xs">
        <Text size="xs" c="dimmed">0s</Text>
        <Text size="xs" c="dimmed">{(duration / 1000).toFixed(0)}s</Text>
      </Group>
    </Paper>
  );
}

export default Spectrogram;

