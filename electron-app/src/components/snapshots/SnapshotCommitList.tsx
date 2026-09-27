import { Badge, Group, ScrollArea, Stack, Text, UnstyledButton } from '@mantine/core';
import { Fragment, useEffect, useRef, type KeyboardEvent } from 'react';
import { useDateTimeFormat } from '../../hooks/useDateTimeFormat';
import { ApiSnapshotCommit } from '../../Types';
import { formatDate } from '../../utils/dateTime';
import { MicroLabel } from '../common/Headings.tsx';

interface SnapshotCommitListProps {
  commits: ApiSnapshotCommit[];
  selectedCommitId?: string;
  onSelectCommit: (commitId: string) => void;
}

/** Tall enough for about ten snapshots; longer histories scroll inside the list. */
const LIST_MAX_HEIGHT = 520;

function dayKey(dateString: string) {
  return new Date(dateString).toDateString();
}

function formatDay(dateString: string): string {
  const date = new Date(dateString);
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);

  if (date.toDateString() === today.toDateString()) return 'Today';
  if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';

  return formatDate(date) ?? '';
}

/** Added, removed and changed counts as coloured numbers, or why there are none. */
export function CommitChangeSummary({ commit }: { commit: ApiSnapshotCommit }) {
  if (!commit.hasSnapshot) {
    return (
      <Text size="xs" c="dimmed">
        No snapshot
      </Text>
    );
  }

  if (commit.totalChanges === 0) {
    return (
      <Text size="xs" c="dimmed">
        No changes
      </Text>
    );
  }

  return (
    <Group gap="xs" wrap="nowrap">
      {commit.additions > 0 && (
        <Text size="xs" fw={600} c="green.5">
          +{commit.additions}
        </Text>
      )}
      {commit.removals > 0 && (
        <Text size="xs" fw={600} c="red.5">
          −{commit.removals}
        </Text>
      )}
      {commit.modifications > 0 && (
        <Text size="xs" fw={600} c="yellow.5">
          ~{commit.modifications}
        </Text>
      )}
    </Group>
  );
}

/**
 * Snapshots newest first, grouped by day. Arrow keys move through them, like scrolling a log.
 */
function SnapshotCommitList({
  commits,
  selectedCommitId,
  onSelectCommit,
}: SnapshotCommitListProps) {
  const { formatTime } = useDateTimeFormat();
  const viewportRef = useRef<HTMLDivElement>(null);
  const currentIndex = commits.findIndex((commit) => commit.id === selectedCommitId);

  useEffect(() => {
    if (!selectedCommitId) return;
    const el = viewportRef.current?.querySelector(`[data-commit-id="${selectedCommitId}"]`);
    el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }, [selectedCommitId]);

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = event.key === 'ArrowDown' ? 1 : event.key === 'ArrowUp' ? -1 : 0;
    if (step === 0) return;

    event.preventDefault();
    const next = commits[Math.min(Math.max(currentIndex + step, 0), commits.length - 1)];
    if (next) {
      onSelectCommit(next.id);
      viewportRef.current
        ?.querySelector<HTMLElement>(`[data-commit-id="${next.id}"]`)
        ?.focus({ preventScroll: true });
    }
  };

  // offsetScrollbars keeps room for the scrollbar, so it never covers the change counts.
  return (
    <ScrollArea.Autosize
      mah={LIST_MAX_HEIGHT}
      type="auto"
      scrollbars="y"
      offsetScrollbars="y"
      viewportRef={viewportRef}
    >
      <Stack gap="xs" p="2xs" role="listbox" aria-label="Snapshots" onKeyDown={handleKeyDown}>
        {commits.map((commit, index) => {
          const isSelected = commit.id === selectedCommitId;
          const startsDay = index === 0 || dayKey(commits[index - 1].date) !== dayKey(commit.date);

          return (
            <Fragment key={commit.id}>
              {startsDay && (
                <MicroLabel pt={index === 0 ? 0 : 'sm'} px="sm" pb="2xs">
                  {formatDay(commit.date)}
                </MicroLabel>
              )}
              <UnstyledButton
                className="mv-clickable-row"
                data-commit-id={commit.id}
                data-selected={isSelected || undefined}
                role="option"
                aria-selected={isSelected}
                tabIndex={isSelected || (currentIndex === -1 && index === 0) ? 0 : -1}
                px="sm"
                py="xs"
                onClick={() => onSelectCommit(commit.id)}
              >
                <Group justify="space-between" gap="sm" wrap="nowrap">
                  <Group gap="xs" wrap="nowrap">
                    <Text size="sm" fw={500}>
                      {formatTime(commit.date)}
                    </Text>
                    {index === 0 && <Badge color="green">Latest</Badge>}
                  </Group>
                  <CommitChangeSummary commit={commit} />
                </Group>
              </UnstyledButton>
            </Fragment>
          );
        })}
      </Stack>
    </ScrollArea.Autosize>
  );
}

export default SnapshotCommitList;
