import { ActionIcon, Button, Group, Menu, Stack, Text, Tooltip } from '@mantine/core';
import {
  IconChevronDown,
  IconDownload,
  IconFolder,
  IconMessage,
  IconRefresh,
  IconVersions,
  IconWorld,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import ShortcutLabel from './ShortcutLabel.tsx';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { useOpenExternal } from '../../hooks/useOpenExternal.ts';
import { openPathOrNotify } from '../../utils/notify.tsx';

const NOT_SUBMITTED = 'Not submitted yet';

interface OpenItemProps {
  icon: ReactNode;
  label: string;
  /** When set, the item is disabled and says why under its label. */
  disabledReason?: string;
  onClick: () => void;
}

function OpenItem({ icon, label, disabledReason, onClick }: OpenItemProps) {
  return (
    <Menu.Item leftSection={icon} disabled={!!disabledReason} onClick={onClick}>
      {disabledReason ? (
        <Stack gap={0}>
          <Text size="sm">{label}</Text>
          <Text size="xs" c="dimmed">
            {disabledReason}
          </Text>
        </Stack>
      ) : (
        label
      )}
    </Menu.Item>
  );
}

/**
 * What can be done with the mapset as a whole, on the right of its title: refresh it, and open it
 * elsewhere. The places to open it are one labelled menu rather than a row of look-alike icons,
 * and a place that isn't available says why instead of only being greyed out.
 */
function MapsetActions() {
  const { beatmapFolderPath, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const openExternal = useOpenExternal();
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const beatmapSetId = beatmapInfo?.beatmapSetId ?? undefined;
  const beatmapId = beatmapInfo?.beatmapId ?? undefined;
  const isSubmitted = !!beatmapSetId && beatmapSetId > 0;
  const hasBeatmapId = !!beatmapId && beatmapId > 0;

  // Snapshots are saved per mapset once checks run on it, so whether the folder exists is looked up
  // each time the menu opens rather than assumed.
  const snapshotFolderQuery = useQuery({
    queryKey: ['snapshot-folder', beatmapSetId],
    queryFn: async () =>
      (await window.electronAPI?.app.getSnapshotFolderPath(beatmapSetId!)) ?? null,
    enabled: isMenuOpen && beatmapSetId != null,
    staleTime: 0,
  });
  const snapshotFolder = beatmapSetId != null ? snapshotFolderQuery.data : null;

  const refresh = async () => {
    setIsRefreshing(true);
    try {
      await triggerReparse();
    } finally {
      setIsRefreshing(false);
    }
  };

  return (
    <Group gap="xs" wrap="nowrap">
      <Tooltip label={<ShortcutLabel label="Refresh mapset" keys={['F5']} />}>
        <ActionIcon
          variant="default"
          size="input-sm"
          aria-label="Refresh mapset"
          loading={isRefreshing}
          onClick={() => void refresh()}
        >
          <IconRefresh size={18} stroke={1.5} />
        </ActionIcon>
      </Tooltip>
      <Menu
        position="bottom-end"
        withinPortal
        onOpen={() => setIsMenuOpen(true)}
        onClose={() => setIsMenuOpen(false)}
      >
        <Menu.Target>
          <Button
            variant="default"
            size="sm"
            rightSection={<IconChevronDown size={16} stroke={1.5} />}
          >
            Open
          </Button>
        </Menu.Target>
        <Menu.Dropdown>
          <OpenItem
            icon={<IconFolder size={16} stroke={1.5} />}
            label="Beatmap folder"
            disabledReason={beatmapFolderPath ? undefined : 'Folder not found'}
            onClick={() => {
              if (beatmapFolderPath) {
                void openPathOrNotify(beatmapFolderPath, "Couldn't open the mapset folder.");
              }
            }}
          />
          <OpenItem
            icon={<IconVersions size={16} stroke={1.5} />}
            label="Snapshot folder"
            disabledReason={
              snapshotFolder
                ? undefined
                : snapshotFolder === undefined
                  ? 'Checking…'
                  : 'No snapshots yet'
            }
            onClick={() => {
              if (snapshotFolder) {
                void openPathOrNotify(snapshotFolder, "Couldn't open the snapshot folder.");
              }
            }}
          />
          <Menu.Divider />
          <OpenItem
            icon={<IconWorld size={16} stroke={1.5} />}
            label="Beatmap page"
            disabledReason={isSubmitted ? undefined : NOT_SUBMITTED}
            onClick={() => void openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}`)}
          />
          <OpenItem
            icon={<IconMessage size={16} stroke={1.5} />}
            label="Modding discussion"
            disabledReason={isSubmitted ? undefined : NOT_SUBMITTED}
            onClick={() =>
              void openExternal(`https://osu.ppy.sh/beatmapsets/${beatmapSetId}/discussion`)
            }
          />
          <OpenItem
            icon={<IconDownload size={16} stroke={1.5} />}
            label="osu!direct"
            disabledReason={hasBeatmapId ? undefined : NOT_SUBMITTED}
            onClick={() => void openExternal(`osu://b/${beatmapId}`)}
          />
        </Menu.Dropdown>
      </Menu>
    </Group>
  );
}

export default MapsetActions;
