import { Button, Group, Tooltip, useMantineTheme } from '@mantine/core';
import { IconFolder, IconRefresh, IconWorld } from '@tabler/icons-react';
import { invoke } from '@tauri-apps/api/core';

interface BeatmapActionButtonsProps {
  beatmapFolderPath?: string;
  beatmapSetId?: number;
  onReparse: () => Promise<void>;
}

function BeatmapActionButtons({ beatmapFolderPath, beatmapSetId, onReparse }: BeatmapActionButtonsProps) {
  const theme = useMantineTheme();

  return (
    <Group
      p="xs"
      gap="xs"
      bg={theme.colors.dark[8]}
      style={{ borderRadius: theme.radius.md }}
    >
      <Tooltip label="Reparse the beatmap">
        <Button
          size="xs"
          variant="default"
          onClick={onReparse}
        >
          <IconRefresh />
        </Button>
      </Tooltip>
      <Button
        title="Open beatmap folder"
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
        title="Open beatmap page"
        size="xs"
        variant="default"
        component="a"
        href={beatmapSetId ? `https://osu.ppy.sh/beatmapsets/${beatmapSetId}` : undefined}
        target="_blank"
        rel="noopener noreferrer"
        disabled={!beatmapSetId}
      >
        <IconWorld />
      </Button>
    </Group>
  );
}

export default BeatmapActionButtons;

