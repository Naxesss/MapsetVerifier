import { Box, Stack, Flex, Title, Text, Anchor, Group, Button } from '@mantine/core';
import { IconFolder, IconWorld } from '@tabler/icons-react';
import { invoke } from '@tauri-apps/api/core';
import { ApiBeatmapSetCheckResult } from '../../Types';

interface BeatmapHeaderProps {
  data?: ApiBeatmapSetCheckResult;
  beatmapFolderPath?: string;
  bgUrl?: string;
}

function BeatmapHeader({ data, beatmapFolderPath, bgUrl }: BeatmapHeaderProps) {
  return (
    <Box
      style={{
        position: 'relative',
        width: '100%',
        backgroundImage: bgUrl ? `url('${bgUrl}')` : 'none',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat',
        overflow: 'hidden',
      }}
    >
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: '100%',
          background: 'rgba(0,0,0,0.7)',
          zIndex: 1,
          pointerEvents: 'none',
        }}
      />
      <Box p="md" style={{ position: 'relative', zIndex: 2 }}>
        <Stack gap="md">
          <Flex gap="xs" direction="column">
            <Title order={2}>
              {data?.artist} - {data?.title}
            </Title>
            <Text>
              BeatmapSet by{' '} 
              <Anchor
                href={data?.creator ? `https://osu.ppy.sh/users/${data.creator}` : undefined}
                target="_blank"
                rel="noopener noreferrer"
              >
                {data?.creator}
              </Anchor>
            </Text>
          </Flex>
          <Group gap="sm" mb="xs">
            <Button
              size="xs"
              variant="default"
              onClick={async () => {
                if (!beatmapFolderPath) return;
                try {
                  await invoke('open_folder', { path: beatmapFolderPath });
                } catch (e) {
                  console.error('Failed to open folder:', e);
                  alert('Failed to open folder. See console for details.');
                }
              }}
              disabled={!beatmapFolderPath}
            >
              <IconFolder />
            </Button>
            <Button
              size="xs"
              variant="default"
              component="a"
              href={data?.beatmapSetId ? `https://osu.ppy.sh/beatmapsets/${data.beatmapSetId}` : undefined}
              target="_blank"
              rel="noopener noreferrer"
              disabled={!data?.beatmapSetId}
            >
              <IconWorld /> Open beatmap page
            </Button>
          </Group>
        </Stack>
      </Box>
    </Box>
  );
}

export default BeatmapHeader;
