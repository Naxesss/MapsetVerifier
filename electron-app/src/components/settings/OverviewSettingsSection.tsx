import { SegmentedControl } from '@mantine/core';
import { IconChartAreaLine } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';
import type { DifficultyStrainDisplayMode } from '../../context/SettingsContext';

const DISPLAY_MODE_OPTIONS: { value: DifficultyStrainDisplayMode; label: string }[] = [
  { value: 'strainOnly', label: 'Strain' },
  { value: 'starRatingOnly', label: 'SR' },
  { value: 'both', label: 'Both' },
];

export default function OverviewSettingsSection() {
  const { settings, setSettings } = useSettings();

  return (
    <SettingsSection
      icon={<IconChartAreaLine size={28} />}
      title="Overview"
      description="Controls what's displayed on the beatmap overview page."
    >
      <SettingsRow
        title="Star Rating chart"
        description="Whether the Star Rating overview chart shows the cumulative line, the difficulty strain overlay, or both, by default."
        control={
          <SegmentedControl
            size="xs"
            data={DISPLAY_MODE_OPTIONS}
            value={settings.difficultyStrainDisplayMode}
            onChange={(value) =>
              setSettings((prev) => ({
                ...prev,
                difficultyStrainDisplayMode: value as DifficultyStrainDisplayMode,
              }))
            }
          />
        }
      />
    </SettingsSection>
  );
}
