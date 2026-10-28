import { Box, Group, Text } from '@mantine/core';
import { gridLines, SNAP_COLOR, usedDivisors } from './beatGrid';
import type { ApiSnapshotHunkVisual } from '../../../Types';

/** The snaps the hunk's objects use, so every view of it draws the same lines. */
export function hunkDivisors(visual: ApiSnapshotHunkVisual): number[] {
  return usedDivisors([
    { objects: visual.before, timing: visual.beforeTiming },
    { objects: visual.after, timing: visual.afterTiming },
  ]);
}

/**
 * The beat grid behind a field, like the editor's timeline: a line for every position of the snaps
 * that are in use, in the editor's colour for that snap. `place` turns a time into where the line
 * goes on the field; the line then runs across the field in the other direction.
 */
export default function SnapGrid({
  timing,
  divisors,
  from,
  to,
  place,
  across,
  visible,
}: {
  timing: ApiSnapshotHunkVisual['beforeTiming'];
  divisors: number[];
  from: number;
  to: number;
  /** Position of a time along the time axis (y for lanes, x for taiko). */
  place: (time: number) => number;
  /** Where the line starts and ends on the other axis, and which axis time runs along. */
  across: { start: number; end: number; vertical?: boolean };
  /** Leaves out lines where time is not drawn to scale (a compressed gap). */
  visible?: (time: number) => boolean;
}) {
  if (timing.length === 0) return null;

  return (
    <g aria-hidden>
      {gridLines(timing, divisors, from, to)
        .filter(({ time }) => visible?.(time) ?? true)
        .map(({ time, divisor }) => {
          const p = place(time);
          const beat = divisor === 1;

          return (
            <line
              key={`${time}-${divisor}`}
              x1={across.vertical ? p : across.start}
              x2={across.vertical ? p : across.end}
              y1={across.vertical ? across.start : p}
              y2={across.vertical ? across.end : p}
              stroke={SNAP_COLOR[divisor] ?? '#9AA5B1'}
              strokeOpacity={beat ? 0.45 : 0.3}
              strokeWidth={beat ? 1.2 : 1}
            />
          );
        })}
    </g>
  );
}

/** Which colour is which snap, for the snaps the hunk uses. */
export function SnapLegend({ divisors }: { divisors: number[] }) {
  return (
    <Group gap="sm" wrap="wrap">
      <Text size="xs" c="dimmed">
        Grid:
      </Text>
      {divisors.map((divisor) => (
        <Group key={divisor} gap={4} wrap="nowrap">
          <Box style={{ width: 12, height: 3, borderRadius: 1, background: SNAP_COLOR[divisor] }} />
          <Text size="xs" c="dimmed">
            1/{divisor}
          </Text>
        </Group>
      ))}
    </Group>
  );
}
