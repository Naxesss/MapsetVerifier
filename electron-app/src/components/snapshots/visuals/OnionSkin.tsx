import { Group, SegmentedControl, Text } from '@mantine/core';
import { useId, useMemo, useState } from 'react';
import { PALETTE, pairObjects, STATUS_COLOR, type PairedObject } from './pairing';
import PairTip from './PairTip';
import type { ApiSnapshotHunkVisual, ApiSnapshotVisualObject } from '../../../Types';

const RADIUS = 24;
const PAD = 28;
const FIELD_WIDTH = 512;
const FIELD_HEIGHT = 384;

type View = 'before' | 'both' | 'after';

function pathData(path: number[][]) {
  return path.map(([x, y], i) => `${i === 0 ? 'M' : 'L'}${x} ${y}`).join(' ');
}

/** The editor's grid: lines every 32 osu!pixels across the 512 x 384 playfield. */
const GRID = 32;

type Mark = { x: number; y: number; angle: number; reverse: boolean };

/**
 * Where a slider ends and where it turns around. The path runs from the head to the far end once;
 * every turn alternates between the two, and the tail is wherever the last slide stops.
 */
function sliderMarks(object: ApiSnapshotVisualObject): Mark[] {
  const path = object.path;
  if (!path || path.length < 2) return [];

  const last = path.length - 1;
  const ends = [
    { point: path[0], next: path[1] },
    { point: path[last], next: path[last - 1] },
  ];
  const turns = object.edges?.length ?? 0;
  const at = (slide: number) => ends[slide % 2];
  const mark = (slide: number, reverse: boolean): Mark => {
    const { point, next } = at(slide);

    return {
      x: point[0],
      y: point[1],
      angle: (Math.atan2(next[1] - point[1], next[0] - point[0]) * 180) / Math.PI,
      reverse,
    };
  };

  // Slide 0 ends at the far end, slide 1 back at the head, and so on.
  return [...Array.from({ length: turns }, (_, i) => mark(i + 1, true)), mark(turns + 1, false)];
}

function SliderMarks({
  object,
  color,
  ghost,
}: {
  object: ApiSnapshotVisualObject;
  color: string;
  ghost?: boolean;
}) {
  const stroke = ghost ? PALETTE.muted : color;

  return (
    <g fill="none" stroke={stroke} strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round">
      {sliderMarks(object).map((mark, i) => (
        // Where the slider stops (an open ring) or turns around (a solid disc with an arrow back
        // along the slider, like the game's reverse arrow).
        <g key={i} transform={`translate(${mark.x} ${mark.y})`}>
          {mark.reverse ? (
            <>
              <circle
                r={RADIUS - 3}
                fill={stroke}
                fillOpacity={ghost ? 0.35 : 0.95}
                strokeDasharray={ghost ? '5 4' : undefined}
              />
              <path
                d="M-4 -8 L6 0 L-4 8"
                stroke={ghost ? '#161D28' : PALETTE.ink}
                strokeWidth={4}
                transform={`rotate(${mark.angle})`}
              />
            </>
          ) : (
            <circle r={RADIUS - 4} strokeDasharray={ghost ? '5 4' : undefined} />
          )}
        </g>
      ))}
    </g>
  );
}

function Circle({
  x,
  y,
  status,
  ghost,
  label,
}: {
  x: number;
  y: number;
  status: PairedObject['status'];
  ghost?: boolean;
  label?: number;
}) {
  const color = STATUS_COLOR[status];

  return (
    <g>
      <circle
        cx={x}
        cy={y}
        r={RADIUS}
        fill={ghost ? 'none' : color}
        fillOpacity={ghost ? 0 : 0.92}
        stroke={ghost ? (status === 'removed' ? color : '#A3ADBD') : 'none'}
        strokeWidth={2}
        strokeDasharray={ghost ? '5 4' : undefined}
      />
      {ghost && status === 'removed' && (
        <path
          d={`M${x - 11} ${y - 11} l22 22 m0 -22 l-22 22`}
          stroke={color}
          strokeWidth={3}
          strokeLinecap="round"
        />
      )}
      {!ghost && label !== undefined && (
        <text x={x} y={y + 5} textAnchor="middle" fontSize={15} fontWeight={800} fill="#11161D">
          {label}
        </text>
      )}
    </g>
  );
}

/**
 * The hunk's objects on the playfield: the old layout as ghosts under the new one, with arrows for
 * what moved. Used for osu!; catch has its own falling-fruit view.
 */
