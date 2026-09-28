import { Text } from '@mantine/core';
import ComparisonTable, { type ComparisonRow } from '../ComparisonTable.tsx';
import type { DifficultyDifficultySettings } from '../../../Types';

interface DifficultySettingsInfoProps {
  difficultySettings: DifficultyDifficultySettings[];
  /** Star rating per difficulty version, for the column headers. */
  starRatings: Map<string, number>;
}

const isMania = (settings: DifficultyDifficultySettings) => settings.mode === 'Mania';
const text = (value: string) => <Text size="sm">{value}</Text>;

// A null value means the setting doesn't apply to the mode (e.g. approach rate in osu!mania), so
// the row is left out.
const ROWS: ComparisonRow<DifficultyDifficultySettings>[] = [
  {
    id: 'hpDrain',
    label: 'HP',
    value: (s) => s.hpDrain,
    render: (s) => text(s.hpDrain.toFixed(1)),
  },
  {
    id: 'circleSize',
    label: 'CS',
    value: (s) => (s.mode === 'Taiko' ? null : s.circleSize),
    render: (s) => text(s.circleSize ?? 'N/A'),
  },
  {
    id: 'overallDifficulty',
    label: 'OD',
    value: (s) => s.overallDifficulty,
    render: (s) => text(s.overallDifficulty.toFixed(1)),
  },
  {
    id: 'approachRate',
    label: 'AR',
    value: (s) => (isMania(s) ? null : s.approachRate),
    render: (s) => text(s.approachRate ?? 'N/A'),
  },
  {
    id: 'sliderTickRate',
    label: 'Tick rate',
    groupColours: true,
    value: (s) => (isMania(s) ? null : s.sliderTickRate),
    render: (s) => text(s.sliderTickRate ?? 'N/A'),
  },
  {
    id: 'sliderVelocity',
    label: 'SV',
    value: (s) => (isMania(s) ? null : s.sliderVelocity),
    render: (s) => text(s.sliderVelocity ? `${s.sliderVelocity}x` : 'N/A'),
  },
];

function DifficultySettingsInfo({ difficultySettings, starRatings }: DifficultySettingsInfoProps) {
  return (
    <ComparisonTable
      starRatings={starRatings}
      title="Difficulty settings"
      items={difficultySettings}
      rows={ROWS}
    />
  );
}

export default DifficultySettingsInfo;
