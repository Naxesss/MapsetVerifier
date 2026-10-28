import {
  ActionIcon,
  Badge,
  Box,
  Group,
  Menu,
  Stack,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import {
  IconCamera,
  IconChecklist,
  IconDeviceFloppy,
  IconDots,
  IconFileImport,
  IconPin,
  IconPinnedOff,
} from '@tabler/icons-react';
import { Fragment, useEffect, useRef, type KeyboardEvent, type MouseEvent } from 'react';
import ChangeCounts from './ChangeCounts';
import { describeCheckDelta, TRIGGER_LABEL } from './describe';
import { indexOfEntry } from './range';
import { useDateTimeFormat } from '../../hooks/useDateTimeFormat';
import { formatDate } from '../../utils/dateTime';
import { MicroLabel } from '../common/Headings.tsx';
import type { ApiSnapshotHistoryDifficulty, ApiSnapshotHistoryEntry } from '../../Types';

interface SnapshotHistoryListProps {
  entries: ApiSnapshotHistoryEntry[];
  difficulties: ApiSnapshotHistoryDifficulty[];
  targetId?: string;
  baseId?: string;
  /** Click ends the comparison at a snapshot; shift-click starts it there. */
  onSelect: (id: string, asBase: boolean) => void;
  onPin: (entry: ApiSnapshotHistoryEntry) => void;
  onUnpin: (entry: ApiSnapshotHistoryEntry) => void;
}

/** Tall enough for about ten snapshots; longer histories scroll inside the list. */
const LIST_MAX_HEIGHT = 560;

const TRIGGER_ICON = {
  checkRun: IconChecklist,
  manual: IconCamera,
  import: IconFileImport,
  pageOpen: IconDeviceFloppy,
} as const;

/** Where the rail of the compared range runs, down the middle of the marks. */
const RAIL_X = 14;
const RAIL_COLOR = 'var(--mantine-color-primary-4)';

const dayKey = (time: string) => new Date(time).toDateString();

function formatDay(time: string): string {
  const date = new Date(time);
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);

  if (date.toDateString() === today.toDateString()) return 'Today';
  if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';

  return formatDate(date) ?? '';
}

/**
 * Snapshots newest first, grouped by day. The rail marks the range being compared: a click ends
 * it at a snapshot (B), a shift-click starts it there (A). Arrow keys move B through the list.
 */
