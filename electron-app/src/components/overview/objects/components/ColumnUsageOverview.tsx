import { Box, Stack, Text, useMantineTheme } from '@mantine/core';
import { useMemo } from 'react';
import ComparisonTable, { type ComparisonRow } from '../../ComparisonTable.tsx';
import type { Mode, ObjectsColumnUsage, ObjectsOverviewDifficulty } from '../../../../Types';

/** A column's objects, their share, and a bar scaled to the difficulty's busiest column. */
function ColumnUsageValue({ usage, peak }: { usage: ObjectsColumnUsage; peak: number }) {
  const theme = useMantineTheme();

  return (
    <Stack gap="2xs" align="flex-end">
      <Text size="sm" fw={600} c={usage.totalCount === 0 ? 'dimmed' : undefined}>
        {usage.totalCount.toLocaleString()}
      </Text>
      <Text size="xs" c="dimmed">
        {usage.percentage.toFixed(1)}%
      </Text>
      <Box
        style={{
          width: 40,
          height: 4,
          borderRadius: 2,
          backgroundColor: theme.colors.dark[4],
          overflow: 'hidden',
        }}
      >
        <Box
          style={{
            width: `${peak === 0 ? 0 : (usage.totalCount / peak) * 100}%`,
            height: '100%',
            backgroundColor: theme.colors.blue[5],
          }}
        />
      </Box>
    </Stack>
  );
}

interface ColumnUsageOverviewProps {
  mode?: Mode;
  difficulties: ObjectsOverviewDifficulty[];
}

/** osu!mania objects per column, one table column per difficulty. */
export default function ColumnUsageOverview({ mode, difficulties }: ColumnUsageOverviewProps) {
  const maniaDifficulties = useMemo(
    () => difficulties.filter((difficulty) => (difficulty.columnUsage?.length ?? 0) > 0),
    [difficulties]
  );
  const starRatings = useMemo(
    () => new Map(maniaDifficulties.map((d) => [d.version, d.starRating ?? 0])),
    [maniaDifficulties]
  );

  const rows = useMemo<ComparisonRow<ObjectsOverviewDifficulty>[]>(() => {
    const usage = (d: ObjectsOverviewDifficulty) => d.columnUsage ?? [];
    const total = (d: ObjectsOverviewDifficulty) =>
      usage(d).reduce((sum, column) => sum + column.totalCount, 0);
    const peak = (d: ObjectsOverviewDifficulty) =>
      Math.max(0, ...usage(d).map((column) => column.totalCount));
    const maxColumnCount = Math.max(0, ...maniaDifficulties.map((d) => usage(d).length));

    return [
      {
        id: 'keys',
        label: 'Keys',
        value: (d) => usage(d).length,
        render: (d) => <Text size="sm">{usage(d).length}K</Text>,
      },
      {
        id: 'total',
        label: 'Total',
        value: (d) => total(d),
        render: (d) => <Text size="sm">{total(d).toLocaleString()}</Text>,
      },
      ...Array.from(
        { length: maxColumnCount },
        (_, index): ComparisonRow<ObjectsOverviewDifficulty> => ({
          id: `column-${index + 1}`,
          label: `Column ${index + 1}`,
          group: 'Columns',
          // A difficulty with fewer keys has no such column.
          value: (d) => usage(d)[index]?.totalCount ?? null,
          render: (d) =>
            index < usage(d).length ? (
              <ColumnUsageValue usage={usage(d)[index]} peak={peak(d)} />
            ) : (
              <Text size="sm" c="dimmed">
                –
              </Text>
            ),
        })
      ),
    ];
  }, [maniaDifficulties]);

  if (mode !== 'Mania' || maniaDifficulties.length === 0) {
    return null;
  }

  return (
    <ComparisonTable
      title="Column usage"
      info="Objects per column with their share of the total."
      items={maniaDifficulties}
      rows={rows}
      starRatings={starRatings}
    />
  );
}
