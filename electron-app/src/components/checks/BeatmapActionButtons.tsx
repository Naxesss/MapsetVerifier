import { Button, Group, Tooltip, useMantineTheme } from '@mantine/core';
import { IconFolder, IconMessage, IconRefresh, IconWorld } from '@tabler/icons-react';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';

interface BeatmapActionButtonsProps {
  beatmapFolderPath?: string;
  beatmapSetId?: number;
  onReparse: () => Promise<void>;
}

function BeatmapActionButtons({
  beatmapFolderPath,
  beatmapSetId,
  onReparse,
}: BeatmapActionButtonsProps) {
  const theme = useMantineTheme();
  const openExternal = useOpenExternal();

  return (
    <Group p="xs" gap="xs" bg={theme.colors.dark[8]} style={{ borderRadius: theme.radius.md }}>
      <Tooltip label="Refresh beatmap (F5)">
        <Button size="xs" variant="default" onClick={onReparse}>
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
          type="button"
          onClick={async () => {
            if (!beatmapSetId) return;
            try {
              await openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}`);
            } catch (e) {
              console.error('Failed to open beatmap page:', e);
              alert('Failed to open beatmap page. See console for details.');
            }
          }}
          disabled={!beatmapSetId}
        >
          <IconWorld />
        </Button>
      </Tooltip>
      <Tooltip label="Open modding page">
        <Button
          size="xs"
          variant="default"
          type="button"
          onClick={async () => {
            if (!beatmapSetId) return;
            try {
              await openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}/discussion`);
            } catch (e) {
              console.error('Failed to open modding page:', e);
              alert('Failed to open modding page. See console for details.');
            }
          }}
          disabled={!beatmapSetId}
        >
          <IconMessage />
        </Button>
      </Tooltip>
    </Group>
  );
}

export default BeatmapActionButtons;