export default function OnionSkin({
  visual,
  shift = 0,
  range,
}: {
  visual: ApiSnapshotHunkVisual;
  shift?: number;
  /** The stretch of the song shown; objects past it only help the edges pair up. */
  range?: [number, number];
}) {
  const [view, setView] = useState<View>('both');
  const arrow = `${useId()}-arrow`;
  const pairs = useMemo(
    () => pairObjects(visual.before, visual.after, shift, false, range),
    [visual, shift, range]
  );

  const showBefore = view !== 'after';
  const showAfter = view !== 'before';

  return (
    <div>
      <Group justify="space-between" mb="xs" gap="xs" wrap="wrap">
        <SegmentedControl
          size="xs"
          value={view}
          onChange={(value) => setView(value as View)}
          data={[
            { value: 'before', label: 'Before' },
            { value: 'both', label: 'Both' },
            { value: 'after', label: 'After' },
          ]}
        />
        <Text size="xs" c="dimmed">
          Dashed outlines are the old positions. A ring is where a slider ends, a ring with a solid
          centre where it turns around.
        </Text>
      </Group>
      <svg
        viewBox={`${-PAD} ${-PAD} ${FIELD_WIDTH + PAD * 2} ${FIELD_HEIGHT + PAD * 2}`}
        role="img"
        aria-label="Objects of this change on the playfield, before and after"
        style={{ display: 'block', width: '100%', maxWidth: 520, height: 'auto' }}
      >
        <defs>
          <marker
            id={arrow}
            viewBox="0 0 10 10"
            refX={8}
            refY={5}
            markerWidth={6}
            markerHeight={6}
            orient="auto-start-reverse"
          >
            <path d="M0,0 L10,5 L0,10 z" fill={STATUS_COLOR.changed} />
          </marker>
        </defs>
        <rect
          x={0}
          y={0}
          width={FIELD_WIDTH}
          height={FIELD_HEIGHT}
          rx={6}
          fill={PALETTE.surface}
          stroke={PALETTE.line}
        />
        {Array.from({ length: FIELD_WIDTH / GRID - 1 }, (_, i) => (
          <line
            key={`v${i}`}
            x1={(i + 1) * GRID}
            x2={(i + 1) * GRID}
            y1={0}
            y2={FIELD_HEIGHT}
            stroke={PALETTE.grid}
          />
        ))}
        {Array.from({ length: FIELD_HEIGHT / GRID - 1 }, (_, i) => (
          <line
            key={`h${i}`}
            y1={(i + 1) * GRID}
            y2={(i + 1) * GRID}
            x1={0}
            x2={FIELD_WIDTH}
            stroke={PALETTE.grid}
          />
        ))}

        {/* Slider bodies first, so circles sit on top of them. */}
        {pairs.map((pair, i) => (
          <g key={`path-${i}`}>
            {showBefore && pair.before?.path && (pair.status !== 'same' || !showAfter) && (
              <path
                d={pathData(pair.before.path)}
                fill="none"
                stroke={PALETTE.muted}
                strokeOpacity={pair.status === 'same' ? 0.2 : 0.18}
                strokeWidth={RADIUS * 2}
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            )}
            {showAfter && pair.after?.path && (
              <path
                d={pathData(pair.after.path)}
                fill="none"
                stroke={STATUS_COLOR[pair.status]}
                strokeOpacity={pair.status === 'same' ? 0.2 : 0.35}
                strokeWidth={RADIUS * 2}
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            )}
          </g>
        ))}

        {pairs.map((pair, i) => {
          const moved =
            pair.before &&
            pair.after &&
            Math.hypot(pair.before.x - pair.after.x, pair.before.y - pair.after.y) > 6;

          return (
            <PairTip key={i} pair={pair}>
              <g>
                {showBefore && pair.before && pair.status !== 'same' && (
                  <>
                    <SliderMarks object={pair.before} color={STATUS_COLOR[pair.status]} ghost />
                    <Circle x={pair.before.x} y={pair.before.y} status={pair.status} ghost />
                  </>
                )}
                {showBefore && !showAfter && pair.before && pair.status === 'same' && (
                  <SliderMarks object={pair.before} color={STATUS_COLOR.same} />
                )}
                {showBefore && !showAfter && pair.before && pair.status === 'same' && (
                  <Circle
                    x={pair.before.x}
                    y={pair.before.y}
                    status="same"
                    label={pair.before.combo ?? undefined}
                  />
                )}
                {showBoth(view) && moved && (
                  <line
                    x1={pair.before!.x}
                    y1={pair.before!.y}
                    x2={pair.after!.x}
                    y2={pair.after!.y}
                    stroke={STATUS_COLOR.changed}
                    strokeWidth={3}
                    markerEnd={`url(#${arrow})`}
                  />
                )}
                {showAfter && pair.after && (
                  <>
                    <SliderMarks object={pair.after} color={STATUS_COLOR[pair.status]} />
                    <Circle
                      x={pair.after.x}
                      y={pair.after.y}
                      status={pair.status}
                      label={pair.after.combo ?? undefined}
                    />
                  </>
                )}
              </g>
            </PairTip>
          );
        })}
      </svg>
    </div>
  );
}

const showBoth = (view: View) => view === 'both';
