import { Button, ColorSwatch, Group, Stack, useMantineTheme } from '@mantine/core';
import { memo } from 'react';
import type { SeriesConfig } from './types.ts';

type TimeSeriesLegendProps = {
  series: SeriesConfig[];
  isVisible: (seriesId: string) => boolean;
  onToggle: (seriesId: string) => void;
  onSelectAll: () => void;
  onUnselectAll: () => void;
};

function TimeSeriesLegend({
  series,
  isVisible,
  onToggle,
  onSelectAll,
  onUnselectAll,
}: TimeSeriesLegendProps) {
  const theme = useMantineTheme();

  if (series.length <= 1) {
    return null;
  }

  return (
    <Stack gap="xs" w="100%">
      <Group gap="xs" wrap="wrap">
        <Button type="button" variant="subtle" color="gray" size="compact-xs" onClick={onSelectAll}>
          Select all
        </Button>
        <Button
          type="button"
          variant="subtle"
          color="gray"
          size="compact-xs"
          onClick={onUnselectAll}
        >
          Unselect all
        </Button>
      </Group>
      <Group
        gap="xs"
        wrap="wrap"
        justify="flex-end"
        align="flex-start"
        w="100%"
        style={{ overflow: 'visible' }}
      >
        {series.map((item) => {
          const visible = isVisible(item.id);

          return (
            <Button
              key={item.id}
              type="button"
              variant={visible ? 'subtle' : 'light'}
              color="gray"
              size="compact-sm"
              justify="flex-start"
              onClick={() => onToggle(item.id)}
              aria-pressed={visible}
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
