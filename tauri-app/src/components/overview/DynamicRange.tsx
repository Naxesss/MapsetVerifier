import { Box, Text, Badge, Group, Paper, useMantineTheme, Stack, SimpleGrid } from '@mantine/core';
import { AreaChart } from '@mantine/charts';
import { useMemo } from 'react';
import { DynamicRangeResult } from '../../Types';

interface DynamicRangeProps {
  data: DynamicRangeResult;
}

function getCompressionColor(severity: string): string {
  switch (severity) {
    case 'None': return 'green';
    case 'Light': return 'blue';
    case 'Moderate': return 'yellow';
    case 'Heavy': return 'red';
    default: return 'gray';
  }
}

function DynamicRange({ data }: DynamicRangeProps) {
  const theme = useMantineTheme();

  // Transform data for Mantine AreaChart - sample data for performance
  const chartData = useMemo(() => {
    if (!data.loudnessOverTime?.length) return [];
    const rawData = data.loudnessOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_, i) => i % step === 0);
    return sampled.map(point => ({
      time: `${(point.timeMs / 1000).toFixed(1)}s`,
      rms: Math.round(point.rmsLevel * 10) / 10,
      peak: Math.round(point.peakLevel * 10) / 10,
    }));
  }, [data.loudnessOverTime]);

  const yDomain = useMemo(() => {
    if (!data.loudnessOverTime?.length) return [-60, 0];
    const minVal = Math.min(
      ...data.loudnessOverTime.map(d => Math.min(d.rmsLevel, d.peakLevel))
    );
    return [Math.floor(minVal / 10) * 10, 0];
  }, [data.loudnessOverTime]);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Dynamic Range Analysis</Text>
        <Group gap="xs">
          <Badge color={getCompressionColor(data.compressionSeverity)} variant="light">
            {data.compressionSeverity} Compression
          </Badge>
          {data.clippingDetected && (
            <Badge color="red" variant="filled">Clipping Detected ({data.clippingCount})</Badge>
          )}
        </Group>
      </Group>
      <SimpleGrid cols={4} mb="md">
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Loudness Range</Text>
          <Text fw={600}>{data.loudnessRange.toFixed(1)} LU</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Integrated</Text>
          <Text fw={600}>{data.integratedLoudness.toFixed(1)} LUFS</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">True Peak</Text>
          <Text fw={600}>{data.truePeak.toFixed(1)} dBTP</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Dynamic Range</Text>
          <Text fw={600}>{data.dynamicRange.toFixed(1)} dB</Text>
        </Stack>
      </SimpleGrid>
      {chartData.length > 0 ? (
        <AreaChart
          h={180}
          data={chartData}
          dataKey="time"
          series={[
            { name: 'rms', label: 'RMS Level', color: 'blue.6' },
            { name: 'peak', label: 'Peak Level', color: 'orange.5' },
          ]}
          curveType="monotone"
          withDots={false}
          yAxisProps={{ domain: yDomain }}
          xAxisProps={{ tickMargin: 10 }}
          valueFormatter={(value) => `${value} dB`}
          fillOpacity={0.4}
          gridAxis="xy"
          withLegend
          legendProps={{ verticalAlign: 'bottom', height: 30 }}
          referenceLines={data.clippingDetected ? [{ y: 0, label: 'Clipping', color: 'red.5' }] : []}
        />
      ) : (
        <Text c="dimmed" ta="center" py="xl">No loudness data available</Text>
      )}
      {data.clippingDetected && data.clippingMarkers?.length > 0 && (
        <Group gap="md" mt="xs">
          <Group gap={4}><Box w={2} h={12} bg="red.5" /><Text size="xs" c="dimmed">Clipping at: {data.clippingMarkers.slice(0, 5).map(m => `${(m.timeMs / 1000).toFixed(1)}s`).join(', ')}{data.clippingMarkers.length > 5 ? '...' : ''}</Text></Group>
        </Group>
      )}
    </Paper>
  );
}

export default DynamicRange;

