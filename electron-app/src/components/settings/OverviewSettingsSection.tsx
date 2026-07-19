import { SegmentedControl } from '@mantine/core';
import { IconChartAreaLine } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';
import type { DifficultySpikeDisplayMode } from '../../context/SettingsContext';

const DISPLAY_MODE_OPTIONS: { value: DifficultySpikeDisplayMode; label: string }[] = [
  { value: 'both', label: 'Both' },
  { value: 'starRatingOnly', label: 'Star Rating only' },
  { value: 'spikesOnly', label: 'Spikes only' },
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
        description="Whether the Star Rating overview chart shows the cumulative line, the difficulty spike overlay, or both, by default."
        control={
          <SegmentedControl
            size="xs"
            data={DISPLAY_MODE_OPTIONS}
            value={settings.difficultySpikeDisplayMode}
            onChange={(value) =>
              setSettings((prev) => ({
                ...prev,
                difficultySpikeDisplayMode: value as DifficultySpikeDisplayMode,
              }))
            }
          />
        }
      />
    </SettingsSection>
  );
}
