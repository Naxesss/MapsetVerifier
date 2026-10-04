import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import { PALETTE, pairObjects, STATUS_COLOR } from './pairing';
import PairTip from './PairTip';
import SnapGrid, { hunkDivisors, SnapLegend } from './SnapGrid';
import { formatClock } from '../describe';
import type { ApiSnapshotHunkVisual } from '../../../Types';

const WIDTH = 640;
const LEFT = 56;
const RIGHT = 8;
/** The space a dot needs between its neighbours to show its number. */
const NUMBER_ROOM = 19;

/**
 * The hunk's rhythm as two lanes over song time, before above after, so a retimed, added or
 * removed object shows as a dot that moved, appeared or disappeared.
 */
export default function RhythmLanes({
  visual,
  start,
  end,
  shift = 0,
}: {
  visual: ApiSnapshotHunkVisual;
  start: number;
  end: number;
  /** A time shift every object shares, which is not a change to each object. */
  shift?: number;
}) {
  const pairs = useMemo(
    () => pairObjects(visual.before, visual.after, shift, false, [start, end]),
    [visual, shift, start, end]
  );

  // Exactly the window, so the zoom is the same however far it is scrolled.
  const from = start;
  const span = Math.max(end - start, 1);
  const clipId = useId();
  const x = (time: number) => LEFT + ((time - from) / span) * (WIDTH - LEFT - RIGHT);

  // The same beat grid as the other modes, so the rhythm reads against its snaps.
  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const divisors = fitDivisors(visual.afterTiming, used, start, end, x);

  // A dot is big enough for its combo number when its neighbours leave room; in a dense stretch it
  // stays a small dot.
  const crowded = useMemo(() => {
    const result = new Set<unknown>();
    for (const side of ['before', 'after'] as const) {
      const objects = pairs
        .map((pair) => pair[side])
        .filter((object) => object != null)
        .sort((a, b) => a.time - b.time);

      objects.forEach((object, i) => {
        const near = [objects[i - 1], objects[i + 1]].some(
          (other) => other && Math.abs(x(other.time) - x(object.time)) < NUMBER_ROOM
        );
        if (near) result.add(object);
      });
    }

    return result;
  }, [pairs, start, end]);

  return (
    <div>
      <svg
        viewBox={`0 0 ${WIDTH} 96`}
        role="img"
        aria-label="Rhythm before and after"
        style={{ display: 'block', width: '100%', maxWidth: WIDTH, height: 'auto' }}
      >
        {[
          { label: 'BEFORE', y: 30, timing: visual.beforeTiming },
          { label: 'AFTER', y: 66, timing: visual.afterTiming },
        ].map((lane) => (
          <g key={lane.label}>
            <text x={0} y={lane.y + 4} fontSize={10.5} fontWeight={700} fill={PALETTE.dim}>
              {lane.label}
            </text>
            <rect
              x={LEFT}
              y={lane.y - 14}
              width={WIDTH - LEFT - RIGHT}
              height={28}
              rx={4}
              fill={PALETTE.surface}
            />
            <SnapGrid
              timing={lane.timing}
              divisors={divisors}
              from={start}
              to={end}
              place={x}
              across={{ start: lane.y - 14, end: lane.y + 14, vertical: true }}
            />
          </g>
        ))}

        <clipPath id={clipId}>
          <rect x={LEFT} y={0} width={WIDTH - LEFT - RIGHT} height={80} />
        </clipPath>
        <g clipPath={`url(#${clipId})`}>
          {pairs.map((pair, i) => {
            const draw = (side: 'before' | 'after') => {
              const object = pair[side];
              if (!object) return null;

              const y = side === 'before' ? 30 : 66;
              const color = STATUS_COLOR[pair.status];
              const dim = pair.status === 'same';
              const x1 = x(object.time);
              const x2 = object.endTime != null ? x(object.endTime) : x1;
              const numbered = object.combo != null && !crowded.has(object);

              return (
                <PairTip key={`${side}-${i}`} pair={pair}>
                  <g>
                    {x2 - x1 > 3 && (
                      <rect
                        x={x1}
                        y={y - 5}
                        width={x2 - x1}
                        height={10}
                        rx={5}
                        fill={color}
                        fillOpacity={0.4}
                        stroke={color}
                      />
                    )}
                    {object.edges?.map((time) => (
                      <line
                        key={`edge-${time}`}
                        x1={x(time)}
                        x2={x(time)}
                        y1={y - 7}
                        y2={y + 7}
                        stroke={color}
                        strokeWidth={2}
                      />
                    ))}
                    {object.ticks?.map((time) => (
                      <circle
                        key={`tick-${time}`}
                        cx={x(time)}
                        cy={y}
                        r={2}
                        fill="var(--mantine-color-gray-0)"
                        fillOpacity={0.9}
                      />
                    ))}
                    <circle
                      cx={x1}
                      cy={y}
                      r={numbered ? 9 : 5.5}
                      fill={dim ? '#5A677C' : color}
                      stroke="#161D28"
                      strokeWidth={1.5}
                    />
                    {numbered && (
                      <text
                        x={x1}
                        y={y + 3.5}
                        textAnchor="middle"
                        fontSize={10}
                        fontWeight={800}
                        fill="#11161D"
                      >
                        {object.combo}
                      </text>
                    )}
                  </g>
                </PairTip>
              );
            };

            return (
              <g key={i}>
                {pair.before &&
                  pair.after &&
                  pair.status === 'changed' &&
                  Math.abs(pair.before.time - pair.after.time) > 1 && (
                    <line
                      x1={x(pair.before.time)}
                      y1={36}
                      x2={x(pair.after.time)}
                      y2={60}
                      stroke={STATUS_COLOR.changed}
                      strokeWidth={1.5}
                      strokeDasharray="3 3"
                    />
                  )}
                {draw('before')}
                {draw('after')}
              </g>
            );
          })}
        </g>
        <text x={LEFT} y={94} fontSize={10} fill={PALETTE.dim} fontFamily="monospace">
          {formatClock(start)}
        </text>
        <text
          x={WIDTH - RIGHT}
          y={94}
          fontSize={10}
          textAnchor="end"
          fill={PALETTE.dim}
          fontFamily="monospace"
        >
          {formatClock(end)}
        </text>
      </svg>
      <SnapLegend divisors={divisors} />
    </div>
  );
}
