import { formatClock } from '../describe';
import type { ApiSnapshotVisualObject } from '../../../Types';

export type PairStatus = 'same' | 'changed' | 'added' | 'removed';

export type PairedObject = {
  before?: ApiSnapshotVisualObject;
  after?: ApiSnapshotVisualObject;
  status: PairStatus;
  /** What changed about this object, for the hover card. */
  details: string[];
};

/** Objects this close in time and place are the same object, retimed or nudged. */
const RETIME_WINDOW_MS = 125;
const RETIME_MAX_DISTANCE = 40;

/**
 * The pictures' own palette, the one the design was drawn in: a calm grey for what did not change
 * and clear hues for what did, on near-black surfaces.
 */
export const PALETTE = {
  surface: '#121923',
  grid: '#1B2330',
  line: '#353F52',
  muted: '#A3ADBD',
  dim: '#8695AB',
  ink: '#11161D',
  dot: '#5A677C',
};

/** What the drawing colours use; the same hues as the added, removed and changed counts. */
export const STATUS_COLOR: Record<PairStatus, string> = {
  same: '#C9D4E3',
  changed: '#FCC419',
  added: '#3BDF70',
  removed: '#FF6B6B',
};

const distance = (a: ApiSnapshotVisualObject, b: ApiSnapshotVisualObject) =>
  Math.hypot(a.x - b.x, a.y - b.y);

/**
 * How far apart two objects are in time once a shared shift is allowed for. An offset change moves
 * the objects it touches by the shift, but objects it left alone are just as unchanged, so a gap of
 * either nothing or the shift counts as no gap.
 */
const timeGap = (a: number, b: number, shift: number) =>
  Math.min(Math.abs(a - b), Math.abs(a - b - shift));

function identical(
  a: ApiSnapshotVisualObject,
  b: ApiSnapshotVisualObject,
  shift: number,
  compareHitsound: boolean
) {
  const samePath = JSON.stringify(a.path) === JSON.stringify(b.path);

  // A slider lasts as long as its length and the tempo and slider velocity say, so an SV or BPM
  // edit elsewhere makes it shorter or longer without the slider itself being touched. That shows
  // with the timing changes; here only a slider whose own shape changed counts. Spinners and hold
  // notes have no shape, so their end time is their own.
  const sliderTimingOnly =
    a.type === 'Slider' &&
    b.type === 'Slider' &&
    samePath &&
    (a.edges?.length ?? 0) === (b.edges?.length ?? 0);
  const endsAgree =
    sliderTimingOnly ||
    (a.endTime == null || b.endTime == null
      ? a.endTime == null && b.endTime == null
      : timeGap(a.endTime, b.endTime, shift) < 1);

  return (
    timeGap(a.time, b.time, shift) < 1 &&
    distance(a, b) < 0.5 &&
    a.column === b.column &&
    (!compareHitsound || a.hitSound === b.hitSound) &&
    endsAgree &&
    samePath
  );
}

/**
 * Matches the objects of a hunk before and after: the same object (even if it moved a little)
 * becomes one pair, the rest were added or removed. Both lists come sorted by time.
 */
