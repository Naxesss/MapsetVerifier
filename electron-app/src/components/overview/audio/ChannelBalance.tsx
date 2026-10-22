import { AreaChart } from '@mantine/charts';
import { Text, Badge, Group, Progress, Stack } from '@mantine/core';
import { useMemo } from 'react';
import { ChannelAnalysisResult, ChannelBalanceDataPoint } from '../../../Types';
import SectionCard from '../../common/SectionCard.tsx';

interface ChannelBalanceProps {
  data: ChannelAnalysisResult;
  durationMs: number;
}

function getSeverityColor(severity: string): string {
  switch (severity) {
    case 'None':
      return 'green';
    case 'Minor':
      return 'yellow';
    case 'Warning':
      return 'orange';
    case 'Severe':
      return 'red';
    default:
      return 'gray';
  }
}

function ChannelBalance({ data }: ChannelBalanceProps) {
  // Transform data for Mantine AreaChart - sample data for performance
  const chartData = useMemo(() => {
    if (!data.balanceOverTime?.length) return [];
    const rawData = data.balanceOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_: ChannelBalanceDataPoint, i: number) => i % step === 0);
    return sampled.map((point: ChannelBalanceDataPoint) => ({
      time: `${(point.timeMs / 1000).toFixed(1)}s`,
      left: Math.round(point.leftLevel * 100),
      right: Math.round(point.rightLevel * 100),
    }));
  }, [data.balanceOverTime]);

  const leftPercent = Math.round(data.leftChannelLevel * 100);
  const rightPercent = Math.round(data.rightChannelLevel * 100);

  return (
    <SectionCard
      title="Channel balance"
      actions={
        <>
          <Badge color={getSeverityColor(data.severity)}>
            {data.severity === 'None' ? 'Balanced' : data.severity}
          </Badge>
          <Badge color="gray">{data.isMono ? 'Mono' : 'Stereo'}</Badge>
        </>
      }
    >
      <Stack gap="xs" mb="md">
        <Group gap="xs">
          <Text size="sm" w={60} c="blue.4">
            Left
          </Text>
          <Progress value={leftPercent} color="blue" style={{ flex: 1 }} size="lg" />
          <Text size="sm" w={40}>
            {leftPercent}%
          </Text>
        </Group>
        <Group gap="xs">
          <Text size="sm" w={60} c="pink.4">
            Right
          </Text>
          <Progress value={rightPercent} color="pink" style={{ flex: 1 }} size="lg" />
          <Text size="sm" w={40}>
            {rightPercent}%
          </Text>
        </Group>
      </Stack>
      <Group gap="lg" mb="md">
        <Text size="sm" c="dimmed">
          Stereo width:{' '}
          <Text span fw={500} c="white">
            {(data.stereoWidth * 100).toFixed(0)}%
          </Text>
        </Text>
        <Text size="sm" c="dimmed">
          Phase:{' '}
          <Text span fw={500} c="white">
            {data.phaseCorrelation.toFixed(2)}
          </Text>
        </Text>
        {data.louderChannel !== 'Balanced' && (
          <Text size="sm" c="dimmed">
            Louder:{' '}
            <Text span fw={500} c="white">
              {data.louderChannel}
            </Text>
          </Text>
        )}
      </Group>
      {chartData.length > 0 ? (
        <AreaChart
          h={180}
          data={chartData}
          dataKey="time"
          // Recharts 3 focuses the plot SVG on click; these charts are read-only.
          areaChartProps={{ accessibilityLayer: false }}
          onMouseDownCapture={(e) => e.preventDefault()}
          series={[
            { name: 'left', label: 'Left channel', color: 'blue.6' },
            { name: 'right', label: 'Right channel', color: 'pink.6' },
          ]}
          curveType="monotone"
          withDots={false}
          yAxisProps={{ domain: [0, 100] }}
          xAxisProps={{ tickMargin: 10 }}
          valueFormatter={(value) => `${value}%`}
          fillOpacity={0.5}
          gridAxis="xy"
          withLegend
          legendProps={{ verticalAlign: 'bottom', height: 30 }}
        />
      ) : (
        <Text c="dimmed" ta="center" py="xl">
          No channel balance data available.
        </Text>
      )}
    </SectionCard>
  );
}

export default ChannelBalance;
