import { ActionIcon, Button, Group, Skeleton, Stack, Text, Tooltip } from '@mantine/core';
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react';
import { useEffect, useRef, useState } from 'react';
import { formatClock } from './describe';
import { useSnapshotWindow, type SnapshotWindowContext } from './hooks/useSnapshotWindow';
import CatchField from './visuals/CatchField';
import HitsoundSequencer from './visuals/HitsoundSequencer';
import ManiaLanes from './visuals/ManiaLanes';
import OnionSkin from './visuals/OnionSkin';
import RhythmLanes from './visuals/RhythmLanes';
import TaikoStrip from './visuals/TaikoStrip';
import { countWord } from '../../utils/countWord';
import type { ApiSnapshotHunk, Mode } from '../../Types';

interface HunkWindowProps {
  /** What to draw: where objects sit, or what sounds they play. */
  view?: 'objects' | 'hitsounds';
  hunk: ApiSnapshotHunk;
  mode: Mode;
  context: SnapshotWindowContext;
  /** A time shift every object shares, so shifted context objects don't count as changed. */
  shift: number;
}

/** How far the wheel turns (in pixels) for the window to move one step (half a beat); a notch is about 100. */
const WHEEL_PX_PER_STEP = 60;

/** A fast wheel flick never jumps more than this many steps at once. */
const MAX_STEPS_PER_WHEEL_EVENT = 3;

/** A picture sits on its own darker card, at a fixed size however wide the page is. */
function VizCard({ width, children }: { width: number; children: React.ReactNode }) {
  return (
    <div
      style={{
        width: `min(100%, ${width}px)`,
        padding: 8,
        borderRadius: 5,
        background: 'var(--mantine-color-dark-8)',
        minWidth: 0,
      }}
    >
      {children}
    </div>
  );
}

/**
 * A fixed stretch of the song (one measure) around a change, before and after, so the change is seen
 * in context and the picture is always the same size and zoom. Scroll over it (or use the arrows)
 * to move along the song. Context is muted; what changed is coloured.
 */
export default function HunkWindow({
  hunk,
  mode,
  context,
  shift,
  view = 'objects',
}: HunkWindowProps) {
  const [offset, setOffset] = useState(0);
  const { data, isPlaceholderData, isError } = useSnapshotWindow(
    context,
    hunk.start,
    hunk.end,
    offset
  );

  // The wheel handler lives outside React's render, so it reads the limits from here.
  const limits = useRef({ earlier: false, later: false });
  const hasEarlier = !!data?.hasEarlier;
  const hasLater = !!data?.hasLater;
  useEffect(() => {
    limits.current = { earlier: hasEarlier, later: hasLater };
  }, [hasEarlier, hasLater]);
  const frame = useRef<HTMLDivElement>(null);
  const turned = useRef(0);

  // React's wheel handlers are passive and cannot stop the page from scrolling, so this is a native
  // one. It only takes the wheel while there is more to scroll to, so the page scrolls on at the ends.
  useEffect(() => {
    const element = frame.current;
    if (!element) return;

    const onWheel = (event: WheelEvent) => {
      if (event.ctrlKey) return;

      const delta = event.deltaY || event.deltaX;
      const forward = delta > 0;
      if (!delta || !(forward ? limits.current.later : limits.current.earlier)) {
        turned.current = 0;
        return;
      }

      event.preventDefault();
      turned.current += delta;

      const steps = Math.trunc(turned.current / WHEEL_PX_PER_STEP);
      if (steps === 0) return;

      turned.current -= steps * WHEEL_PX_PER_STEP;
      const clamped = Math.max(
        -MAX_STEPS_PER_WHEEL_EVENT,
        Math.min(MAX_STEPS_PER_WHEEL_EVENT, steps)
      );
      setOffset((current) => current + clamped);
    };

    element.addEventListener('wheel', onWheel, { passive: false });
    return () => element.removeEventListener('wheel', onWheel);
  }, []);

  const content = (() => {
    if (isError) {
      return (
        <Text size="xs" c="dimmed">
          Couldn&apos;t load this part of the song.
        </Text>
      );
    }

    if (!data) return <Skeleton height={180} radius="md" aria-busy aria-label="Loading" />;

    const { visual, from, to } = data;
    const empty = visual.before.length === 0 && visual.after.length === 0;
    // The change itself can be longer than what is shown at once.
    const changeContinues = offset === 0 && hunk.end > to;

    return (
      <Stack gap="xs">
        <Group gap="xs" wrap="nowrap" justify="space-between">
          <Group gap="xs" wrap="wrap">
            <Group gap="xs" wrap="nowrap">
              <Tooltip label="Half a beat earlier">
                <ActionIcon
                  variant="default"
                  size="sm"
                  aria-label="Show half a beat earlier"
                  disabled={!data.hasEarlier}
                  onClick={() => setOffset((o) => o - 1)}
                >
                  <IconChevronLeft size={14} />
                </ActionIcon>
              </Tooltip>
              <Tooltip label="Half a beat later">
                <ActionIcon
                  variant="default"
                  size="sm"
                  aria-label="Show half a beat later"
                  disabled={!data.hasLater}
                  onClick={() => setOffset((o) => o + 1)}
                >
                  <IconChevronRight size={14} />
                </ActionIcon>
              </Tooltip>
            </Group>
            <Text size="xs" c="dimmed" ff="monospace">
              {formatClock(Math.max(from, 0))} to {formatClock(to)}
            </Text>
            <Text size="xs" c="dimmed">
              {countWord(data.objects, 'object')}
            </Text>
            {(data.hasEarlier || data.hasLater) && (
              <Text size="xs" c="dimmed">
                Scroll to move along the song
              </Text>
            )}
            {changeContinues && (
              <Text size="xs" c="yellow.5">
                The change goes on after these
              </Text>
            )}
          </Group>
          {offset !== 0 && (
            <Button variant="subtle" size="compact-xs" onClick={() => setOffset(0)}>
              Back to the change
            </Button>
          )}
        </Group>

        <div style={{ opacity: isPlaceholderData ? 0.55 : 1, transition: 'opacity 0.15s' }}>
          {empty ? (
            <Text size="sm" c="dimmed" py="md">
              No objects here.
            </Text>
          ) : (
            <>
              {view === 'hitsounds' && (
                <VizCard width={640}>
                  <HitsoundSequencer visual={visual} start={from} end={to} shift={shift} />
                </VizCard>
              )}
              {view === 'objects' && mode === 'Standard' && (
                <Group gap="sm" align="flex-start" wrap="wrap">
                  <VizCard width={600}>
                    <RhythmLanes visual={visual} start={from} end={to} shift={shift} />
                  </VizCard>
                  <VizCard width={420}>
                    <OnionSkin visual={visual} shift={shift} range={[from, to]} />
                  </VizCard>
                </Group>
              )}
              {view === 'objects' && mode === 'Catch' && (
                <VizCard width={520}>
                  <CatchField visual={visual} start={from} end={to} shift={shift} />
                </VizCard>
              )}
              {view === 'objects' && mode === 'Taiko' && (
                <VizCard width={620}>
                  <TaikoStrip visual={visual} start={from} end={to} shift={shift} />
                </VizCard>
              )}
              {view === 'objects' && mode === 'Mania' && (
                <VizCard width={460}>
                  <ManiaLanes visual={visual} start={from} end={to} shift={shift} />
                </VizCard>
              )}
            </>
          )}
        </div>
      </Stack>
    );
  })();

  return <div ref={frame}>{content}</div>;
}
