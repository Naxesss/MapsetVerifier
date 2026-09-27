import { Button, Group, Tooltip, useMantineTheme } from '@mantine/core';
import {
  IconFolder,
  IconLink,
  IconMessage,
  IconRefresh,
  IconVersions,
  IconWorld,
} from '@tabler/icons-react';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';
import { notifyError, openPathOrNotify } from '../../utils/notify.tsx';
import type { ReactNode } from 'react';

export type SnapshotFolderTarget = {
  beatmapSetId: number;
  subfolder: string;
};

interface BeatmapActionButtonsProps {
  beatmapFolderPath?: string;
  beatmapSetId?: number;
  beatmapId?: number;
  onReparse: () => Promise<void>;
  /** Undefined hides the button; null disables it on the snapshots page. */
  snapshotFolder?: SnapshotFolderTarget | null;
}

const NOT_SUBMITTED = 'Not submitted yet';

interface HeaderActionProps {
  label: string;
  /** When set, the action is disabled and the tooltip explains why. */
  disabledReason?: string;
  onClick: () => void | Promise<void>;
  children: ReactNode;
}

/**
 * Disabled buttons don't receive hover, so a disabled action is rendered with `data-disabled`
 * instead; that keeps its tooltip working to explain why it can't be used.
 */
function HeaderAction({ label, disabledReason, onClick, children }: HeaderActionProps) {
  const disabled = !!disabledReason;

  return (
    <Tooltip label={disabled ? `${label} (${disabledReason.toLowerCase()})` : label}>
      <Button
        size="xs"
        variant="default"
        type="button"
        aria-label={label}
        data-disabled={disabled || undefined}
        aria-disabled={disabled || undefined}
        onClick={(event) => {
          if (disabled) {
            event.preventDefault();
            return;
          }
          void onClick();
        }}
      >
        {children}
      </Button>
    </Tooltip>
  );
}

function BeatmapActionButtons({
  beatmapFolderPath,
  beatmapSetId,
  beatmapId,
  onReparse,
  snapshotFolder,
}: BeatmapActionButtonsProps) {
  const theme = useMantineTheme();
  const openExternal = useOpenExternal();

  const isSubmitted = !!beatmapSetId && beatmapSetId > 0;
  const hasBeatmapId = !!beatmapId && beatmapId > 0;

  return (
    <Group
      p="xs"
      gap="xs"
      w="fit-content"
      bg={theme.colors.dark[8]}
      style={{ borderRadius: theme.radius.md }}
    >
      <HeaderAction label="Refresh mapset (F5)" onClick={onReparse}>
        <IconRefresh />
      </HeaderAction>
      <HeaderAction
        label="Open mapset folder"
        disabledReason={beatmapFolderPath ? undefined : 'Folder not found'}
        onClick={() =>
          beatmapFolderPath
            ? openPathOrNotify(beatmapFolderPath, "Couldn't open the mapset folder.")
            : undefined
        }
      >
        <IconFolder />
      </HeaderAction>
      <HeaderAction
        label="Open mapset page"
        disabledReason={isSubmitted ? undefined : NOT_SUBMITTED}
        onClick={() => openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}`)}
      >
        <IconWorld />
      </HeaderAction>
      <HeaderAction
        label="Open modding page"
        disabledReason={isSubmitted ? undefined : NOT_SUBMITTED}
        onClick={() => openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}/discussion`)}
      >
        <IconMessage />
      </HeaderAction>
      <HeaderAction
        label="Open with osu!direct"
        disabledReason={hasBeatmapId ? undefined : NOT_SUBMITTED}
        onClick={() => openExternal(`osu://b/${beatmapId}`)}
      >
        <IconLink />
      </HeaderAction>
      {/* Page-specific actions go last so the shared ones keep their place across pages. */}
      {snapshotFolder !== undefined && (
        <HeaderAction
          label="Open snapshot folder"
          disabledReason={snapshotFolder ? undefined : 'No snapshot available'}
          onClick={async () => {
            if (!snapshotFolder) return;
            const failureMessage = "Couldn't open the snapshot folder.";
            try {
              const folderPath = await window.electronAPI?.app.getSnapshotFolderPath(
                snapshotFolder.beatmapSetId,
                snapshotFolder.subfolder
              );
              if (folderPath) await openPathOrNotify(folderPath, failureMessage);
            } catch (e) {
              console.error(failureMessage, e);
              notifyError(failureMessage);
            }
          }}
        >
          <IconVersions />
        </HeaderAction>
      )}
    </Group>
  );
}

export default BeatmapActionButtons;
