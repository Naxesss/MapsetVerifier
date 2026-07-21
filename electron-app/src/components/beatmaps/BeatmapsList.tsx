import { Flex, SegmentedControl } from '@mantine/core';
import LazerBeatmapsPanel from './LazerBeatmapsPanel.tsx';
import StableBeatmapsPanel from './StableBeatmapsPanel.tsx';
import { useSettings } from '../../context/SettingsContext';

interface Props {
  songFolder?: string;
  lazerDataDir?: string;
  lazerLookupEnabled: boolean;
  onOpenSettings: () => void;
}

export default function BeatmapsList({
  songFolder,
  lazerDataDir,
  lazerLookupEnabled,
  onOpenSettings,
}: Props) {
  const { settings, setSettings } = useSettings();
  const lookupMode = settings.beatmapLookupMode;
  const effectiveLookupMode = !lazerLookupEnabled ? 'stable' : lookupMode;
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
      {lazerLookupEnabled && (
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
