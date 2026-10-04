import { Group, Stack, Text, Tooltip } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors } from './beatGrid';
import { PALETTE, pairObjects, STATUS_COLOR, type PairedObject } from './pairing';
import SnapGrid, { hunkDivisors, SnapLegend } from './SnapGrid';
import { formatClock } from '../describe';
import type {
  ApiSnapshotHunkVisual,
  ApiSnapshotSoundPart,
  ApiSnapshotVisualObject,
} from '../../../Types';

const WIDTH = 640;
const LEFT = 64;
const RIGHT = 8;
const ROW = 22;
const ROW_GAP = 6;
const TOP = 30;
const OBJECTS_Y = 14;
const VOLUME_HEIGHT = 36;
const SOUNDS = ['Whistle', 'Finish', 'Clap'] as const;
const MAX_CELL = 22;

/** Which sampleset a cell comes from, in the editor's spirit: normal plain, soft cool, drum warm. */
const SAMPLESET_COLOR: Record<string, string> = {
  Normal: '#A3ADBD',
  Soft: '#4FD1C5',
  Drum: '#FF922B',
};
const samplesetLetter = (sampleset: string) => sampleset.charAt(0) || '?';

/** Whether a slot changed where its sounds come from (the sampleset or the additions' sampleset). */
const samplesetChanged = (slot: { before?: ApiSnapshotSoundPart; after?: ApiSnapshotSoundPart }) =>
  !!slot.before &&
  !!slot.after &&
  (slot.before.sampleset !== slot.after.sampleset || slot.before.addition !== slot.after.addition);

const hasSound = (hitSound: string | undefined, sound: string) =>
  hitSound != null && new RegExp(sound, 'i').test(hitSound);

/** One place an object makes a sound, on each side: its head, a reverse, its tail or its body. */
type Slot = {
  key: string;
  label: string;
  kind: ApiSnapshotSoundPart['kind'];
  time: number;
  /** What plays there before and after; undefined where that side has nothing there. */
  before?: ApiSnapshotSoundPart;
  after?: ApiSnapshotSoundPart;
};

const partsOf = (object: ApiSnapshotVisualObject | undefined): ApiSnapshotSoundPart[] =>
  !object
    ? []
    : (object.sounds ?? [
        { time: object.time, kind: 'Head', hitSound: object.hitSound, sampleset: '', addition: '' },
      ]);

/** Pairs up the sounds of both sides: the head with the head, the n-th reverse with the n-th, and so on. */
function slotsOf(pair: PairedObject): Slot[] {
  const index = (object: ApiSnapshotVisualObject | undefined) => {
    let repeat = 0;

    return new Map(
      partsOf(object).map((part) => [
        part.kind === 'Repeat' ? `Repeat${++repeat}` : part.kind,
        part,
      ])
    );
  };
  const before = index(pair.before);
  const after = index(pair.after);
  const keys = [...new Set([...before.keys(), ...after.keys()])];

  return keys
    .map((key) => {
      const was = before.get(key);
      const now = after.get(key);
      const kind = (now ?? was)!.kind;

      return {
        key,
        kind,
        label:
          kind === 'Head'
            ? ''
            : kind === 'Repeat'
              ? `Reverse ${key.slice(6)}`
              : kind === 'Tail'
                ? 'Tail'
                : 'Slide',
        time: (now ?? was)!.time,
        before: was,
        after: now,
      };
    })
    .sort((x, y) => x.time - y.time);
}

