import { Box, Group, Progress, SimpleGrid, Stack, Text } from '@mantine/core';
import { KIND_COLORS } from './resourceFiles.ts';
import { buildBeatmapResourceUrl } from '../../../utils/buildBeatmapFolderPath.ts';
import AudioWaveformPlayer from '../../common/audio/AudioWaveformPlayer.tsx';
import { useWaveformPeaks } from '../../common/audio/useWaveformPeaks.ts';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import TimestampLink from '../../common/TimestampLink.tsx';
import type { HitSoundUsage } from '../../../Types';

interface HitSoundDetailProps {
  hitSound: HitSoundUsage;
  beatmapFolderPath: string | undefined;
}

/** A hit sound to see and listen to, and how often each difficulty uses it, from where it's first heard. */
function HitSoundDetail({ hitSound, beatmapFolderPath }: HitSoundDetailProps) {
  const url = beatmapFolderPath
    ? buildBeatmapResourceUrl(beatmapFolderPath, hitSound.fileName)
    : null;
  // Shared with the player below, so the file is read once.
  const isEmpty = useWaveformPeaks(url).data?.isEmpty ?? false;
  const maxUses = Math.max(1, ...hitSound.usagePerDifficulty.map((u) => u.usageCount));

  return (
    <>
      <SectionCard title="Sound">
        <Stack gap="sm">
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
            <StatField
              label="Length"
              value={
                hitSound.durationMs > 0
                  ? `${Math.round(hitSound.durationMs)} ms`
                  : isEmpty
                    ? '0 ms'
                    : 'Unknown'
              }
            />
            <StatField label="Uses" value={hitSound.totalUsageCount} />
          </SimpleGrid>
          {url && <AudioWaveformPlayer url={url} color={KIND_COLORS.hitSound} />}
        </Stack>
      </SectionCard>

      <SectionCard
        title="Uses per difficulty"
        info="How many times each difficulty plays this sound, and where it first does. Click a time to open it in osu!."
      >
        {hitSound.usagePerDifficulty.length === 0 ? (
          <Text size="sm" c="dimmed">
            No difficulty uses this hit sound.
          </Text>
        ) : (
          <Stack gap="sm">
            {hitSound.usagePerDifficulty.map((usage) => {
              const first = usage.timestamps[0]?.replace(/\s*-\s*$/, '');
              return (
                <Box
                  key={usage.version}
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'minmax(80px, 180px) minmax(60px, 1fr) 48px 96px',
                    gap: 'var(--mantine-spacing-sm)',
                    alignItems: 'center',
                  }}
                >
                  <Text size="sm" truncate>
                    {usage.version}
                  </Text>
                  <Progress value={(usage.usageCount / maxUses) * 100} size="sm" />
                  <Text size="sm" ta="right">
                    {usage.usageCount}
                  </Text>
                  <Group justify="flex-end">
                    {first && !first.startsWith('-') && <TimestampLink displayTimestamp={first} />}
                  </Group>
                </Box>
              );
            })}
          </Stack>
        )}
      </SectionCard>
    </>
  );
}

export default HitSoundDetail;
