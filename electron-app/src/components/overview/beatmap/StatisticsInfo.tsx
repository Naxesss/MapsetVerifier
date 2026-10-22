import { Text } from '@mantine/core';
import { useMemo } from 'react';
import { trimTimestamp } from '../../../utils/timestamps';
import ComparisonTable, { type ComparisonRow } from '../ComparisonTable.tsx';
import type { DifficultyStatistics } from '../../../Types';

interface StatisticsInfoProps {
  statistics: DifficultyStatistics[];
  /** Star rating per difficulty version, for the column headers. */
  starRatings: Map<string, number>;
}

const text = (value: string) => <Text size="sm">{value}</Text>;
/** A count; zero is dimmed so the counts that matter stand out. */
const count = (value: number | null) =>
  value === null ? (
    text('N/A')
  ) : (
    <Text size="sm" c={value === 0 ? 'dimmed' : undefined}>
      {value.toLocaleString()}
    </Text>
  );

function buildRows(isMania: boolean): ComparisonRow<DifficultyStatistics>[] {
  return [
    {
      id: 'circleCount',
      label: 'Circles',
      group: 'Objects',
      value: (s) => s.circleCount,
      render: (s) => count(s.circleCount),
    },
    {
      id: 'sliderCount',
      label: isMania ? 'LNs' : 'Sliders',
      group: 'Objects',
      value: (s) => (isMania ? s.holdNoteCount : s.sliderCount),
      render: (s) => count(isMania ? s.holdNoteCount : s.sliderCount),
    },
    {
      id: 'spinnerCount',
      label: 'Spinners',
      group: 'Objects',
      value: (s) => s.spinnerCount,
      render: (s) => count(s.spinnerCount),
    },
    {
      id: 'newComboCount',
      label: 'New combos',
      group: 'Misc',
      value: (s) => s.newComboCount,
      render: (s) => count(s.newComboCount),
    },
    {
      id: 'breakCount',
      label: 'Breaks',
      group: 'Misc',
      groupColours: true,
      value: (s) => s.breakCount,
      render: (s) => count(s.breakCount),
    },
    {
      id: 'uninheritedLineCount',
      label: 'Uninherited',
      group: 'Timing',
      groupColours: true,
      value: (s) => s.uninheritedLineCount,
      render: (s) => count(s.uninheritedLineCount),
    },
    {
      id: 'inheritedLineCount',
      label: 'Inherited',
      group: 'Timing',
      value: (s) => s.inheritedLineCount,
      render: (s) => count(s.inheritedLineCount),
    },
    {
      id: 'kiaiTimeMs',
      label: 'Kiai time',
      group: 'Duration',
      groupColours: true,
      value: (s) => s.kiaiTimeMs,
      render: (s) => text(trimTimestamp(s.kiaiTimeFormatted)),
    },
    {
      id: 'drainTimeMs',
      label: 'Drain time',
      group: 'Duration',
      groupColours: true,
      value: (s) => s.drainTimeMs,
      render: (s) => text(trimTimestamp(s.drainTimeFormatted)),
    },
    {
      id: 'playTimeMs',
      label: 'Play time',
      group: 'Duration',
      groupColours: true,
      value: (s) => s.playTimeMs,
      render: (s) => text(trimTimestamp(s.playTimeFormatted)),
    },
  ];
}

function StatisticsInfo({ statistics, starRatings }: StatisticsInfoProps) {
  // The table shows one mode at a time; osu!mania counts long notes where others count sliders.
  const isMania = statistics.length > 0 && statistics.every((s) => s.mode === 'Mania');
  const rows = useMemo(() => buildRows(isMania), [isMania]);

  return (
    <ComparisonTable starRatings={starRatings} title="Statistics" items={statistics} rows={rows} />
  );
}

export default StatisticsInfo;
