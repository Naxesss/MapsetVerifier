import { Group, Text } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import { pairObjects, STATUS_COLOR, type PairedObject } from './pairing';
import PairTip from './PairTip';
import SnapGrid, { hunkDivisors, SnapLegend } from './SnapGrid';
import type {
  ApiSnapshotHunkVisual,
  ApiSnapshotTimingMark,
  ApiSnapshotVisualObject,
} from '../../../Types';

const LANE = 30;
/** Every picture is the same size, so scrolling never changes the zoom. */
const FIELD_HEIGHT = 300;
const GAP = 40;

function Field({
  x0,
  label,
  columns,
  height,
  scale,
  items,
  timing,
  divisors,
  from,
  to,
  clipId,
}: {
  x0: number;
  label: string;
  columns: number;
  height: number;
  scale: (time: number) => number;
  items: {
    object: ApiSnapshotVisualObject;
    status: keyof typeof STATUS_COLOR;
    pair: PairedObject;
  }[];
  timing: ApiSnapshotTimingMark[];
  divisors: number[];
  from: number;
  to: number;
  clipId: string;
}) {
  const width = columns * LANE;

  return (
    <g>
      <text
        x={x0 + width / 2}
        y={12}
        textAnchor="middle"
        fontSize={10.5}
        fontWeight={700}
        fill="#8695AB"
      >
        {label}
      </text>
      <rect x={x0} y={20} width={width} height={height} rx={4} fill="#121923" stroke="#353F52" />
      <clipPath id={clipId}>
        <rect x={x0} y={20} width={width} height={height} rx={4} />
      </clipPath>
      <g clipPath={`url(#${clipId})`}>
        {Array.from({ length: columns - 1 }, (_, i) => (
          <line
            key={i}
            x1={x0 + (i + 1) * LANE}
            x2={x0 + (i + 1) * LANE}
            y1={20}
            y2={20 + height}
            stroke="#1B2330"
          />
        ))}
        <SnapGrid
          timing={timing}
          divisors={divisors}
          from={from}
          to={to}
          place={scale}
          across={{ start: x0, end: x0 + width }}
        />
        {items.map(({ object, status, pair }, i) => (
          <PairTip key={i} pair={pair}>
            {(() => {
              const column = object.column ?? 0;
              const y = scale(object.time);
              const x = x0 + column * LANE + 3;
              const outline = status === 'same' ? null : STATUS_COLOR[status];
              const fill = column === 1 || column === columns - 2 ? '#8FB8F0' : '#E9EDF3';
              const top = object.endTime != null ? scale(object.endTime) : y;

              return (
                <g key={i}>
                  {object.endTime != null && (
                    <rect
                      x={x + 6}
                      y={top}
                      width={LANE - 18}
                      height={Math.max(y - top, 2)}
                      rx={3}
                      fill={fill}
                      fillOpacity={0.35}
                    />
                  )}
                  <rect x={x} y={y - 4.5} width={LANE - 6} height={9} rx={2} fill={fill} />
                  {outline && (
                    <rect
                      x={x - 2.5}
                      y={top - 7}
                      width={LANE - 1}
                      height={y - top + 14}
                      rx={4}
                      fill="none"
                      stroke={outline}
                      strokeWidth={2}
                      strokeDasharray={status === 'removed' ? '4 3' : undefined}
                    />
                  )}
                </g>
              );
            })()}
          </PairTip>
        ))}
      </g>
    </g>
  );
}

/**
 * Before and after as two sets of lanes, time running upward like the editor. Notes pair up by
 * column and time, so a note moved to another column shows as removed in one and added in the other.
 */
export default function ManiaLanes({
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

  const columns = Math.max(
    4,
    ...[...visual.before, ...visual.after].map((o) => (o.column ?? 0) + 1)
  );
  // The window is a fixed stretch of the song on a fixed canvas, so the zoom never changes.
  const lastTime = end;
  const span = Math.max(end - start, 1);
  const height = FIELD_HEIGHT;
  const clipId = useId();
  const scale = (time: number) => 20 + height - 14 - ((time - start) / span) * (height - 28);

  const before = pairs
    .filter((p) => p.before)
    .map((p) => ({
      object: p.before!,
      status: p.status === 'added' ? ('same' as const) : p.status,
      pair: p,
    }));
  const after = pairs
    .filter((p) => p.after)
    .map((p) => ({ object: p.after!, status: p.status, pair: p }));

  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const divisors = fitDivisors(visual.afterTiming, used, start, lastTime, scale);
  const fieldWidth = columns * LANE;
  const width = fieldWidth * 2 + GAP + 8;

  return (
    <div>
      <svg
        viewBox={`0 0 ${width} ${height + 28}`}
        role="img"
        aria-label="Mania notes before and after"
        style={{ display: 'block', width: '100%', maxWidth: width, height: 'auto' }}
      >
        <Field
          x0={4}
          label="BEFORE"
          columns={columns}
          height={height}
          scale={scale}
          items={before}
          timing={visual.beforeTiming}
          divisors={divisors}
          from={start}
          to={lastTime}
          clipId={`${clipId}-before`}
        />
        <Field
          x0={4 + fieldWidth + GAP}
          label="AFTER"
          columns={columns}
          height={height}
          scale={scale}
          items={after}
          timing={visual.afterTiming}
          divisors={divisors}
          from={start}
          to={lastTime}
          clipId={`${clipId}-after`}
        />
      </svg>
      <Group gap="md" mt="xs" wrap="wrap">
        <Text size="xs" c="dimmed">
          Outline: yellow moved or lengthened, green added, red removed
        </Text>
        <SnapLegend divisors={divisors} />
      </Group>
    </div>
  );
}
