import { Badge, Stack, Text } from '@mantine/core';
import { useMemo } from 'react';
import { EdgeTimesPopover } from './EdgeTimesPopover.tsx';
import ComparisonTable, { type ComparisonRow } from '../../ComparisonTable.tsx';
import { buildRoundedEdgePartNameMap, getSnappingColumns } from '../timelineUtils.ts';
import type { ObjectsOverviewDifficulty } from '../../../../Types';

/**
 * Count over its share. In a clickable cell the value brings the cell's padding, since the cell
 * drops its own to make the whole cell the click target.
 */
function SnappingTableValue({
  count,
  percentage,
  padded,
}: {
  count: number;
  percentage: number;
  padded: boolean;
}) {
  return (
    <Stack gap={0} align="flex-end" px={padded ? 6 : 0} py={padded ? 'sm' : 0}>
      <Text size="sm" fw={600} c={count === 0 ? 'dimmed' : undefined}>
        {count.toLocaleString()}
      </Text>
      <Text size="xs" c="dimmed">
        {percentage.toFixed(1)}%
      </Text>
    </Stack>
  );
}

const count = (value: number) => (
  <Text size="sm" c={value === 0 ? 'dimmed' : undefined}>
    {value.toLocaleString()}
  </Text>
);

interface SnappingsOverviewProps {
  difficulties: ObjectsOverviewDifficulty[];
}

/** Objects per snap divisor, one column per difficulty; a count opens its timestamps. */
export default function SnappingsOverview({ difficulties }: SnappingsOverviewProps) {
  const partNames = useMemo(
    () => new Map(difficulties.map((d) => [d.version, buildRoundedEdgePartNameMap(d)])),
    [difficulties]
  );
  const starRatings = useMemo(
    () => new Map(difficulties.map((d) => [d.version, d.starRating ?? 0])),
    [difficulties]
  );

  const rows = useMemo<ComparisonRow<ObjectsOverviewDifficulty>[]>(() => {
    const snapTimes = (d: ObjectsOverviewDifficulty, label: string) =>
      d.snappings.find((bucket) => bucket.label === label);

    return [
      {
        id: 'objects',
        label: 'Objects',
        value: (d) => d.objectCount,
        render: (d) => count(d.objectCount),
      },
      {
        id: 'edges',
        label: 'Edges',
        value: (d) => d.edgeCount,
        render: (d) => count(d.edgeCount),
      },
      ...getSnappingColumns(difficulties).map(
        (column): ComparisonRow<ObjectsOverviewDifficulty> => ({
          id: `snap-${column.label}`,
          label: column.label,
          group: 'Snap divisors',
          value: (d) => snapTimes(d, column.label)?.count ?? 0,
          clickable: (d) => (snapTimes(d, column.label)?.edgeTimesMs?.length ?? 0) > 0,
          render: (d) => {
            const bucket = snapTimes(d, column.label);
            const timesMs = bucket?.edgeTimesMs ?? [];
            return (
              <EdgeTimesPopover
                headingLabel={`${column.label} snaps`}
                difficultyVersion={d.version}
                timesMs={timesMs}
                roundedEdgePartNameMap={partNames.get(d.version)}
              >
                <SnappingTableValue
                  count={bucket?.count ?? 0}
                  percentage={bucket?.percentage ?? 0}
                  padded={timesMs.length > 0}
                />
              </EdgeTimesPopover>
            );
          },
        })
      ),
      {
        id: 'unsnapped',
        label: 'Unsnapped',
        value: (d) => d.unsnappedCount,
        clickable: (d) => (d.unsnappedEdgeTimesMs?.length ?? 0) > 0,
        render: (d) => {
          const timesMs = d.unsnappedEdgeTimesMs ?? [];
          const badge = (
            <Badge color="gray">
              {d.unsnappedCount.toLocaleString()} ({d.unsnappedPercentage.toFixed(1)}%)
            </Badge>
          );
          return timesMs.length > 0 ? (
            <EdgeTimesPopover
              headingLabel="Unsnapped objects"
              difficultyVersion={d.version}
              timesMs={timesMs}
              roundedEdgePartNameMap={partNames.get(d.version)}
            >
              <Stack align="flex-end" px={6} py="sm">
                {badge}
              </Stack>
            </EdgeTimesPopover>
          ) : (
            badge
          );
        },
      },
    ];
  }, [difficulties, partNames]);

  return (
    <ComparisonTable
      title="Snapping overview"
      info="Click a count to see all timestamps for that snapping."
      items={difficulties}
      rows={rows}
      starRatings={starRatings}
    />
  );
}
