import { Button, Group, Tooltip, useMantineTheme } from '@mantine/core';
import { IconFolder, IconMessage, IconRefresh, IconVersions, IconWorld } from '@tabler/icons-react';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';

export type SnapshotFolderTarget = {
  beatmapSetId: number;
  subfolder: string;
};

interface BeatmapActionButtonsProps {
  beatmapFolderPath?: string;
  beatmapSetId?: number;
  onReparse: () => Promise<void>;
  /** Undefined hides the button; null disables it on the snapshots page. */
  snapshotFolder?: SnapshotFolderTarget | null;
}

async function openFolderPath(folderPath: string) {
  const err = await window.electronAPI?.shell.openPath(folderPath);
  if (err) throw new Error(err);
}

function BeatmapActionButtons({
  beatmapFolderPath,
  beatmapSetId,
  onReparse,
  snapshotFolder,
}: BeatmapActionButtonsProps) {
  const theme = useMantineTheme();
  const openExternal = useOpenExternal();
  const showSnapshotFolderButton = snapshotFolder !== undefined;

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
              await openFolderPath(beatmapFolderPath);
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
      {showSnapshotFolderButton && (
        <Tooltip label="Open snapshot folder">
          <Button
            size="xs"
            variant="default"
            onClick={async () => {
              if (!snapshotFolder) return;
              try {
                const folderPath = await window.electronAPI?.app.getSnapshotFolderPath(
                  snapshotFolder.beatmapSetId,
                  snapshotFolder.subfolder
                );
                if (!folderPath) return;
                await openFolderPath(folderPath);
              } catch (e) {
                console.error('Failed to open snapshot folder:', e);
                alert('Failed to open snapshot folder. See console for details.');
              }
            }}
            disabled={!snapshotFolder}
          >
            <IconVersions />
          </Button>
        </Tooltip>
      )}
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
