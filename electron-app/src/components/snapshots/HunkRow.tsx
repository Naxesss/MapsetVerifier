import { Badge, Box, Group, Stack, Text, UnstyledButton } from '@mantine/core';
import { IconChevronRight } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import ChangeCounts from './ChangeCounts';
import ChangeLines from './ChangeLines';
import {
  formatClock,
  formatStamp,
  HUNK_LABEL,
  hunkSummary,
  KIND_COLOR,
  KIND_LABEL,
  KIND_ORDER,
  toChangeLines,
} from './describe';
import HunkWindow from './HunkWindow';
import SvCurve, { hasSvChanges } from './visuals/SvCurve';
import OsuLink from '../common/OsuLink.tsx';
import type { SnapshotWindowContext } from './hooks/useSnapshotWindow';
import type { ApiSnapshotHunk, Mode } from '../../Types';

interface HunkRowProps {
  hunk: ApiSnapshotHunk;
  mode: Mode;
  defaultOpen?: boolean;
  /** Which snapshots and difficulty the objects around the change are read from. */
  context: SnapshotWindowContext;
  /** A time shift every object shares (an offset change), which is not a change to each object. */
  shift?: number;
}

const hasHitsoundChanges = (changes: ApiSnapshotHunk['changes']) =>
  changes.some((c) => c.kind === 'Hitsound');

function KindDots({ kinds }: { kinds: ApiSnapshotHunk['kinds'] }) {
  return (
    <Group gap={3} wrap="nowrap" aria-label={kinds.map((k) => KIND_LABEL[k]).join(', ')}>
      {KIND_ORDER.filter((k) => kinds.includes(k)).map((kind) => (
        <Box
          key={kind}
          title={KIND_LABEL[kind]}
          style={{ width: 8, height: 8, borderRadius: 2, background: KIND_COLOR[kind] }}
        />
      ))}
    </Group>
  );
}

/** Whether a hunk is shown as pictures; then the details live in the hover cards, not in text. */
function drawsPictures(hunk: ApiSnapshotHunk) {
  return (
    (hunk.kinds.some((k) => k === 'Rhythm' || k === 'Placement') &&
      hunk.changes.some((c) => c.object)) ||
    hasHitsoundChanges(hunk.changes) ||
    hasSvChanges(hunk.changes)
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
      {hasSvChanges(hunk.changes) && (
        <SvCurve changes={hunk.changes} start={hunk.start} end={hunk.end} />
      )}
    </Stack>
  );
}

/**
 * One stretch of the song where something changed: closed it is a single row (where, what kind,
 * how much); open it shows the before/after picture and every change in it.
 */
export default function HunkRow({
  hunk,
  mode,
  defaultOpen = false,
  context,
  shift = 0,
}: HunkRowProps) {
  const [open, setOpen] = useState(defaultOpen);
  const lines = useMemo(() => toChangeLines(hunk.changes), [hunk.changes]);
  const range = hunk.end > hunk.start + 1;

  return (
    <Box
      style={{
        borderRadius: 'var(--mantine-radius-md)',
        background: 'var(--mantine-color-dark-6)',
      }}
    >
      <Group gap="sm" wrap="nowrap" px="sm" py="xs" mih="var(--mv-control-height)">
        <UnstyledButton
          onClick={() => setOpen((v) => !v)}
          aria-expanded={open}
          aria-label={open ? 'Collapse' : 'Expand'}
          style={{ display: 'flex', alignItems: 'center', flexShrink: 0 }}
        >
          <IconChevronRight
            size={16}
            style={{ transform: open ? 'rotate(90deg)' : undefined, transition: 'transform 0.15s' }}
          />
        </UnstyledButton>
        <Text size="sm" style={{ flexShrink: 0 }}>
          <OsuLink text={formatStamp(hunk.start)} />
        </Text>
        {range && (
          <Text size="xs" c="dimmed" style={{ flexShrink: 0 }}>
            to {formatClock(hunk.end)}
          </Text>
        )}
        <Badge color="gray">{HUNK_LABEL[hunk.label]}</Badge>
        <KindDots kinds={hunk.kinds} />
        <Text size="sm" c="dimmed" truncate style={{ flex: 1, minWidth: 0 }}>
          {hunkSummary(hunk)}
        </Text>
        <ChangeCounts counts={hunk.counts} empty={null} />
      </Group>
      {open && (
        <Stack gap="md" px="sm" pb="sm">
          <HunkVisuals hunk={hunk} mode={mode} context={context} shift={shift} />
          {!drawsPictures(hunk) && <ChangeLines lines={lines} />}
        </Stack>
      )}
    </Box>
  );
}
