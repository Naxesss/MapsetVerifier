import type { ApiSnapshotTimingMark, ApiSnapshotVisualObject } from '../../../Types';

/**
 * Beat snap divisors the grid draws, coarsest first. The editor goes finer, but past 1/6 the lines
 * turn the picture into a texture, so the grid stops there.
 */
const DIVISORS = [1, 2, 3, 4, 6];

/** An object this close to a grid line is on it (the game rounds times to whole milliseconds). */
const TOLERANCE_MS = 2;

/**
 * Slider ends and reverse points come from a length the editor rounds, so on real maps they sit up
 * to about 13 ms off the grid (only a third are within 2 ms). They get more room, but never more
 * than a third of the snap's own spacing, so fine snaps stay apart.
 */
const END_TOLERANCE_MS = 14;
const LENIENT_MAX_DIVISOR = 4;

/** More lines than this just turn the field into a texture; the finest snaps are dropped first. */
const MAX_LINES = 500;

/** The editor's snap colours (white, red, purple, blue, ...). */
export const SNAP_COLOR: Record<number, string> = {
  1: '#F2F4F7',
  2: '#FF5C5C',
  3: '#B07CFF',
  4: '#4FA3FF',
  6: '#E27CFF',
  8: '#FFD84F',
  12: '#C9A0DC',
  16: '#9AA5B1',
};

export type GridLine = { time: number; divisor: number };

const gcd = (a: number, b: number): number => (b === 0 ? a : gcd(b, a % b));
const lcm = (a: number, b: number) => (a * b) / gcd(a, b);

function segmentAt(timing: ApiSnapshotTimingMark[], time: number) {
  let current = timing[0];
  for (const mark of timing) {
    if (mark.offset <= time + TOLERANCE_MS) current = mark;
  }
  return current;
}

/** The coarsest snap a time sits on, or null when it is off the grid (an unsnapped object). */
export function snapOf(
  time: number,
  timing: ApiSnapshotTimingMark[],
  /** For slider ends and reverse points, which are rounded by the editor. */
  lenient = false
): number | null {
  const mark = segmentAt(timing, time);
  if (!mark) return null;

  for (const divisor of DIVISORS) {
    const step = mark.beatLength / divisor;
    const k = Math.round((time - mark.offset) / step);
    // The extra room only goes to the coarse snaps. With it a 1/6 grid (lines about 80 ms apart)
    // would catch a third of all slider ends by chance, and show a snap nobody mapped to.
    const tolerance =
      lenient && divisor <= LENIENT_MAX_DIVISOR
        ? Math.min(END_TOLERANCE_MS, step / 3)
        : TOLERANCE_MS;
    if (Math.abs(time - mark.offset - k * step) <= tolerance) return divisor;
  }
  return null;
}

/** The snaps the objects actually use, so only those lines are drawn, as in the editor. */
export function usedDivisors(
  sides: { objects: ApiSnapshotVisualObject[]; timing: ApiSnapshotTimingMark[] }[]
): number[] {
  const counts = new Map<number, number>();
  const count = (divisor: number | null) => {
    if (divisor) counts.set(divisor, (counts.get(divisor) ?? 0) + 1);
  };
  let total = 0;

  for (const { objects, timing } of sides) {
    for (const object of objects) {
      // The head is exact; where a slider ends or turns around is only as exact as its length.
      count(snapOf(object.time, timing));
      total++;

      for (const time of [
        ...(object.edges ?? []),
        ...(object.endTime != null ? [object.endTime] : []),
      ])
        count(snapOf(time, timing, true));
    }
  }

  // A snap that a single object happens to land on is noise, not something the map is built on;
  // only a few objects at all and every snap counts. The beat line is always there.
  const needed = total <= 6 ? 1 : 2;

  return DIVISORS.filter((d) => d === 1 || (counts.get(d) ?? 0) >= needed);
}

/**
 * Grid lines between two times for the given snaps. A position is drawn once, at the coarsest snap
 * it falls on (a 1/2 line on a beat is just the beat line).
 */
export function gridLines(
  timing: ApiSnapshotTimingMark[],
  divisors: number[],
  from: number,
  to: number
): GridLine[] {
  const lines: GridLine[] = [];

  timing.forEach((mark, index) => {
    const segmentStart = index === 0 ? -Infinity : mark.offset;
    const segmentEnd = index + 1 < timing.length ? timing[index + 1].offset : Infinity;
    const lo = Math.max(from, segmentStart);
    const hi = Math.min(to, segmentEnd);
    if (hi < lo) return;

    // Spaced so that every snap in use lands on a line (1/2 and 1/3 meet at 1/6 of a beat).
    const step = mark.beatLength / divisors.reduce(lcm, 1);
    const first = Math.ceil((lo - mark.offset) / step - 1e-6);
    const last = Math.floor((hi - mark.offset) / step + 1e-6);

    for (let k = first; k <= last; k++) {
      const time = mark.offset + k * step;
      // A line on the boundary belongs to the next segment (its own grid takes over there).
      const endsHere = segmentEnd <= to;
      if (time < lo || (endsHere ? time >= hi - 1e-6 : time > hi + 1e-6)) continue;

      const divisor = divisors.find((d) => {
        const s = mark.beatLength / d;
        const n = Math.round((time - mark.offset) / s);
        return Math.abs(time - mark.offset - n * s) <= 0.01;
      });
      if (divisor) lines.push({ time, divisor });
    }
  });

  if (lines.length <= MAX_LINES || divisors.length <= 1) return lines;

  // Too dense: leave out the finest snap and try again.
  return gridLines(timing, divisors.slice(0, -1), from, to);
}

/** Lines closer together than this (on screen) are a blur, not a grid. */
export const MIN_LINE_SPACING_PX = 7;

/**
 * The snaps whose lines fit the picture: while the lines would sit closer than
 * <see cref="MIN_LINE_SPACING_PX" />, the finest snap is left out. The beat line always stays.
 */
export function fitDivisors(
  timing: ApiSnapshotTimingMark[],
  divisors: number[],
  from: number,
  to: number,
  /** Position of a time on screen, in pixels along the time axis. */
  place: (time: number) => number,
  /** Leaves out lines where time is not drawn to scale. */
  visible: (time: number) => boolean = () => true
): number[] {
  let shown = divisors;

  while (shown.length > 1) {
    const positions = gridLines(timing, shown, from, to)
      .filter((line) => visible(line.time))
      .map((line) => place(line.time))
      .sort((a, b) => a - b);

    let tightest = Infinity;
    for (let i = 1; i < positions.length; i++)
      tightest = Math.min(tightest, Math.abs(positions[i] - positions[i - 1]));

    if (tightest >= MIN_LINE_SPACING_PX) break;
    shown = shown.slice(0, -1);
  }

  return shown;
}
