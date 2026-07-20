import { Button, ColorSwatch, Group, Stack, useMantineTheme } from '@mantine/core';
import { memo } from 'react';
import type { SeriesConfig } from './types.ts';

type TimeSeriesLegendProps = {
  series: SeriesConfig[];
  isVisible: (seriesId: string) => boolean;
  onToggle: (seriesId: string) => void;
  onIsolate: (seriesId: string) => void;
};

function TimeSeriesLegend({ series, isVisible, onToggle, onIsolate }: TimeSeriesLegendProps) {
  const theme = useMantineTheme();
  const legendSeries = series.filter((item) => !item.hideFromLegend);

  if (legendSeries.length <= 1) {
    return null;
  }

  return (
    <Stack gap="xs" w="100%">
      <Group
        gap="xs"
        wrap="wrap"
        justify="flex-end"
        align="flex-start"
        w="100%"
        style={{ overflow: 'visible' }}
      >
        {legendSeries.map((item) => {
          const visibilityId = item.visibilityId ?? item.id;
          const visible = isVisible(visibilityId);

          return (
            <Button
              key={item.id}
              type="button"
              variant={visible ? 'subtle' : 'light'}
              color="gray"
              size="compact-sm"
              justify="flex-start"
              onClick={(event) => {
                if (event.ctrlKey || event.metaKey) {
                  onToggle(visibilityId);
                } else {
                  onIsolate(visibilityId);
                }
              }}
              aria-pressed={visible}
              title="Click to isolate, Ctrl/Cmd+click to toggle just this one"
              leftSection={
                <ColorSwatch
                  color={item.color}
                  size={12}
                  withShadow={false}
                  style={{ flexShrink: 0, marginTop: 2, opacity: visible ? 1 : 0.4 }}
                />
              }
              styles={{
                root: {
                  opacity: visible ? 1 : 0.55,
                  transition: 'opacity 120ms ease',
                  height: 'auto',
                  minHeight: 'unset',
                  maxWidth: '100%',
                  alignItems: 'flex-start',
                  paddingTop: theme.spacing.xs,
                  paddingBottom: theme.spacing.xs,
                },
                inner: {
                  flexWrap: 'wrap',
                  alignItems: 'flex-start',
                  justifyContent: 'flex-start',
                },
                label: {
                  whiteSpace: 'normal',
                  wordBreak: 'break-word',
                  lineHeight: 1.35,
                  textAlign: 'left',
                },
              }}
            >
              {item.label}
            </Button>
          );
        })}
      </Group>
    </Stack>
  );
}

export default memo(TimeSeriesLegend);
