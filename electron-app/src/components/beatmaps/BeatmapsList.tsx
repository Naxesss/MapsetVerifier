import {
  Flex,
  SegmentedControl,
} from '@mantine/core';
import { useEffect, useState } from 'react';
import LazerBeatmapsPanel from './LazerBeatmapsPanel.tsx';
import StableBeatmapsPanel from './StableBeatmapsPanel.tsx';

interface Props {
  songFolder?: string;
  lazerLookupEnabled: boolean;
  onOpenSettings: () => void;
}

export default function BeatmapsList({ songFolder, lazerLookupEnabled, onOpenSettings }: Props) {
  const [lookupMode, setLookupMode] = useState<'stable' | 'lazer'>('stable');
  const stableEnabled = lookupMode === 'stable';

  useEffect(() => {
    if (!lazerLookupEnabled && lookupMode === 'lazer') {
      setLookupMode('stable');
    }
  }, [lazerLookupEnabled, lookupMode]);

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
            value={lookupMode}
            onChange={(value) => setLookupMode(value as 'stable' | 'lazer')}
          />
        </Flex>
      )}
      {stableEnabled ? <StableBeatmapsPanel songFolder={songFolder} onOpenSettings={onOpenSettings} /> : <LazerBeatmapsPanel />}
    </Flex>
  );
}