/** What differs about one object, for its hover card. */
function describe(pair: PairedObject, slots: Slot[]): string[] {
  const notes: string[] = [];

  if (pair.status === 'added') notes.push('Object added');
  if (pair.status === 'removed') notes.push('Object removed');

  if (pair.before && pair.after) {
    for (const slot of slots) {
      for (const sound of slot.kind === 'Body' ? ['Whistle'] : SOUNDS) {
        const was = hasSound(slot.before?.hitSound, sound);
        const now = hasSound(slot.after?.hitSound, sound);
        const where = slot.label ? `${slot.label}: ` : '';
        if (!was && now) notes.push(`${where}${sound} added`);
        if (was && !now) notes.push(`${where}${sound} removed`);
      }
    }

    for (const slot of slots) {
      if (slot.kind === 'Body' || !samplesetChanged(slot)) continue;
      const where = slot.label ? `${slot.label}: ` : '';
      if (slot.before!.sampleset !== slot.after!.sampleset)
        notes.push(`${where}Sampleset ${slot.before!.sampleset} to ${slot.after!.sampleset}`);
      if (slot.before!.addition !== slot.after!.addition)
        notes.push(`${where}Additions ${slot.before!.addition} to ${slot.after!.addition}`);
    }

    if (pair.before.volume !== pair.after.volume)
      notes.push(`Volume ${pair.before.volume ?? '?'}% to ${pair.after.volume ?? '?'}%`);
  }

  return notes;
}

function Cell({
  x,
  y,
  width,
  was,
  now,
  lone,
  color,
}: {
  x: number;
  y: number;
  width: number;
  was: boolean;
  now: boolean;
  /** The object only exists on one side, so its sounds are shown as they are, not as changes. */
  lone: boolean;
  color: string;
}) {
  if (!was && !now) return null;

  if (now) {
    return (
      <rect
        x={x - width / 2}
        y={y + 2}
        width={width}
        height={ROW - 4}
        rx={3}
        fill={lone ? color : was ? PALETTE.dot : STATUS_COLOR.added}
      />
    );
  }

  return (
    <g stroke={lone ? color : STATUS_COLOR.removed}>
      <rect
        x={x - width / 2 + 1}
        y={y + 3}
        width={width - 2}
        height={ROW - 6}
        rx={3}
        fill="none"
        strokeWidth={2}
      />
      {!lone && (
        <line
          x1={x - width / 2 + 4}
          x2={x + width / 2 - 4}
          y1={y + ROW - 6}
          y2={y + 6}
          strokeWidth={1.5}
        />
      )}
    </g>
  );
}

/**
 * What a hitsounding pass did, as a step sequencer: the objects along song time, a row each for
 * whistle, finish and clap with a cell where the object plays it, and a row of volume bars. Cells
 * that were added are green, ones that were taken away are outlined in red, and the rest are grey,
 * so a pass reads as a pattern instead of a list.
 */