export default function SnapshotHistoryList({
  entries,
  difficulties,
  targetId,
  baseId,
  onSelect,
  onPin,
  onUnpin,
}: SnapshotHistoryListProps) {
  const { formatTime } = useDateTimeFormat();
  const viewportRef = useRef<HTMLDivElement>(null);
  const targetIndex = indexOfEntry(entries, targetId);
  const baseIndex = indexOfEntry(entries, baseId);

  // Keeps the selected snapshot in view inside the list only. scrollIntoView would also scroll the
  // page itself, jumping it down to the list whenever the end snapshot is picked from the bar above.
  useEffect(() => {
    const viewport = viewportRef.current;
    const row = viewport?.querySelector<HTMLElement>(`[data-entry-id="${targetId}"]`);
    if (!viewport || !row) return;

    const margin = 8;
    const rowBox = row.getBoundingClientRect();
    const viewBox = viewport.getBoundingClientRect();

    if (rowBox.top < viewBox.top + margin) {
      viewport.scrollBy({ top: rowBox.top - viewBox.top - margin, behavior: 'smooth' });
    } else if (rowBox.bottom > viewBox.bottom - margin) {
      viewport.scrollBy({ top: rowBox.bottom - viewBox.bottom + margin, behavior: 'smooth' });
    }
  }, [targetId]);

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = event.key === 'ArrowDown' ? 1 : event.key === 'ArrowUp' ? -1 : 0;
    if (step === 0) return;

    event.preventDefault();
    const next = entries[Math.min(Math.max(targetIndex + step, 0), entries.length - 1)];
    if (next) {
      onSelect(next.id, false);
      viewportRef.current
        ?.querySelector<HTMLElement>(`[data-entry-id="${next.id}"]`)
        ?.focus({ preventScroll: true });
    }
  };

  const nameOf = (key: string) => difficulties.find((d) => d.key === key)?.name ?? key;

  return (
    // A plain scrolling box: the scrollbar gets its own gutter, so it never sits over the rows, and
    // the rows keep the width of the box instead of growing with their content.
    <div
      ref={viewportRef}
      style={{ maxHeight: LIST_MAX_HEIGHT, overflowY: 'auto', scrollbarGutter: 'stable' }}
    >
      <Stack gap={3} p="2xs" role="listbox" aria-label="Snapshots" onKeyDown={handleKeyDown}>
        {entries.map((entry, index) => {
          const isTarget = index === targetIndex;
          const isBase = index === baseIndex;
          const inRange =
            targetIndex >= 0 && baseIndex >= 0 && index >= targetIndex && index <= baseIndex;
          const startsDay = index === 0 || dayKey(entries[index - 1].time) !== dayKey(entry.time);
          const Trigger =
            TRIGGER_ICON[entry.trigger as keyof typeof TRIGGER_ICON] ?? IconDeviceFloppy;
          const delta = describeCheckDelta(entry.checks, entry.previousChecks);
          const changed = entry.changedDifficulties;

          return (
            <Fragment key={entry.id}>
              {startsDay && (
                <Box style={{ position: 'relative' }}>
                  {inRange && !isTarget && (
                    <Box
                      aria-hidden
                      style={{
                        position: 'absolute',
                        left: RAIL_X,
                        width: 2,
                        top: -3,
                        bottom: -3,
                        background: RAIL_COLOR,
                      }}
                    />
                  )}
                  <MicroLabel pt={index === 0 ? 0 : 'xs'} pb="2xs" style={{ paddingLeft: 34 }}>
                    {formatDay(entry.time)}
                  </MicroLabel>
                </Box>
              )}
              <Box style={{ position: 'relative' }}>
                {/* The rail of the compared range: a line down the marks, from B to A. */}
                {inRange && (
                  <Box
                    aria-hidden
                    style={{
                      position: 'absolute',
                      left: RAIL_X,
                      width: 2,
                      top: isTarget ? '50%' : -3,
                      bottom: isBase ? '50%' : -3,
                      background: RAIL_COLOR,
                      zIndex: 1,
                    }}
                  />
                )}
                <Box
                  aria-hidden
                  style={{
                    position: 'absolute',
                    left: isTarget || isBase ? 5 : 11,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    zIndex: 1,
                  }}
                >
                  {isTarget || isBase ? (
                    <Box
                      style={{
                        width: 20,
                        height: 20,
                        borderRadius: 4,
                        display: 'grid',
                        placeItems: 'center',
                        fontSize: 11,
                        fontWeight: 800,
                        color: 'var(--mantine-color-dark-9)',
                        background: isTarget
                          ? 'var(--mantine-color-green-5)'
                          : 'var(--mantine-color-primary-4)',
                      }}
                    >
                      {isTarget ? 'B' : 'A'}
                    </Box>
                  ) : (
                    <Box
                      style={{
                        width: 8,
                        height: 8,
                        borderRadius: '50%',
                        background: inRange ? RAIL_COLOR : 'var(--mantine-color-dark-7)',
                        border: `2px solid ${inRange ? RAIL_COLOR : 'var(--mantine-color-dark-2)'}`,
                      }}
                    />
                  )}
                </Box>
                <UnstyledButton
                  className="mv-clickable-row"
                  data-entry-id={entry.id}
                  data-selected={isTarget || undefined}
                  role="option"
                  aria-selected={isTarget}
                  tabIndex={isTarget || (targetIndex === -1 && index === 0) ? 0 : -1}
                  style={{
                    display: 'block',
                    width: '100%',
                    padding: '5px 30px 5px 34px',
                    background: inRange
                      ? 'color-mix(in srgb, var(--mantine-color-primary-4) 10%, transparent)'
                      : undefined,
                  }}
                  onClick={(event: MouseEvent) => onSelect(entry.id, event.shiftKey)}
                >
                  <Group gap={7} wrap="nowrap" style={{ minWidth: 0 }}>
                    <Text size="sm" fw={700} style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {formatTime(entry.time)}
                    </Text>
                    <Tooltip label={TRIGGER_LABEL[entry.trigger] ?? entry.trigger}>
                      <Box style={{ display: 'flex', opacity: 0.6 }}>
                        <Trigger size={14} />
                      </Box>
                    </Tooltip>
                    {entry.pin ? (
                      <Group gap={4} wrap="nowrap" style={{ minWidth: 0 }}>
                        <IconPin size={13} color="var(--mantine-color-orange-3)" />
                        <Text size="xs" fw={700} c="orange.3" truncate>
                          {entry.pin}
                        </Text>
                      </Group>
                    ) : (
                      index === 0 && <Badge color="green">Latest</Badge>
                    )}
                    <Box style={{ flex: 1 }} />
                    {entry.isFirst ? (
                      <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
                        First
                      </Text>
                    ) : (
                      <ChangeCounts
                        counts={entry.counts}
                        empty={entry.fileChanges + entry.generalChanges > 0 ? null : 'No changes'}
                      />
                    )}
                  </Group>
                  {(changed.length > 0 || delta) && (
                    <Group gap={6} wrap="nowrap" mt={2} style={{ minWidth: 0 }}>
                      <Text size="xs" c="dimmed" truncate style={{ flex: 1, minWidth: 0 }}>
                        {changed.length === 0
                          ? ''
                          : changed.length <= 2
                            ? changed.map(nameOf).join(', ')
                            : `${changed.length} difficulties`}
                      </Text>
                      {delta && (
                        <Text
                          size="xs"
                          c={delta.better ? 'green.4' : 'red.4'}
                          style={{ whiteSpace: 'nowrap' }}
                        >
                          {delta.text}
                        </Text>
                      )}
                    </Group>
                  )}
                </UnstyledButton>
                <Menu position="bottom-end" withinPortal>
                  <Menu.Target>
                    <ActionIcon
                      variant="subtle"
                      color="gray"
                      size="sm"
                      aria-label="Snapshot options"
                      style={{ position: 'absolute', right: 3, top: 4 }}
                    >
                      <IconDots size={14} />
                    </ActionIcon>
                  </Menu.Target>
                  <Menu.Dropdown>
                    <Menu.Item leftSection={<IconPin size={16} />} onClick={() => onPin(entry)}>
                      {entry.pin ? 'Rename milestone' : 'Pin as milestone'}
                    </Menu.Item>
                    {entry.pin && (
                      <Menu.Item
                        leftSection={<IconPinnedOff size={16} />}
                        onClick={() => onUnpin(entry)}
                      >
                        Remove milestone
                      </Menu.Item>
                    )}
                  </Menu.Dropdown>
                </Menu>
              </Box>
            </Fragment>
          );
        })}
      </Stack>
    </div>
  );
}
