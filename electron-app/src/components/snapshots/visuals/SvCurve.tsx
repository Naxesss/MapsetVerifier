import { useMemo } from 'react';
import { formatClock } from '../describe';
import type { ApiSnapshotChange } from '../../../Types';

const WIDTH = 640;
const HEIGHT = 150;
const LEFT = 44;
const RIGHT = 12;
const TOP = 14;
const BOTTOM = 28;

type Step = { time: number; before: number; after: number };

/** The slider velocity changes of a hunk as a step curve, the old one dashed under the new one. */
export function hasSvChanges(changes: ApiSnapshotChange[]) {
  return changes.some((c) => c.field === 'Sv' && c.before != null && c.after != null);
}

export default function SvCurve({
  changes,
  start,
  end,
}: {
  changes: ApiSnapshotChange[];
  start: number;
  end: number;
}) {
  const steps = useMemo<Step[]>(
    () =>
      changes
        .filter((c) => c.field === 'Sv' && c.before != null && c.after != null)
        .map((c) => ({ time: c.time, before: Number(c.before), after: Number(c.after) }))
        .sort((a, b) => a.time - b.time),
    [changes]
  );

  if (steps.length === 0) return null;

  const pad = Math.max(300, (end - start) * 0.08);
  const from = start - pad;
  const span = end - start + pad * 2;
  const values = steps.flatMap((s) => [s.before, s.after]);
  const lo = Math.min(...values) - 0.05;
  const hi = Math.max(...values) + 0.05;

  const x = (t: number) => LEFT + ((t - from) / span) * (WIDTH - LEFT - RIGHT);
  const y = (v: number) => TOP + (1 - (v - lo) / (hi - lo)) * (HEIGHT - TOP - BOTTOM);

  // Before the first change both curves sit at the old value; after it, each at its own.
  const path = (pick: 'before' | 'after') => {
    let d = `M${x(from)} ${y(steps[0][pick === 'before' ? 'before' : 'before'])}`;
    steps.forEach((s, i) => {
      const previous =
        i === 0 ? s.before : pick === 'before' ? steps[i - 1].before : steps[i - 1].after;
      d += ` H${x(s.time)} V${y(previous)} V${y(s[pick])}`;
    });
    d += ` H${x(from + span)}`;
    return d;
  };

  const ticks = [lo + 0.05, (lo + hi) / 2, hi - 0.05];

  return (
    <svg
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      role="img"
      aria-label="Slider velocity before and after"
      style={{ display: 'block', width: '100%', maxWidth: WIDTH, height: 'auto' }}
    >
      {ticks.map((v) => (
        <g key={v}>
          <line x1={LEFT} x2={WIDTH - RIGHT} y1={y(v)} y2={y(v)} stroke="#232C3A" />
          <text
            x={LEFT - 6}
            y={y(v) + 4}
            textAnchor="end"
            fontSize={10.5}
            fill="#8695AB"
            fontFamily="monospace"
          >
            {v.toFixed(2)}x
          </text>
        </g>
      ))}
      <path d={path('before')} fill="none" stroke="#A3ADBD" strokeWidth={2} strokeDasharray="6 5" />
      <path
        d={path('after')}
        fill="none"
        stroke="#FF922B"
        strokeWidth={2.5}
        strokeLinejoin="round"
      />
      <text x={LEFT} y={HEIGHT - 8} fontSize={10} fill="#8695AB" fontFamily="monospace">
        {formatClock(start)}
      </text>
      <text
        x={WIDTH - RIGHT}
        y={HEIGHT - 8}
        fontSize={10}
        textAnchor="end"
        fill="#8695AB"
        fontFamily="monospace"
      >
        {formatClock(end)}
      </text>
    </svg>
  );
}
