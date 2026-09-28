import { AreaChart } from '@mantine/charts';
import { Box, Text, Badge, Group, SimpleGrid } from '@mantine/core';
import { useMemo } from 'react';
import { DynamicRangeResult, LoudnessDataPoint, ClippingMarker } from '../../../Types';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';

interface DynamicRangeProps {
  data: DynamicRangeResult;
  durationMs: number;
}

function getCompressionColor(severity: string): string {
  switch (severity) {
    case 'None':
      return 'green';
    case 'Light':
      return 'blue';
    case 'Moderate':
      return 'yellow';
    case 'Heavy':
      return 'red';
    default:
      return 'gray';
  }
}

function DynamicRange({ data }: DynamicRangeProps) {
  // Transform data for Mantine AreaChart - sample data for performance
  const chartData = useMemo(() => {
    if (!data.loudnessOverTime?.length) return [];
    const rawData = data.loudnessOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_: LoudnessDataPoint, i: number) => i % step === 0);
    return sampled.map((point: LoudnessDataPoint) => ({
      time: `${(point.timeMs / 1000).toFixed(1)}s`,
      rms: Math.round(point.rmsLevel * 10) / 10,
      peak: Math.round(point.peakLevel * 10) / 10,
    }));
  }, [data.loudnessOverTime]);

  const yDomain = useMemo(() => {
    if (!data.loudnessOverTime?.length) return [-60, 0];
    const minVal = Math.min(
      ...data.loudnessOverTime.map((d: LoudnessDataPoint) => Math.min(d.rmsLevel, d.peakLevel))
    );
    return [Math.floor(minVal / 10) * 10, 0];
  }, [data.loudnessOverTime]);

  return (
    <SectionCard
      title="Dynamic range"
      actions={
        <>
          <Badge color={getCompressionColor(data.compressionSeverity)}>
            {data.compressionSeverity} compression
          </Badge>
          {data.clippingDetected && (
            <Badge color="red">Clipping detected ({data.clippingCount})</Badge>
          )}
        </>
      }
    >
      <SimpleGrid cols={4} mb="md">
        <StatField label="Loudness range" value={`${data.loudnessRange.toFixed(1)} LU`} />
        <StatField label="Integrated" value={`${data.integratedLoudness.toFixed(1)} LUFS`} />
        <StatField label="True peak" value={`${data.truePeak.toFixed(1)} dBTP`} />
        <StatField label="Dynamic range" value={`${data.dynamicRange.toFixed(1)} dB`} />
      </SimpleGrid>
      {chartData.length > 0 ? (
        <AreaChart
          h={180}
          data={chartData}
          dataKey="time"
          // Recharts 3 focuses the plot SVG on click; these charts are read-only.
          areaChartProps={{ accessibilityLayer: false }}
          onMouseDownCapture={(e) => e.preventDefault()}
          series={[
            { name: 'rms', label: 'RMS level', color: 'blue.6' },
            { name: 'peak', label: 'Peak level', color: 'orange.5' },
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
          referenceLines={
            data.clippingDetected ? [{ y: 0, label: 'Clipping', color: 'red.5' }] : []
          }
        />
      ) : (
        <Text c="dimmed" ta="center" py="xl">
          No loudness data available.
        </Text>
      )}
      {data.clippingDetected && data.clippingMarkers?.length > 0 && (
        <Group gap="md" mt="xs">
          <Group gap="xs">
            <Box w={2} h={12} bg="red.5" />
            <Text size="xs" c="dimmed">
              Clipping at:{' '}
              {data.clippingMarkers
                .slice(0, 5)
                .map((m: ClippingMarker) => `${(m.timeMs / 1000).toFixed(1)}s`)
                .join(', ')}
              {data.clippingMarkers.length > 5 ? 'â€¦' : ''}
            </Text>
          </Group>
        </Group>
      )}
    </SectionCard>
  );
}

export default DynamicRange;
