import { Paper, Stack, Text } from '@mantine/core';
import { memo } from 'react';
import { formatAxisMetricValue } from './sampleFormat.ts';
import { formatEditorTimestamp } from '../../overview/objects/timelineUtils.ts';
import type { PeakHoverState } from './types.ts';

type TimeSeriesHoverTooltipProps = {
  hover: PeakHoverState;
  valueSuffix?: string;
  showTimestamp?: boolean;
  /** Plain stack only (e.g. inside floating panel). */
  embedded?: boolean;
};

function TimeSeriesHoverTooltip({
  hover,
  valueSuffix,
  showTimestamp = true,
  embedded = false,
}: TimeSeriesHoverTooltipProps) {
  const content = (
    <Stack gap={4}>
      {showTimestamp ? (
        <Text size="xs" fw={700}>
          {formatEditorTimestamp(hover.timeMs)}
        </Text>
      ) : null}
      {hover.values.length > 0 ? (
        hover.values.map((entry) => (
          <Text key={entry.seriesId} size="xs" c="dimmed" style={{ lineHeight: 1.35 }}>
            <Text span fw={600} style={{ color: entry.color }}>
              {entry.label}
            </Text>
            {': '}
            {formatAxisMetricValue(entry.value, entry.valueSuffix ?? valueSuffix)}
            {entry.secondaryValue !== undefined
              ? ` (${formatAxisMetricValue(entry.secondaryValue, entry.secondaryValueSuffix)})`
              : null}
          </Text>
        ))
      ) : (
        <Text size="xs" c="dimmed">
          No values at this sample
        </Text>
      )}
    </Stack>
  );

  if (embedded) {
    return content;
  }

  return (
    <Paper
      shadow="md"
      p="xs"
      radius="sm"
      withBorder
      style={{
        pointerEvents: 'none',
        width: 'max-content',
        maxWidth: 360,
        flexShrink: 0,
        background: 'var(--mantine-color-dark-6)',
      }}
    >
      {content}
    </Paper>
  );
}

export default memo(TimeSeriesHoverTooltip);
