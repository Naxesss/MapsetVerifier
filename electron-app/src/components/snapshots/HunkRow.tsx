import { Box, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import {
  IconArrowsMove,
  IconChevronRight,
  IconClockEdit,
  IconMetronome,
  IconRefresh,
  IconShape,
  IconVolume,
  IconVolume2,
  IconWaveSine,
} from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import ChangeCounts from './ChangeCounts';
import ChangeLines from './ChangeLines';
import { formatStamp, hunkDetail, hunkHeadline, toChangeLines } from './describe';
import HunkWindow from './HunkWindow';
import SvTable, { hasSvChanges, svChanges } from './visuals/SvTable';
import OsuLink from '../common/OsuLink.tsx';
import type { SnapshotWindowContext } from './hooks/useSnapshotWindow';
import type { ApiSnapshotHunk, Mode, SnapshotHunkLabel } from '../../Types';

interface HunkRowProps {
  hunk: ApiSnapshotHunk;
  mode: Mode;
  defaultOpen?: boolean;
  /** Which snapshots and difficulty the objects around the change are read from. */
  context: SnapshotWindowContext;
  /** A time shift every object shares (an offset change), which is not a change to each object. */
  shift?: number;
}

const LABEL_ICON: Record<SnapshotHunkLabel, typeof IconClockEdit> = {
  Remapped: IconRefresh,
  Rhythm: IconMetronome,
  Placement: IconArrowsMove,
  Hitsounding: IconVolume,
  Sv: IconWaveSine,
  Mixed: IconShape,
  Shifted: IconClockEdit,
  Volume: IconVolume2,
};

/** Rows that stand for many changes made the same way: they say it all and do not open. */
const isGroup = (hunk: ApiSnapshotHunk) => hunk.label === 'Shifted' || hunk.label === 'Volume';

/** Width of the open/close arrow, kept free on rows without one so the icons line up. */
const TOGGLE_WIDTH = 16;

const hasHitsoundChanges = (changes: ApiSnapshotHunk['changes']) =>
  changes.some((c) => c.kind === 'Hitsound');

/** Whether a hunk is shown as pictures; then the details live in the hover cards, not in text. */
function drawsPictures(hunk: ApiSnapshotHunk) {
  return (
    (hunk.kinds.some((k) => k === 'Rhythm' || k === 'Placement') &&
      hunk.changes.some((c) => c.object)) ||
    hasHitsoundChanges(hunk.changes)
  );
}

function HunkVisuals({
  hunk,
  mode,
  context,
  shift,
}: {
  hunk: ApiSnapshotHunk;
  mode: Mode;
  context: SnapshotWindowContext;
  shift: number;
}) {
  // Only changes to the objects themselves are drawn; a hitsound pass or a timing change says
  // nothing in a picture of where objects sit.
  const drawsObjects =
    hunk.kinds.some((k) => k === 'Rhythm' || k === 'Placement') &&
    hunk.changes.some((c) => c.object);

  return (
    <Stack gap="md">
      {drawsObjects && <HunkWindow hunk={hunk} mode={mode} context={context} shift={shift} />}
      {hasHitsoundChanges(hunk.changes) && (
        <>
          <HunkWindow hunk={hunk} mode={mode} context={context} shift={shift} view="hitsounds" />
        </>
      )}
      {hasSvChanges(hunk.changes) && <SvTable changes={hunk.changes} />}
    </Stack>
  );
}

/**
 * The line every row starts with: what kind of change it is, what happened in a sentence, where
 * it is in the song, and below that a detail when there is one worth reading without opening.
 */
function RowHeader({ hunk, toggle }: { hunk: ApiSnapshotHunk; toggle?: React.ReactNode }) {
  const Icon = LABEL_ICON[hunk.label];
  const detail = hunkDetail(hunk);
  const range = hunk.end > hunk.start + 1;

  return (
    <Group gap="sm" wrap="nowrap" align="flex-start" p="sm">
      <Box style={{ width: TOGGLE_WIDTH, flexShrink: 0, display: 'flex', marginTop: 2 }}>
        {toggle}
      </Box>
      <Icon size={20} stroke={1.5} style={{ flexShrink: 0 }} />
      <Stack gap={2} style={{ flex: 1, minWidth: 0 }}>
        <Group gap="xs" wrap="wrap">
          <Text size="sm" fw={500}>
            {hunkHeadline(hunk)}
          </Text>
          <Text size="sm">
            <OsuLink text={formatStamp(hunk.start)} disableSeparators />
            {range && (
              <>
                {' to '}
                <OsuLink text={formatStamp(hunk.end)} disableSeparators />
              </>
            )}
          </Text>
        </Group>
        {detail && (
          <Text size="xs" c="dimmed">
            {detail}
          </Text>
        )}
      </Stack>
      {!isGroup(hunk) && (
        <Box style={{ flexShrink: 0, marginTop: 2 }}>
          <ChangeCounts counts={hunk.counts} empty={null} />
        </Box>
      )}
    </Group>
  );
}

const rowStyle = {
  borderRadius: 'var(--mantine-radius-md)',
  background: 'var(--mantine-color-dark-6)',
};

/**
 * One stretch of the song where something changed: closed it is a single row (what happened and
 * where); open it shows the before/after picture and every change in it. A part of the map moved
 * in time has nothing more to show, so it does not open.
 */
export default function HunkRow({
  hunk,
  mode,
  defaultOpen = false,
  context,
  shift = 0,
}: HunkRowProps) {
  const [open, setOpen] = useState(defaultOpen);
  // SV changes are in their table; the rest of a timing hunk (BPM, kiai, lines added) is listed.
  const lines = useMemo(() => {
    const inTable = new Set(svChanges(hunk.changes));
    return toChangeLines(hunk.changes.filter((c) => !inTable.has(c)));
  }, [hunk.changes]);

  if (isGroup(hunk)) {
    return (
      <Box style={rowStyle}>
        <RowHeader hunk={hunk} />
      </Box>
    );
  }

  return (
    <Box style={rowStyle}>
      <RowHeader
        hunk={hunk}
        toggle={
          <UnstyledButton
            onClick={() => setOpen((v) => !v)}
            aria-expanded={open}
            aria-label={open ? 'Collapse' : 'Expand'}
            style={{ display: 'flex', alignItems: 'center' }}
          >
            <IconChevronRight
              size={TOGGLE_WIDTH}
              style={{
                transform: open ? 'rotate(90deg)' : undefined,
                transition: 'transform 0.15s',
              }}
            />
          </UnstyledButton>
        }
      />
      {open && (
        <Stack gap="md" px="sm" pb="sm">
          <HunkVisuals hunk={hunk} mode={mode} context={context} shift={shift} />
          {!drawsPictures(hunk) && <ChangeLines lines={lines} />}
        </Stack>
      )}
    </Box>
  );
}
