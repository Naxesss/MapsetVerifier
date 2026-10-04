import { Group, Text } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import {
  hasFinishSound,
  hasKatSound,
  pairObjects,
  STATUS_COLOR,
  type PairedObject,
} from './pairing';
import PairTip from './PairTip';
import SnapGrid, { hunkDivisors, SnapLegend } from './SnapGrid';
import { formatClock } from '../describe';
import type { ApiSnapshotHunkVisual, ApiSnapshotVisualObject } from '../../../Types';

const WIDTH = 640;
const LEFT = 58;
const RIGHT = 16;
const DON = '#F2503F';
const KAT = '#3FA4D8';

function Note({
  object,
  x,
  endX,
  cy,
  status,
}: {
  object: ApiSnapshotVisualObject;
  x: number;
  endX: number | null;
  cy: number;
  status: PairedObject['status'];
}) {
  const big = hasFinishSound(object.hitSound);
  const radius = big ? 12.5 : 9;

  // Drumrolls and shakers last for a while, so they are drawn as bars from start to end.
  if ((object.type === 'Slider' || object.type === 'Spinner') && endX != null) {
    const color = object.type === 'Slider' ? '#E8A33D' : '#FF922B';

    return (
      <rect
        x={x}
        y={cy - (big ? 9 : 6)}
        width={Math.max(endX - x, 10)}
        height={big ? 18 : 12}
        rx={6}
        fill={color}
        stroke={status === 'same' ? 'none' : STATUS_COLOR[status]}
        strokeWidth={2}
      />
    );
  }

  return (
    <g>
      {status !== 'same' && (
        <circle
          cx={x}
          cy={cy}
          r={radius + 4.5}
          fill="none"
          stroke={STATUS_COLOR[status]}
          strokeWidth={2.5}
        />
      )}
      <circle
        cx={x}
        cy={cy}
        r={radius}
        fill={hasKatSound(object.hitSound) ? KAT : DON}
        stroke="#fff"
        strokeOpacity={0.85}
        strokeWidth={2}
      />
    </g>
  );
}

/**
 * Before and after as two note strips on one shared time axis, with the beat grid behind them, so a
 * note that changed colour or size, or was added or removed, sits right under or over what it
 * replaced, and its place on the grid shows which snap it uses. The strip is always the same width
 * and shows the same stretch of the song, so scrolling never changes the zoom.
 */
export default function TaikoStrip({
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
    () => pairObjects(visual.before, visual.after, shift, true),
    [visual, shift]
  );
  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const clipId = useId();

  const span = Math.max(end - start, 1);
  const xOf = (time: number) => LEFT + ((time - start) / span) * (WIDTH - LEFT - RIGHT);
  const divisors = fitDivisors(visual.afterTiming, used, start, end, xOf);

  const rows = [
    { label: 'BEFORE', y: 34, side: 'before' as const, timing: visual.beforeTiming },
    { label: 'AFTER', y: 84, side: 'after' as const, timing: visual.afterTiming },
  ];

  return (
    <div>
      <svg
        viewBox={`0 0 ${WIDTH} 124`}
        role="img"
        aria-label="Taiko notes before and after on the beat grid"
        style={{ display: 'block', width: '100%', maxWidth: WIDTH, height: 'auto' }}
      >
        <clipPath id={clipId}>
          <rect x={LEFT - 8} y={0} width={WIDTH - LEFT - RIGHT + 16} height={124} rx={4} />
        </clipPath>
        {rows.map((row) => (
          <g key={row.label}>
            <text x={0} y={row.y + 4} fontSize={10.5} fontWeight={700} fill="#8695AB">
              {row.label}
            </text>
            <rect
              x={LEFT - 8}
              y={row.y - 24}
              width={WIDTH - LEFT - RIGHT + 16}
              height={48}
              rx={4}
              fill="#121923"
            />
            <g clipPath={`url(#${clipId})`}>
              <SnapGrid
                timing={row.timing}
                divisors={divisors}
                from={start}
                to={end}
                place={xOf}
                across={{ start: row.y - 22, end: row.y + 22, vertical: true }}
              />
              {pairs.map((pair, i) => {
                const object = pair[row.side];
                if (!object) return null;

                return (
                  <PairTip key={i} pair={pair}>
                    <g>
                      <Note
                        object={object}
                        x={xOf(object.time)}
                        endX={object.endTime != null ? xOf(object.endTime) : null}
                        cy={row.y}
                        status={pair.status}
                      />
                    </g>
                  </PairTip>
                );
              })}
            </g>
          </g>
        ))}
        <text x={LEFT} y={120} fontSize={10} fill="#8695AB" fontFamily="monospace">
          {formatClock(start)}
        </text>
        <text
          x={WIDTH - RIGHT}
          y={120}
          fontSize={10}
          textAnchor="end"
          fill="#8695AB"
          fontFamily="monospace"
        >
          {formatClock(end)}
        </text>
      </svg>
      <Group gap="md" mt="xs" wrap="wrap">
        <Text size="xs" c="dimmed">
          Red don, blue kat, big notes are larger
        </Text>
        <Text size="xs" c="dimmed">
          Ring: yellow changed, green added, red removed
        </Text>
        <SnapLegend divisors={divisors} />
      </Group>
    </div>
  );
}