export default function HitsoundSequencer({
  visual,
  start,
  end,
  shift = 0,
}: {
  visual: ApiSnapshotHunkVisual;
  start: number;
  end: number;
  shift?: number;
}) {
  const pairs = useMemo(
    () => pairObjects(visual.before, visual.after, shift, false, [start, end]),
    [visual, shift, start, end]
  );
  const clipId = useId();

  const span = Math.max(end - start, 1);
  const xOf = (time: number) => LEFT + ((time - start) / span) * (WIDTH - LEFT - RIGHT);

  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const divisors = fitDivisors(visual.afterTiming, used, start, end, xOf);

  // Cells get as wide as the closest two objects allow.
  const xs = pairs.map((p) => xOf((p.after ?? p.before)!.time)).sort((a, b) => a - b);
  let closest = Infinity;
  for (let i = 1; i < xs.length; i++) closest = Math.min(closest, xs[i] - xs[i - 1]);
  const cell = Math.max(5, Math.min(MAX_CELL, closest - 3));

  const showVolume = pairs.some((p) => p.before && p.after && p.before.volume !== p.after.volume);
  const showSampleset = pairs.some((p) => slotsOf(p).some(samplesetChanged));
  const rowY = (i: number) => TOP + i * (ROW + ROW_GAP);
  const samplesetY = rowY(SOUNDS.length);
  const volumeY = samplesetY + (showSampleset ? ROW + ROW_GAP : 0);
  const height = showVolume ? volumeY + VOLUME_HEIGHT + 14 : volumeY + 6;
  const bar = (volume: number) => Math.max(2, (volume / 100) * (VOLUME_HEIGHT - 6));

  return (
    <Stack gap="xs">
      <svg
        viewBox={`0 0 ${WIDTH} ${height}`}
        role="img"
        aria-label="Hitsounds before and after"
        style={{ display: 'block', width: '100%', maxWidth: WIDTH, height: 'auto' }}
      >
        <clipPath id={clipId}>
          <rect x={LEFT} y={0} width={WIDTH - LEFT - RIGHT} height={height} />
        </clipPath>

        <text x={0} y={OBJECTS_Y + 4} fontSize={10.5} fontWeight={700} fill={PALETTE.dim}>
          Objects
        </text>
        {SOUNDS.map((sound, i) => (
          <g key={sound}>
            <text
              x={0}
              y={rowY(i) + ROW / 2 + 4}
              fontSize={10.5}
              fontWeight={700}
              fill={PALETTE.dim}
            >
              {sound}
            </text>
            <rect
              x={LEFT}
              y={rowY(i)}
              width={WIDTH - LEFT - RIGHT}
              height={ROW}
              rx={4}
              fill={PALETTE.surface}
            />
          </g>
        ))}
        {showSampleset && (
          <g>
            <text
              x={0}
              y={samplesetY + ROW / 2 + 4}
              fontSize={10.5}
              fontWeight={700}
              fill={PALETTE.dim}
            >
              Sampleset
            </text>
            <rect
              x={LEFT}
              y={samplesetY}
              width={WIDTH - LEFT - RIGHT}
              height={ROW}
              rx={4}
              fill={PALETTE.surface}
            />
          </g>
        )}
        {showVolume && (
          <g>
            <text
              x={0}
              y={volumeY + VOLUME_HEIGHT / 2 + 4}
              fontSize={10.5}
              fontWeight={700}
              fill={PALETTE.dim}
            >
              Volume
            </text>
            <rect
              x={LEFT}
              y={volumeY}
              width={WIDTH - LEFT - RIGHT}
              height={VOLUME_HEIGHT}
              rx={4}
              fill={PALETTE.surface}
            />
          </g>
        )}

        <g clipPath={`url(#${clipId})`}>
          <SnapGrid
            timing={visual.afterTiming}
            divisors={divisors}
            from={start}
            to={end}
            place={xOf}
            across={{ start: TOP - 4, end: height - 10, vertical: true }}
          />

          {pairs.map((pair, i) => {
            const object = (pair.after ?? pair.before)!;
            const x = xOf(object.time);
            const lone = !pair.before || !pair.after;
            const slots = slotsOf(pair);
            const notes = describe(pair, slots);
            const dot = pair.status === 'same' ? PALETTE.dot : STATUS_COLOR[pair.status];

            const column = (
              <g>
                {object.endTime != null && (
                  <line
                    x1={x}
                    x2={xOf(object.endTime)}
                    y1={OBJECTS_Y}
                    y2={OBJECTS_Y}
                    stroke={dot}
                    strokeOpacity={0.5}
                    strokeWidth={2}
                  />
                )}
                {object.ticks?.map((time) => (
                  <circle key={time} cx={xOf(time)} cy={OBJECTS_Y} r={2} fill={dot} />
                ))}
                {slots
                  .filter((slot) => slot.kind !== 'Body')
                  .map((slot) => (
                    <circle
                      key={slot.key}
                      cx={xOf(slot.time)}
                      cy={OBJECTS_Y}
                      r={slot.kind === 'Head' ? 5 : 3.5}
                      fill={dot}
                    />
                  ))}
                {slots.map((slot) =>
                  SOUNDS.map((sound, r) => {
                    // The body only ever has a slide whistle.
                    if (slot.kind === 'Body' && sound !== 'Whistle') return null;

                    const was = hasSound(slot.before?.hitSound, sound);
                    const now = hasSound(slot.after?.hitSound, sound);
                    const color = STATUS_COLOR[pair.status];

                    if (slot.kind === 'Body') {
                      if (!was && !now) return null;
                      const x2 = xOf(object.endTime ?? object.time);

                      return (
                        <rect
                          key={`${slot.key}-${sound}`}
                          x={xOf(slot.time)}
                          y={rowY(r) + ROW / 2 - 3}
                          width={Math.max(x2 - xOf(slot.time), 4)}
                          height={6}
                          rx={3}
                          fill={now ? (was ? PALETTE.dot : STATUS_COLOR.added) : 'none'}
                          fillOpacity={lone ? 0.6 : 1}
                          stroke={now ? 'none' : STATUS_COLOR.removed}
                          strokeWidth={1.5}
                          strokeDasharray={now ? undefined : '3 3'}
                        />
                      );
                    }

                    return (
                      <Cell
                        key={`${slot.key}-${sound}`}
                        x={xOf(slot.time)}
                        y={rowY(r)}
                        width={slot.kind === 'Head' ? cell : Math.max(5, cell - 6)}
                        was={was}
                        now={now}
                        lone={lone}
                        color={color}
                      />
                    );
                  })
                )}
                {showSampleset &&
                  slots
                    .filter((slot) => slot.kind !== 'Body' && slot.after)
                    .map((slot) => {
                      const changed = samplesetChanged(slot);
                      const width = slot.kind === 'Head' ? cell : Math.max(5, cell - 6);
                      const color = SAMPLESET_COLOR[slot.after!.sampleset] ?? PALETTE.dot;

                      return (
                        <g key={`${slot.key}-sampleset`}>
                          {changed && (
                            <rect
                              x={xOf(slot.time) - width / 2 - 1}
                              y={samplesetY + 1}
                              width={width + 2}
                              height={ROW - 2}
                              rx={4}
                              fill="none"
                              stroke={STATUS_COLOR.changed}
                              strokeWidth={2}
                            />
                          )}
                          <rect
                            x={xOf(slot.time) - width / 2}
                            y={samplesetY + 3}
                            width={width}
                            height={ROW - 6}
                            rx={3}
                            fill={color}
                            fillOpacity={changed ? 1 : 0.45}
                          />
                          {width >= 12 && (
                            <text
                              x={xOf(slot.time)}
                              y={samplesetY + ROW / 2 + 3.5}
                              textAnchor="middle"
                              fontSize={9.5}
                              fontWeight={800}
                              fill={PALETTE.ink}
                            >
                              {samplesetLetter(slot.after!.sampleset)}
                            </text>
                          )}
                        </g>
                      );
                    })}
                {showVolume && pair.after?.volume != null && (
                  <g>
                    {pair.before?.volume != null && pair.before.volume !== pair.after.volume && (
                      <rect
                        x={x - cell / 2 + 1}
                        y={volumeY + VOLUME_HEIGHT - 3 - bar(pair.before.volume)}
                        width={cell - 2}
                        height={bar(pair.before.volume)}
                        rx={2}
                        fill="none"
                        stroke={PALETTE.muted}
                        strokeDasharray="3 3"
                      />
                    )}
                    <rect
                      x={x - cell / 2 + 3}
                      y={volumeY + VOLUME_HEIGHT - 3 - bar(pair.after.volume)}
                      width={Math.max(2, cell - 6)}
                      height={bar(pair.after.volume)}
                      rx={2}
                      fill={
                        pair.before && pair.before.volume !== pair.after.volume
                          ? STATUS_COLOR.changed
                          : PALETTE.dot
                      }
                    />
                  </g>
                )}
              </g>
            );

            return notes.length === 0 ? (
              <g key={i}>{column}</g>
            ) : (
              <Tooltip
                key={i}
                withinPortal
                multiline
                position="top"
                openDelay={80}
                label={
                  <Stack gap={2}>
                    <Text size="xs" fw={700}>
                      {object.type} at {formatClock(object.time)}
                    </Text>
                    {notes.map((note) => (
                      <Text key={note} size="xs">
                        {note}
                      </Text>
                    ))}
                  </Stack>
                }
              >
                {column}
              </Tooltip>
            );
          })}
        </g>
      </svg>
      <Group gap="md" wrap="wrap">
        <Text size="xs" c="dimmed">
          Green added, red outline removed, grey unchanged, yellow changed (N normal, S soft, D
          drum)
        </Text>
        <SnapLegend divisors={divisors} />
      </Group>
    </Stack>
  );
}
