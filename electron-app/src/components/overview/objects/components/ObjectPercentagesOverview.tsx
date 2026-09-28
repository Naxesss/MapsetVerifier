import { Stack, Text } from '@mantine/core';
import { useMemo } from 'react';
import { ObjectTypeEntriesPopover } from './ObjectTypeEntriesPopover.tsx';
import ComparisonTable, { type ComparisonRow } from '../../ComparisonTable.tsx';
import {
  getObjectTypeBuckets,
  OBJECT_PERCENTAGE_COLUMNS,
  resolveObjectPercentageEntries,
  resolveObjectPercentageValue,
} from '../objectPercentagesUtils.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';

/**
 * Count over its share. In a clickable cell the value brings the cell's padding, since the cell
 * drops its own to make the whole cell the click target.
 */
function PercentageTableValue({
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

interface ObjectPercentagesOverviewProps {
  mode?: Mode;
  difficulties: ObjectsOverviewDifficulty[];
}

/** Objects of each type and their share, one column per difficulty; a count opens its objects. */
export default function ObjectPercentagesOverview({
  mode,
  difficulties,
}: ObjectPercentagesOverviewProps) {
  const buckets = useMemo(
    () =>
      new Map(
        difficulties.map((d) => [d.version, mode ? getObjectTypeBuckets(d, mode) : []] as const)
      ),
    [difficulties, mode]
  );
  const starRatings = useMemo(
    () => new Map(difficulties.map((d) => [d.version, d.starRating ?? 0])),
    [difficulties]
  );

  const rows = useMemo<ComparisonRow<ObjectsOverviewDifficulty>[]>(
    () =>
      (mode ? OBJECT_PERCENTAGE_COLUMNS[mode] : []).map((column) => {
        const of = (d: ObjectsOverviewDifficulty) => buckets.get(d.version) ?? [];
        return {
          id: column.label,
          label: column.label,
          value: (d) => resolveObjectPercentageValue(of(d), column).count,
          clickable: (d) => resolveObjectPercentageEntries(of(d), column).length > 0,
          render: (d) => {
            const value = resolveObjectPercentageValue(of(d), column);
            const entries = resolveObjectPercentageEntries(of(d), column);
            return (
              <ObjectTypeEntriesPopover
                headingLabel={column.label}
                difficultyVersion={d.version}
                entries={entries}
              >
                <PercentageTableValue
                  count={value.count}
                  percentage={value.percentage}
                  padded={entries.length > 0}
                />
              </ObjectTypeEntriesPopover>
            );
          },
        };
      }),
    [buckets, mode]
  );

  if (!mode) {
    return null;
  }

  return (
    <ComparisonTable
      title="Objects overview"
      items={difficulties}
      rows={rows}
      starRatings={starRatings}
    />
  );
}
