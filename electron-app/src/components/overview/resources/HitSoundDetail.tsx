import { Box, Button, Group, Progress, SimpleGrid, Stack, Text } from '@mantine/core';
import { IconPlayerPlayFilled, IconPlayerStopFilled } from '@tabler/icons-react';
import { usePreviewSound } from './usePreviewSound.ts';
import { buildBeatmapResourceUrl } from '../../../utils/buildBeatmapFolderPath.ts';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import TimestampLink from '../../common/TimestampLink.tsx';
import type { HitSoundUsage } from '../../../Types';

interface HitSoundDetailProps {
  hitSound: HitSoundUsage;
  beatmapFolderPath: string | undefined;
}

/** A hit sound to listen to, and how often each difficulty uses it, from where it's first heard. */
function HitSoundDetail({ hitSound, beatmapFolderPath }: HitSoundDetailProps) {
  const sound = usePreviewSound();
  const url = beatmapFolderPath
    ? buildBeatmapResourceUrl(beatmapFolderPath, hitSound.fileName)
    : null;
  const isPlaying = !!url && sound.playingUrl === url;
  const maxUses = Math.max(1, ...hitSound.usagePerDifficulty.map((u) => u.usageCount));

  return (
    <>
      <SectionCard
        title="Sound"
        actions={
          url && (
            <Button
              size="xs"
              variant="light"
              leftSection={
                isPlaying ? <IconPlayerStopFilled size={14} /> : <IconPlayerPlayFilled size={14} />
              }
              onClick={() => (isPlaying ? sound.stop() : sound.play(url))}
            >
              {isPlaying ? 'Stop' : 'Play'}
            </Button>
          )
        }
      >
        <Stack gap="sm">
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
            <StatField label="Format" value={hitSound.format || 'Unknown'} />
            <StatField
              label="Length"
              value={hitSound.durationMs > 0 ? `${Math.round(hitSound.durationMs)} ms` : 'Unknown'}
            />
            <StatField label="File size" value={hitSound.fileSizeFormatted || 'Unknown'} />
            <StatField label="Uses" value={hitSound.totalUsageCount} />
          </SimpleGrid>
          {url && sound.failedUrl === url && (
            <Text size="sm" c="dimmed">
              This sound can&apos;t be played here.
            </Text>
          )}
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
