import { Group, Text } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import { pairObjects, STATUS_COLOR, type PairedObject, type PairStatus } from './pairing';
import PairTip from './PairTip';
import SnapGrid, { hunkDivisors, SnapLegend } from './SnapGrid';
import type {
  ApiSnapshotHunkVisual,
  ApiSnapshotTimingMark,
  ApiSnapshotVisualObject,
} from '../../../Types';

/** The playfield is 512 wide; it is drawn at half size to leave room for two fields side by side. */
const X_SCALE = 0.5;
const FIELD_WIDTH = 512 * X_SCALE;
const GAP = 40;
/** Every picture is the same size, so scrolling never changes the zoom. */
const FIELD_HEIGHT = 300;
const FRUIT = 7;
/** The big droplet at a slider tick, and the small ones between them. */
const DROPLET = 4.5;
const DROPLET_TINY = 2.5;

type Item = { object: ApiSnapshotVisualObject; status: PairStatus; pair: PairedObject };

const HYPER_GLOW = 'var(--mantine-color-red-6)';

/** A fruit that starts a hyperdash glows red, like in the game. */
const isHyper = (object: ApiSnapshotVisualObject, time: number) =>
  object.hyperTimes?.some((t) => Math.abs(t - time) < 1.5) ?? false;

function Glow({ x, y }: { x: number; y: number }) {
  return (
    <circle
      cx={x}
      cy={y}
      r={FRUIT + 4.5}
      fill={HYPER_GLOW}
      fillOpacity={0.75}
      filter="url(#catch-glow)"
    />
  );
}

