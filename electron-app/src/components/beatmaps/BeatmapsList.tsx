import { Flex, SegmentedControl } from '@mantine/core';
import LazerBeatmapsPanel from './LazerBeatmapsPanel.tsx';
import StableBeatmapsPanel from './StableBeatmapsPanel.tsx';
import { useSettings } from '../../context/SettingsContext';

interface Props {
  songFolder?: string;
  lazerDataDir?: string;
  onOpenSettings: () => void;
}

export default function BeatmapsList({ songFolder, lazerDataDir, onOpenSettings }: Props) {
  const { settings, setSettings } = useSettings();
  const viewMode = settings.beatmapViewMode;
  const showModeSwitch = viewMode === 'both';
  const effectiveLookupMode = showModeSwitch ? settings.beatmapLookupMode : viewMode;
  const stableEnabled = effectiveLookupMode === 'stable';

  return (
    <Flex
      direction="column"
      w="100%"
      style={{
        overflow: 'hidden',
        position: 'relative',
      }}
    >
      {showModeSwitch && (
        <Flex direction="column" gap="sm" p="xs">
          <SegmentedControl
            fullWidth
            data={[
              { label: 'osu!(stable)', value: 'stable' },
              { label: 'osu!(lazer)', value: 'lazer' },
            ]}
            value={effectiveLookupMode}
            onChange={(value) =>
              setSettings((prev) => ({
                ...prev,
                beatmapLookupMode: value as 'stable' | 'lazer',
              }))
            }
          />
        </Flex>
      )}
      {stableEnabled ? (
        <StableBeatmapsPanel songFolder={songFolder} onOpenSettings={onOpenSettings} />
      ) : (
        <LazerBeatmapsPanel lazerDataDir={lazerDataDir} onOpenSettings={onOpenSettings} />
      )}
    </Flex>
  );
}
