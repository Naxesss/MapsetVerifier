import { Box } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import Legend from './Legend';
import {
  hasFinishSound,
  hasKatSound,
  pairObjects,
  PALETTE,
  STATUS_COLOR,
  type PairedObject,
} from './pairing';
import PairTip from './PairTip';
import { LabelledPlot } from './PlotLabels';
import SnapGrid, { hunkDivisors } from './SnapGrid';
import type { ApiSnapshotHunkVisual, ApiSnapshotVisualObject } from '../../../Types';

const WIDTH = 640;
const LEFT = 58;
const RIGHT = 16;
/** The part of the drawing the picture shows: the two strips. */
const VIEW_TOP = 10;
const VIEW_HEIGHT = 98;
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
    () => pairObjects(visual.before, visual.after, shift, true, undefined, true),
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
      <LabelledPlot rows={rows} view={[VIEW_TOP, VIEW_HEIGHT]} start={start} end={end}>
        <svg
          viewBox={`${LEFT - 8} ${VIEW_TOP} ${WIDTH - LEFT - RIGHT + 16} ${VIEW_HEIGHT}`}
          role="img"
          aria-label="Taiko notes before and after on the beat grid"
          style={{ display: 'block', width: '100%', height: 'auto' }}
        >
          <clipPath id={clipId}>
            <rect x={LEFT - 8} y={0} width={WIDTH - LEFT - RIGHT + 16} height={124} rx={4} />
          </clipPath>
          {rows.map((row) => (
            <g key={row.label}>
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
        </svg>
      </LabelledPlot>
      <Box mt="xs">
        <Legend
          items={[
            { mark: 'dot', color: DON, label: 'Don' },
            { mark: 'dot', color: KAT, label: 'Kat' },
            { mark: 'bigDot', color: PALETTE.muted, label: 'Big' },
            { mark: 'ring', color: STATUS_COLOR.changed, label: 'Changed' },
            { mark: 'ring', color: STATUS_COLOR.added, label: 'Added' },
            { mark: 'ring', color: STATUS_COLOR.removed, label: 'Removed' },
          ]}
          divisors={divisors}
        />
      </Box>
    </div>
  );
}
