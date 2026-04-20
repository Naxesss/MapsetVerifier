import { Button, Group, Tooltip, useMantineTheme } from '@mantine/core';
import { IconFolder, IconRefresh, IconWorld } from '@tabler/icons-react';

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
      <Tooltip label="Open beatmap folder">
        <Button
          size="xs"
          variant="default"
          onClick={async () => {
            if (!beatmapFolderPath) return;
            try {
              const err = await window.electronAPI?.shell.openPath(beatmapFolderPath);
              if (err) throw new Error(err);
            } catch (e) {
              console.error('Failed to open folder:', e);
              alert('Failed to open folder. See console for details.');
            }
          }}
          disabled={!beatmapFolderPath}
        >
          <IconFolder />
        </Button>
      </Tooltip>
      <Tooltip label="Open beatmap page">
        <Button
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
      </Tooltip>
    </Group>
  );
}

export default BeatmapActionButtons;