function Field({
  x0,
  label,
  height,
  yOf,
  items,
  ghost,
  timing,
  divisors,
  from,
  to,
  clipId,
}: {
  x0: number;
  label: string;
  height: number;
  yOf: (time: number) => number;
  items: Item[];
  /** The old field: removed objects are outlined instead of filled. */
  ghost?: boolean;
  timing: ApiSnapshotTimingMark[];
  divisors: number[];
  from: number;
  to: number;
  clipId: string;
}) {
  const xOf = (x: number) => x0 + Math.max(0, Math.min(512, x)) * X_SCALE;

  return (
    <g>
      <text
        x={x0 + FIELD_WIDTH / 2}
        y={12}
        textAnchor="middle"
        fontSize={10.5}
        fontWeight={700}
        fill="#8695AB"
      >
        {label}
      </text>
      <rect
        x={x0}
        y={20}
        width={FIELD_WIDTH}
        height={height}
        rx={4}
        fill="#121923"
        stroke="#353F52"
      />
      {[1, 2, 3].map((i) => (
        <line
          key={i}
          x1={x0 + (FIELD_WIDTH / 4) * i}
          x2={x0 + (FIELD_WIDTH / 4) * i}
          y1={20}
          y2={20 + height}
          stroke="#1B2330"
        />
      ))}
      <clipPath id={clipId}>
        <rect x={x0} y={20} width={FIELD_WIDTH} height={height} rx={4} />
      </clipPath>
      <g clipPath={`url(#${clipId})`}>
        <SnapGrid
          timing={timing}
          divisors={divisors}
          from={from}
          to={to}
          place={yOf}
          across={{ start: x0, end: x0 + FIELD_WIDTH }}
        />
        {/* The catcher's line, where fruits are caught. */}
        <line
          x1={x0}
          x2={x0 + FIELD_WIDTH}
          y1={20 + height - 8}
          y2={20 + height - 8}
          stroke="#353F52"
          strokeDasharray="4 4"
        />
        {items.map(({ object, status, pair }, i) => (
          <PairTip key={i} pair={pair}>
            {(() => {
              const color = STATUS_COLOR[status];
              const outlined = ghost && status === 'removed';
              const fill = outlined ? 'none' : color;
              const opacity = status === 'same' ? 0.6 : 1;
              const y = yOf(object.time);

              // A banana shower fills the width for as long as it lasts.
              if (object.type === 'Spinner' && object.endTime != null) {
                const top = yOf(object.endTime);

                return (
                  <rect
                    key={i}
                    x={x0 + 4}
                    y={top}
                    width={FIELD_WIDTH - 8}
                    height={Math.max(y - top, 3)}
                    rx={4}
                    fill="#FCC419"
                    fillOpacity={status === 'same' ? 0.18 : 0.35}
                    stroke={color}
                    strokeDasharray={outlined ? '4 3' : undefined}
                  />
                );
              }

              // A juice stream: a fruit at its start, a fruit at each reverse and at its end, a big
              // droplet at every slider tick and small droplets in between, like in the game.
              if (object.type === 'Slider' && object.path && object.endTime != null) {
                const path = object.path;
                const start = object.time;
                const end = object.endTime;
                const slides = (object.edges?.length ?? 0) + 1;
                const slide = (end - start) / slides;

                // Where the stream is at a time: each slide runs the path once, every other one back.
                const at = (time: number) => {
                  const n = Math.min(slides - 1, Math.max(0, Math.floor((time - start) / slide)));
                  const along = Math.min(1, Math.max(0, (time - start - n * slide) / slide));
                  const position = (n % 2 === 0 ? along : 1 - along) * (path.length - 1);
                  const index = Math.min(path.length - 2, Math.floor(position));
                  const [x1] = path[index];
                  const [x2] = path[index + 1] ?? path[index];

                  return { x: xOf(x1 + (x2 - x1) * (position - index)), y: yOf(time) };
                };

                const ticks = (object.ticks ?? []).filter((t) => t > start && t < end);

                // Small droplets fill the gaps between the bigger ones, about every 100 ms or less.
                const tiny: number[] = [];
                for (let n = 0; n < slides; n++) {
                  const stops = [
                    start + n * slide,
                    ...ticks.filter((t) => t > start + n * slide && t < start + (n + 1) * slide),
                    start + (n + 1) * slide,
                  ];

                  for (let k = 1; k < stops.length; k++) {
                    let step = stops[k] - stops[k - 1];
                    while (step > 100) step /= 2;
                    if (step < 1) continue;

                    for (let t = stops[k - 1] + step; t < stops[k] - step / 2; t += step)
                      tiny.push(t);
                  }
                }

                const edgeTimes = [...(object.edges ?? []), end];
                const fruit = (time: number, key: string) => {
                  const p = at(time);

                  return (
                    <g key={key}>
                      {isHyper(object, time) && <Glow x={p.x} y={p.y} />}
                      <circle
                        cx={p.x}
                        cy={p.y}
                        r={FRUIT}
                        fill={fill}
                        stroke={outlined ? color : '#121923'}
                        strokeWidth={1.5}
                        strokeDasharray={outlined ? '3 2' : undefined}
                      />
                    </g>
                  );
                };

                const dots = (times: number[], r: number, key: string) =>
                  times.map((time, d) => {
                    const p = at(time);

                    return (
                      <circle
                        key={`${key}${d}`}
                        cx={p.x}
                        cy={p.y}
                        r={r}
                        fill={fill}
                        stroke={outlined ? color : 'none'}
                      />
                    );
                  });

                return (
                  <g key={i} opacity={opacity}>
                    {dots(tiny, DROPLET_TINY, 'tiny')}
                    {dots(ticks, DROPLET, 'tick')}
                    {edgeTimes.map((time, d) => fruit(time, `edge${d}`))}
                    {fruit(start, 'head')}
                  </g>
                );
              }

              return (
                <g key={i}>
                  {isHyper(object, object.time) && <Glow x={xOf(object.x)} y={y} />}
                  <circle
                    cx={xOf(object.x)}
                    cy={y}
                    r={FRUIT}
                    fill={fill}
                    fillOpacity={opacity}
                    stroke={outlined ? color : '#121923'}
                    strokeWidth={outlined ? 1.5 : 1.5}
                    strokeDasharray={outlined ? '3 2' : undefined}
                  />
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
 * The hunk the way it plays: two catch fields, before and after, with time running upward so the
 * fruits fall toward the catcher at the bottom. Fruits sit at their real horizontal position, so a
 * moved fruit is a fruit that sits somewhere else on the field.
 */
export default function CatchField({
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

  // The window is a fixed stretch of the song on a fixed canvas, so the zoom never changes.
  const lastTime = end;
  const span = Math.max(end - start, 1);
  const height = FIELD_HEIGHT;
  const clipId = useId();
  // Later is higher up; the first object sits just above the catcher's line.
  const yOf = (time: number) => 20 + height - 22 - ((time - start) / span) * (height - 40);

  const before: Item[] = pairs
    .filter((p) => p.before)
    .map((p) => ({ object: p.before!, status: p.status === 'added' ? 'same' : p.status, pair: p }));
  const after: Item[] = pairs
    .filter((p) => p.after)
    .map((p) => ({ object: p.after!, status: p.status, pair: p }));

  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const divisors = fitDivisors(visual.afterTiming, used, start, lastTime, yOf);
  const width = FIELD_WIDTH * 2 + GAP + 8;

  return (
    <div>
      <svg
        viewBox={`0 0 ${width} ${height + 28}`}
        role="img"
        aria-label="Catch fruits before and after, falling toward the catcher"
        style={{ display: 'block', width: '100%', maxWidth: width, height: 'auto' }}
      >
        <defs>
          <filter id="catch-glow" x="-100%" y="-100%" width="300%" height="300%">
            <feGaussianBlur stdDeviation="2.6" />
          </filter>
        </defs>
        <Field
          x0={4}
          label="BEFORE"
          height={height}
          yOf={yOf}
          items={before}
          ghost
          timing={visual.beforeTiming}
          divisors={divisors}
          from={start}
          to={lastTime}
          clipId={`${clipId}-before`}
        />
        <Field
          x0={4 + FIELD_WIDTH + GAP}
          label="AFTER"
          height={height}
          yOf={yOf}
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
          Fruits fall downward. Yellow moved, green added, red dashed removed.
        </Text>
        <SnapLegend divisors={divisors} />
      </Group>
    </div>
  );
}