export function pairObjects(
  before: ApiSnapshotVisualObject[],
  after: ApiSnapshotVisualObject[],
  /** A time shift every object shares (an offset change), which is not a change to each object. */
  shift = 0,
  /**
   * Whether a different hit sound makes an object changed. Only taiko, where hit sounds are what
   * a note looks like; elsewhere they are a change of their own, shown with the hitsounds.
   */
  compareHitsound = false,
  /**
   * The stretch of the song being shown. The objects given reach past it, so the ones at its edges
   * can find their counterpart; only pairs that are in it are returned.
   */
  range?: [number, number]
): PairedObject[] {
  const usedBefore = new Set<number>();
  const pairs: { b: number; a: number }[] = [];

  const sameSpot = (b: ApiSnapshotVisualObject, a: ApiSnapshotVisualObject) =>
    b.type === a.type && (b.column ?? -1) === (a.column ?? -1);

  const pair = (match: (b: ApiSnapshotVisualObject, a: ApiSnapshotVisualObject) => boolean) => {
    const usedAfter = new Set(pairs.map((p) => p.a));

    after.forEach((a, ai) => {
      if (usedAfter.has(ai)) return;

      let best = -1;
      let bestCost = Infinity;

      before.forEach((b, bi) => {
        if (usedBefore.has(bi) || !sameSpot(b, a) || !match(b, a)) return;

        const cost = distance(a, b) + timeGap(a.time, b.time, shift) / 10;
        if (cost < bestCost) {
          best = bi;
          bestCost = cost;
        }
      });

      if (best >= 0) {
        usedBefore.add(best);
        pairs.push({ b: best, a: ai });
      }
    });
  };

  pair((b, a) => timeGap(a.time, b.time, shift) < 1);
  pair(
    (b, a) =>
      timeGap(a.time, b.time, shift) <= RETIME_WINDOW_MS && distance(a, b) <= RETIME_MAX_DISTANCE
  );

  const pairedAfter = new Map(pairs.map((p) => [p.a, p.b]));
  const result: PairedObject[] = [];

  after.forEach((a, ai) => {
    const bi = pairedAfter.get(ai);
    if (bi === undefined) {
      result.push({ after: a, status: 'added', details: [] });
    } else {
      const b = before[bi];
      result.push({
        before: b,
        after: a,
        status: identical(b, a, shift, compareHitsound) ? 'same' : 'changed',
        details: describeDifference(b, a, shift, compareHitsound),
      });
    }
  });

  before.forEach((b, bi) => {
    if (!usedBefore.has(bi)) result.push({ before: b, status: 'removed', details: [] });
  });

  const inRange = (pair: PairedObject) => {
    if (!range) return true;

    // A removed object only exists in the old version, whose times are before the shift.
    const object = (pair.after ?? pair.before)!;
    const offset = pair.after ? 0 : shift;
    const time = object.time + offset;
    const last = (object.endTime ?? object.time) + offset;

    return time < range[1] && last >= range[0];
  };

  return result
    .filter(inRange)
    .sort((x, y) => (x.after ?? x.before)!.time - (y.after ?? y.before)!.time);
}

const noteName = (hitSound: string) => {
  const big = hasFinishSound(hitSound) ? 'big ' : '';
  return `${big}${hasKatSound(hitSound) ? 'kat' : 'don'}`;
};

const place = (o: ApiSnapshotVisualObject) => `(${Math.round(o.x)}, ${Math.round(o.y)})`;

/** In words, how an object differs between two versions; what the hover card lists. */
function describeDifference(
  b: ApiSnapshotVisualObject,
  a: ApiSnapshotVisualObject,
  shift: number,
  compareHitsound: boolean
): string[] {
  const lines: string[] = [];

  if (timeGap(a.time, b.time, shift) >= 1)
    lines.push(`Moved in time from ${formatClock(b.time)} to ${formatClock(a.time)}`);
  if (distance(a, b) >= 0.5) lines.push(`Moved from ${place(b)} to ${place(a)}`);
  if (a.column !== b.column) lines.push(`Moved from column ${b.column} to column ${a.column}`);
  if (compareHitsound && a.hitSound !== b.hitSound)
    lines.push(`Changed from ${noteName(b.hitSound)} to ${noteName(a.hitSound)}`);

  if (a.type === 'Slider' && b.type === 'Slider') {
    if (JSON.stringify(a.path) !== JSON.stringify(b.path)) lines.push('Slider shape changed');
    const reverses = (b.edges?.length ?? 0) !== (a.edges?.length ?? 0);
    if (reverses)
      lines.push(`Reverses changed from ${b.edges?.length ?? 0} to ${a.edges?.length ?? 0}`);
  } else if (a.endTime != null && b.endTime != null && timeGap(a.endTime, b.endTime, shift) >= 1) {
    lines.push(`Ends at ${formatClock(a.endTime)} instead of ${formatClock(b.endTime)}`);
  }

  return lines;
}

export const hasKatSound = (hitSound: string) => /whistle|clap/i.test(hitSound);
export const hasFinishSound = (hitSound: string) => /finish/i.test(hitSound);
