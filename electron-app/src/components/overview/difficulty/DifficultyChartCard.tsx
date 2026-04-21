import { Box, Paper, SimpleGrid, Stack, Text, useMantineTheme } from '@mantine/core';
import { FilterableLineChart } from '../../common/FilterableLineChart.tsx';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type { ChartDefinition } from './difficultyChartModel.ts';

function formatChartDuration(durationMs: number) {
  return formatChartTime(Math.max(0, Math.round(durationMs / 1000)));
}

function formatSeconds(seconds: number) {
  return formatChartTime(Math.max(0, Math.round(seconds)));
}

function formatStrainResolution(msPerPeak: number): string {
  const s = msPerPeak / 1000;
  const trimmed = s.toFixed(3).replace(/\.?0+$/, '');
  return `${trimmed}s`;
}

function MetricStat({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <Text size="xs" c="dimmed">{label}</Text>
      <Text fw={600}>{value}</Text>
    </Stack>
  );
}

export function DifficultyChartCard({ chart }: { chart: ChartDefinition }) {
  const theme = useMantineTheme();

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]} style={{ overflow: 'visible' }}>
      <Stack gap="sm">
        <Text fw={600}>{chart.title}</Text>

        <SimpleGrid cols={4} spacing="md">
          <MetricStat label="Peak" value={`${chart.maxValue.toFixed(2)}${chart.valueSuffix ?? ''}`} />
          <MetricStat label="Peak at" value={formatSeconds(chart.peakTimeSeconds)} />
          <MetricStat label="Duration" value={formatChartDuration(chart.durationMs)} />
          <MetricStat label="Resolution" value={formatStrainResolution(chart.msPerPeak)} />
        </SimpleGrid>

        {chart.data.length > 0 ? (
          <Box pos="relative" style={{ overflow: 'visible' }}>
            <FilterableLineChart
              h={200}
              data={chart.data}
              dataKey="timeMs"
              durationMs={chart.durationMs}
              series={chart.series.map((series) => ({
                name: series.key,
                label: series.label,
                color: series.color,
              }))}
              curveType="linear"
              strokeWidth={2.5}
              withDots={false}
              gridAxis="xy"
              xAxisProps={{ interval: 'preserveStartEnd' }}
              legendProps={{
                verticalAlign: 'bottom',
                wrapperStyle: {
                  overflow: 'visible',
                  width: '100%',
                  maxWidth: '100%',
                },
              }}
              styles={{
                legend: {
                  height: 'auto',
                  minHeight: 'unset',
                  alignItems: 'flex-start',
                  justifyContent: 'flex-end',
                },
                legendItem: {
                  alignItems: 'flex-start',
                  maxWidth: '100%',
                },
                legendItemName: {
                  whiteSpace: 'normal',
                  wordBreak: 'break-word',
                  lineHeight: 1.35,
                },
              }}
              valueFormatter={(value) => `${Number(value).toFixed(2)}${chart.valueSuffix ?? ''}`}
            />
          </Box>
        ) : (
          <Text c="dimmed" ta="center" py="xl">No chart data available</Text>
        )}
      </Stack>
    </Paper>
  );
}
