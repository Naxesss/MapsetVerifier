import type { ObjectsTimingSegment } from '../../../Types.ts';

/** Last uninherited segment whose start is <= timeMs (osu-style timing). */
export function findTimingSegmentForTime(
  segments: ObjectsTimingSegment[],
  timeMs: number
): ObjectsTimingSegment | undefined {
  if (segments.length === 0) return undefined;
  const sorted = [...segments].sort((a, b) => a.startTimeMs - b.startTimeMs);
  let current = sorted[0];
  for (const seg of sorted) {
    if (seg.startTimeMs <= timeMs) current = seg;
    else break;
  }
  return current;
}

/** Snap to nearest step of msPerBeat / snapDivisor anchored at segment offset. */
export function snapTimeToBeatGrid(
  timeMs: number,
  segment: ObjectsTimingSegment,
  snapDivisor: number
): number {
  if (snapDivisor <= 0) return timeMs;
  const step = segment.msPerBeat / snapDivisor;
  if (step <= 0 || !Number.isFinite(step)) return timeMs;
  const anchor = segment.offsetMs;
  const k = Math.round((timeMs - anchor) / step);
  return anchor + k * step;
}
