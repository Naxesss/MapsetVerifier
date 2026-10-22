import { ActionIcon, Alert, Anchor, Button, Group, Text, Tooltip } from '@mantine/core';
import {
  IconAlertCircle,
  IconFolderOff,
  IconPin,
  IconPinFilled,
  IconRefresh,
  IconSearchOff,
  IconSettings,
} from '@tabler/icons-react';
import EmptyState from '../common/EmptyState.tsx';
import SearchInput from '../common/SearchInput.tsx';

/*
 * The pieces the stable and lazer mapset lists share: the search row, why a list is empty, a load
 * error, the end of the list, and the missing Songs folder notice.
 */

interface MapsetListToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  /** Shows the bookmark filter; off while bookmarks are disabled in Settings. */
  bookmarksEnabled: boolean;
  bookmarkedOnly: boolean;
  onToggleBookmarkedOnly: () => void;
  onRefresh: () => void;
}

export function MapsetListToolbar({
  search,
  onSearchChange,
  bookmarksEnabled,
  bookmarkedOnly,
  onToggleBookmarkedOnly,
  onRefresh,
}: MapsetListToolbarProps) {
  const bookmarkLabel = bookmarkedOnly ? 'Show all mapsets' : 'Show bookmarked only';

  return (
    <Group gap="sm" wrap="nowrap">
      <SearchInput
        style={{ flex: 1, minWidth: 0 }}
        placeholder="Search mapsets…"
        hint="Searches title, artist, mapper and IDs."
        value={search}
        onChange={onSearchChange}
      />
      {bookmarksEnabled && (
        <Tooltip label={bookmarkLabel}>
          <ActionIcon
            variant={bookmarkedOnly ? 'light' : 'default'}
            color="yellow"
            size="input-sm"
            aria-label={bookmarkLabel}
            aria-pressed={bookmarkedOnly}
            onClick={onToggleBookmarkedOnly}
          >
            {bookmarkedOnly ? <IconPinFilled size={18} /> : <IconPin size={18} stroke={1.5} />}
          </ActionIcon>
        </Tooltip>
      )}
      <Tooltip label="Refresh mapset list">
        <ActionIcon
          variant="default"
          size="input-sm"
          aria-label="Refresh mapset list"
          onClick={onRefresh}
        >
          <IconRefresh size={18} stroke={1.5} />
        </ActionIcon>
      </Tooltip>
    </Group>
  );
}

interface MapsetListEmptyProps {
  /** The search that found nothing, if any. */
  search: string;
  /** Only bookmarked mapsets are shown. */
  bookmarkedOnly: boolean;
  /** The bookmark filter is on, but nothing is bookmarked yet. */
  noBookmarks: boolean;
  /** Where the mapsets come from, e.g. "osu! Songs folder". */
  libraryName: string;
}

/** Why the list shows no mapsets. */
export function MapsetListEmpty({
  search,
  bookmarkedOnly,
  noBookmarks,
  libraryName,
}: MapsetListEmptyProps) {
  if (noBookmarks) {
    return (
      <EmptyState
        icon={IconPin}
        title="No bookmarks yet"
        description="Pin a mapset in the list to find it here quickly."
      />
    );
  }

  if (search) {
    return (
      <EmptyState
        icon={IconSearchOff}
        title={bookmarkedOnly ? 'No bookmarked mapsets match' : 'No mapsets match your search'}
      />
    );
  }

  if (bookmarkedOnly) {
    return (
      <EmptyState
        icon={IconPin}
        title="No bookmarked mapsets here"
        description={`None of your bookmarks are in your ${libraryName}.`}
      />
    );
  }

  return (
    <EmptyState
      icon={IconFolderOff}
      title="No mapsets found"
      description={`Your ${libraryName} has no mapsets.`}
    />
  );
}

export function MapsetListError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <Alert icon={<IconAlertCircle />} color="red" title="Couldn't load mapsets">
      <Text size="sm" mb="xs">
        {message}
      </Text>
      <Button size="xs" variant="light" color="red" onClick={onRetry}>
        Retry
      </Button>
    </Alert>
  );
}

/** The last row of a fully loaded list. */
export function MapsetListEnd({ onBackToTop }: { onBackToTop: () => void }) {
  return (
    <Group justify="center" gap="xs" py="xs">
      <Text size="xs" c="dimmed">
        End of the list
      </Text>
      <Anchor component="button" type="button" size="xs" onClick={onBackToTop}>
        Back to top
      </Anchor>
    </Group>
  );
}

export function SongsFolderMissing({ onOpenSettings }: { onOpenSettings: () => void }) {
  return (
    <Alert icon={<IconAlertCircle />} color="yellow" title="Songs folder not set">
      <Text size="sm" mb="xs">
        Set your osu! Songs folder in Settings to see your stable mapsets.
      </Text>
      <Button
        size="xs"
        variant="light"
        color="gray"
        leftSection={<IconSettings size={16} />}
        onClick={onOpenSettings}
      >
        Open settings
      </Button>
    </Alert>
  );
}
