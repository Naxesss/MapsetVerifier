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
import { Hint } from '../overview/objects/components/TimelineShortcutHints';
import { useTimelineModifierKeys } from '../overview/objects/hooks/useTimelineModifierKeys';
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

/** A picture sits on its own darker card and fills the width it gets; `width` is its share of a row it shares with another card. */
function VizCard({ width, children }: { width: number; children: React.ReactNode }) {
  return (
    <div
      style={{
        flex: `1 1 ${width}px`,
        width: '100%',
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
  // Lights the Shift hint while it is held, like the objects timeline on the Overview does.
  const { shiftHeld } = useTimelineModifierKeys();
  const { data, isPlaceholderData, isError } = useSnapshotWindow(
    context,
    hunk.start,
    hunk.end,
    offset
  );

  // The wheel handler lives outside React's render, so it reads the limits from here.
  const limits = useRef({ earlier: false, later: false });
  // The way back to the change is always open. At the start of the song nothing comes before the
  // first object, but after scrolling on from it there is still the way back.
  const hasEarlier = !!data?.hasEarlier || offset > 0;
  const hasLater = !!data?.hasLater || offset < 0;
  useEffect(() => {
    limits.current = { earlier: hasEarlier, later: hasLater };
  }, [hasEarlier, hasLater]);
  const frame = useRef<HTMLDivElement>(null);
  const turned = useRef(0);

  // React's wheel handlers are passive and cannot stop the page from scrolling, so this is a native
  // one. The wheel moves along the song only with Shift held (as Shift picks the start of a
  // comparison in the history), or with a sideways swipe; plain scrolling scrolls the page. It also
  // lets go while there is nothing more to scroll to, so the page scrolls on at the ends.
  useEffect(() => {
    const element = frame.current;
    if (!element) return;

    const onWheel = (event: WheelEvent) => {
      if (event.ctrlKey) return;

      const sideways = Math.abs(event.deltaX) > Math.abs(event.deltaY);
      if (!event.shiftKey && !sideways) {
        turned.current = 0;
        return;
      }

      // Holding Shift turns the wheel sideways in some browsers, so either direction counts.
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
                  disabled={!hasEarlier}
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
                  disabled={!hasLater}
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
          </Group>
          {offset !== 0 && (
            <Button variant="subtle" size="compact-xs" onClick={() => setOffset(0)}>
              Back to the change
            </Button>
          )}
        </Group>
        {/* On a line of its own, so it stays put while the time and count above change width. */}
        {(hasEarlier || hasLater) && (
          <Hint active={shiftHeld} keys="Shift">
            moves along the song
          </Hint>
        )}

        <div style={{ opacity: isPlaceholderData ? 0.55 : 1, transition: 'opacity 0.15s' }}>
          {empty ? (
            <Text size="sm" c="dimmed" py="md">
              No objects here.
            </Text>
          ) : (
            <>
              {view === 'hitsounds' && (
                <VizCard width={640}>
                  <HitsoundSequencer
                    visual={visual}
                    start={from}
                    end={to}
                    shift={shift}
                    lineChanges={hunk.changes.filter((c) => !c.object && c.kind === 'Hitsound')}
                    mode={mode}
                  />
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
