import { Text, Group, Paper, useMantineTheme, Stack, SimpleGrid, Loader, Center, Badge } from '@mantine/core';
import { AreaChart } from '@mantine/charts';
import { useMemo } from 'react';
import { FrequencyAnalysisResult } from '../../Types';

interface FrequencyAnalysisProps {
  data: FrequencyAnalysisResult | undefined;
  isLoading: boolean;
}

// Convert frequency to log-spaced label for display
function formatFrequency(freq: number): string {
  if (freq >= 1000) return `${(freq / 1000).toFixed(freq >= 10000 ? 0 : 1)}k`;
  return `${Math.round(freq)}`;
}

function FrequencyAnalysis({ data, isLoading }: FrequencyAnalysisProps) {
  const theme = useMantineTheme();

  // Transform FFT data for Mantine AreaChart
  // Since Mantine doesn't support log scale, we sample at log-spaced intervals
  const chartData = useMemo(() => {
    if (!data?.fftData?.length) return [];
    const rawData = data.fftData.filter(d => d.frequencyHz >= 20);
    if (!rawData.length) return [];

    // Sample at logarithmically spaced points for better visualization
    const logMin = Math.log10(20);
    const logMax = Math.log10(Math.max(...rawData.map(d => d.frequencyHz)));
    const numPoints = 100;
    const result: { freq: string; magnitude: number }[] = [];

    for (let i = 0; i < numPoints; i++) {
      const logFreq = logMin + (logMax - logMin) * (i / (numPoints - 1));
      const targetFreq = Math.pow(10, logFreq);
      // Find closest data point
      const closest = rawData.reduce((prev, curr) =>
        Math.abs(curr.frequencyHz - targetFreq) < Math.abs(prev.frequencyHz - targetFreq) ? curr : prev
      );
      result.push({
        freq: formatFrequency(targetFreq),
        magnitude: Math.round(closest.magnitudeDb * 10) / 10,
      });
    }
    return result;
  }, [data?.fftData]);

  const yDomain = useMemo(() => {
    if (!data?.fftData?.length) return [-80, 0];
    const magnitudes = data.fftData.map(d => d.magnitudeDb);
    const minVal = Math.min(...magnitudes);
    const maxVal = Math.max(...magnitudes);
    return [Math.floor(minVal / 10) * 10, Math.ceil(maxVal / 10) * 10];
  }, [data?.fftData]);

  if (isLoading) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
        <Text fw={600} mb="sm">Frequency Analysis</Text>
        <Center h={200}><Loader size="lg" /></Center>
      </Paper>
    );
  }

  if (!data) {
    return (
      <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
        <Text fw={600} mb="sm">Frequency Analysis</Text>
        <Center h={200}><Text c="dimmed">No frequency data available</Text></Center>
      </Paper>
    );
  }

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Frequency Analysis (FFT)</Text>
        <Text size="sm" c="dimmed">Window: {data.fftWindowSize}</Text>
      </Group>
      <SimpleGrid cols={3} mb="md">
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Bass (20-250Hz)</Text>
          <Badge color="red" variant="light">{data.harmonicAnalysis?.bassEnergy?.toFixed(1) || 'N/A'} dB</Badge>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Mids (250-4kHz)</Text>
          <Badge color="green" variant="light">{data.harmonicAnalysis?.midEnergy?.toFixed(1) || 'N/A'} dB</Badge>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Highs (4k-20kHz)</Text>
          <Badge color="blue" variant="light">{data.harmonicAnalysis?.highEnergy?.toFixed(1) || 'N/A'} dB</Badge>
        </Stack>
      </SimpleGrid>
      {chartData.length > 0 ? (
        <AreaChart
          h={200}
          data={chartData}
          dataKey="freq"
          series={[{ name: 'magnitude', label: 'Magnitude', color: 'cyan.5' }]}
          curveType="monotone"
          withDots={false}
          yAxisProps={{ domain: yDomain }}
          xAxisProps={{ tickMargin: 10, interval: 'preserveStartEnd' }}
          valueFormatter={(value) => `${value} dB`}
          fillOpacity={0.6}
          gridAxis="xy"
          withGradient
        />
      ) : (
        <Center h={200}><Text c="dimmed">No FFT data available</Text></Center>
      )}
      <Text size="xs" c="dimmed" mt="xs" ta="center">Frequency (Hz) - Log-spaced sampling</Text>
      {data.detectedNotes?.length > 0 && (
        <Group gap="xs" mt="md">
          <Text size="xs" c="dimmed">Detected Notes:</Text>
          {data.detectedNotes.slice(0, 5).map((note, idx) => (
            <Badge key={idx} size="sm" variant="outline">{note.noteName}</Badge>
          ))}
        </Group>
      )}
    </Paper>
  );
}

export default FrequencyAnalysis;

