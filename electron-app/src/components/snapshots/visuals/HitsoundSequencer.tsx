import { Stack, Text, Tooltip } from '@mantine/core';
import { useId, useMemo } from 'react';
import { fitDivisors, gridLines, SNAP_COLOR } from './beatGrid';
import Legend from './Legend';
import { PALETTE, pairObjects, STATUS_COLOR, type PairedObject } from './pairing';
import { LabelledPlot } from './PlotLabels';
import { hunkDivisors } from './SnapGrid';
import { useObjectLink } from './useObjectLink';
import { describeChange, formatClock } from '../describe';
import type {
  Mode,
  ApiSnapshotChange,
  ApiSnapshotHunkVisual,
  ApiSnapshotSoundPart,
  ApiSnapshotVisualObject,
} from '../../../Types';

const WIDTH = 640;
const LEFT = 64;
const RIGHT = 8;
const ROW = 22;
const ROW_GAP = 6;
/** The snap ticks along the top, like the editor's timeline; the one starting a measure is taller. */
const TICK_BOTTOM = 14;
const TICK_MEASURE_TOP = 0;
const TICK_TOP = 6;
const TICK_WIDTH = 3;
const OBJECTS_Y = 22;
const TOP = 38;
const SOUNDS: readonly string[] = ['Whistle', 'Finish', 'Clap'];

/**
 * In taiko a finish makes a note big, which is gameplay and shown with the notes, not here. Whistle
 * and clap both make a kat, but which of the two it is is only a sound, so those stay.
 */
const TAIKO_SOUNDS: readonly string[] = ['Whistle', 'Clap'];
const MAX_CELL = 22;

/** Which sampleset a cell comes from, in the editor's spirit: normal plain, soft cool, drum warm. */
const SAMPLESET_COLOR: Record<string, string> = {
  Normal: '#A3ADBD',
  Soft: '#4FD1C5',
  Drum: '#FF922B',
};
const samplesetLetter = (sampleset: string) => sampleset.charAt(0) || '?';

/**
 * The rows below the sounds: where each sound comes from. They are always shown, so every
 * hitsound picture reads the same way, and a cell is outlined where its value changed.
 */
const SAMPLE_ROWS = [
  {
    label: 'Sampleset',
    value: (part: ApiSnapshotSoundPart) => samplesetLetter(part.sampleset),
    color: (part: ApiSnapshotSoundPart) => SAMPLESET_COLOR[part.sampleset] ?? PALETTE.dot,
  },
  {
    label: 'Additions',
    value: (part: ApiSnapshotSoundPart) => samplesetLetter(part.addition),
    color: (part: ApiSnapshotSoundPart) => SAMPLESET_COLOR[part.addition] ?? PALETTE.dot,
  },
  {
    label: 'Index',
    // Left empty where the index is not known, rather than printing nothing as text.
    value: (part: ApiSnapshotSoundPart) => (part.index == null ? '' : String(part.index)),
    color: () => PALETTE.muted,
  },
] as const;

/** Whether a part tells where its sound comes from (spinners and stand-ins do not). */
const hasSampleInfo = (part: ApiSnapshotSoundPart | undefined) => !!part?.sampleset;

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
        {
          time: object.time,
          kind: 'Head',
          hitSound: object.hitSound,
          sampleset: '',
          addition: '',
          index: 0,
        },
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
function describe(pair: PairedObject, slots: Slot[], sounds: readonly string[]): string[] {
  const notes: string[] = [];

  if (pair.status === 'added') notes.push('Object added');
  if (pair.status === 'removed') notes.push('Object removed');

  if (pair.before && pair.after) {
    for (const slot of slots) {
      for (const sound of slot.kind === 'Body' ? ['Whistle'] : sounds) {
        const was = hasSound(slot.before?.hitSound, sound);
        const now = hasSound(slot.after?.hitSound, sound);
        const where = slot.label ? `${slot.label}: ` : '';
        if (!was && now) notes.push(`${where}${sound} added`);
        if (was && !now) notes.push(`${where}${sound} removed`);
      }
    }

    for (const slot of slots) {
      if (slot.kind === 'Body' || !hasSampleInfo(slot.before) || !hasSampleInfo(slot.after))
        continue;
      const was = slot.before!;
      const now = slot.after!;
      const where = slot.label ? `${slot.label}: ` : '';
      if (was.sampleset !== now.sampleset)
        notes.push(`${where}Sampleset ${was.sampleset} to ${now.sampleset}`);
      if (was.addition !== now.addition)
        notes.push(`${where}Additions ${was.addition} to ${now.addition}`);
      if (was.index != null && now.index != null && was.index !== now.index)
        notes.push(`${where}Index ${was.index} to ${now.index}`);
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
 * One value in the rows below the sounds (a sampleset, an index, a volume). What stayed the same
 * is a grey cell like an unchanged sound, so only what changed stands out: a cell filled in its
 * colour with an outline. The value is written in the cell where it fits.
 */
function ValueCell({
  x,
  y,
  width,
  text,
  color,
  changed,
}: {
  x: number;
  y: number;
  width: number;
  text: string;
  color: string;
  changed: boolean;
}) {
  if (!text) return null;

  const fits = width >= text.length * 5.5 + 3;
  const label = (fill: string) => (
    <text
      x={x}
      y={y + ROW / 2 + 3.5}
      textAnchor="middle"
      fontSize={9.5}
      fontWeight={800}
      fill={fill}
    >
      {text}
    </text>
  );

  return (
    <g>
      {changed && (
        <rect
          x={x - width / 2 - 1}
          y={y + 1}
          width={width + 2}
          height={ROW - 2}
          rx={4}
          fill="none"
          stroke={STATUS_COLOR.changed}
          strokeWidth={2}
        />
      )}
      <rect
        x={x - width / 2}
        y={y + 3}
        width={width}
        height={ROW - 6}
        rx={3}
        fill={changed ? color : PALETTE.dot}
      />
      {fits && label(changed ? PALETTE.ink : STATUS_COLOR.same)}
    </g>
  );
}

/**
 * What a hitsounding pass did, as a step sequencer: the objects along song time, a row each for
 * whistle, finish and clap with a cell where the object plays it, then where the sounds come from
 * and the volume in percent. Cells
 * that were added are green, ones that were taken away are outlined in red, and the rest are grey,
 * so a pass reads as a pattern instead of a list.
 */
export default function HitsoundSequencer({
  visual,
  start,
  end,
  shift = 0,
  lineChanges = [],
  mode,
}: {
  visual: ApiSnapshotHunkVisual;
  start: number;
  end: number;
  shift?: number;
  mode?: Mode;
  /**
   * Hitsound changes on timing lines (a volume or sampleset an object inherits). They are marked
   * where the line is, so a row of changed volumes shows what it comes from.
   */
  lineChanges?: ApiSnapshotChange[];
}) {
  const link = useObjectLink();
  const pairs = useMemo(
    () => pairObjects(visual.before, visual.after, shift, false, [start, end], mode === 'Taiko'),
    [visual, shift, start, end, mode]
  );
  const lines = useMemo(() => {
    const byTime = new Map<number, ApiSnapshotChange[]>();
    for (const change of lineChanges) {
      if (change.time < start || change.time > end) continue;
      byTime.set(change.time, [...(byTime.get(change.time) ?? []), change]);
    }
    return [...byTime.entries()].map(([time, changes]) => ({ time, changes }));
  }, [lineChanges, start, end]);
  const clipId = useId();
  const sounds = mode === 'Taiko' ? TAIKO_SOUNDS : SOUNDS;

  // The cells are centred on their time, so the axis stops half a cell short of both edges and the
  // first and last columns are drawn whole rather than cut through the middle.
  const span = Math.max(end - start, 1);
  const inset = MAX_CELL / 2 + 2;
  const xOf = (time: number) =>
    LEFT + inset + ((time - start) / span) * (WIDTH - LEFT - RIGHT - 2 * inset);

  const used = useMemo(() => hunkDivisors(visual), [visual]);
  const divisors = fitDivisors(visual.afterTiming, used, start, end, xOf);

  // Every snap position is a slot, with an empty cell in each row where nothing plays, so the rows
  // read as a step sequencer.
  const grid = gridLines(visual.afterTiming, divisors, start, end);

  /** Whether a time is the first beat of a measure, by the red line it falls under. */
  const startsMeasure = (time: number) => {
    const mark = [...visual.afterTiming].reverse().find((m) => m.offset <= time + 1);
    if (!mark || mark.beatLength <= 0) return false;

    const beats = (time - mark.offset) / mark.beatLength;
    const beat = Math.round(beats);
    return Math.abs(beats - beat) < 0.01 && beat % Math.max(mark.meter, 1) === 0;
  };

  // One cell size for everything, as wide as the closest two slots, or two objects, allow. Objects
  // at the same moment (a chord) share a column, so they do not count as close.
  const tightest = (xs: number[]) => {
    const sorted = [...xs].sort((a, b) => a - b);
    let gap = Infinity;
    for (let i = 1; i < sorted.length; i++)
      if (sorted[i] - sorted[i - 1] > 0.5) gap = Math.min(gap, sorted[i] - sorted[i - 1]);
    return gap;
  };
  const closest = Math.min(
    tightest(grid.map((line) => xOf(line.time))),
    tightest(pairs.map((p) => xOf((p.after ?? p.before)!.time)))
  );
  const cell = Math.max(5, Math.min(MAX_CELL, closest - 3));

  // The columns that get empty cells: every snap slot, and every place an object makes a sound,
  // so a column never has its sounds without the empty cells around them (an unsnapped object, or
  // one on a snap too fine to draw).
  const columns = [
    ...grid.map((line) => xOf(line.time)),
    ...pairs.flatMap((p) =>
      slotsOf(p)
        .filter((s) => s.kind !== 'Body')
        .map((s) => xOf(s.time))
    ),
  ]
    .sort((a, b) => a - b)
    .filter((x, i, xs) => i === 0 || x - xs[i - 1] > 0.5);

  // The volume reads as the percentage the editor shows, in a row like the samples.
  const showVolume = pairs.some((p) => (p.after ?? p.before)?.volume != null);
  // Only spinners have nothing to say about where their sound comes from.
  const sampleRows = pairs.some((p) =>
    slotsOf(p).some((s) => hasSampleInfo(s.after) || hasSampleInfo(s.before))
  )
    ? SAMPLE_ROWS
    : [];
  const rowY = (i: number) => TOP + i * (ROW + ROW_GAP);
  const sampleY = (i: number) => rowY(sounds.length + i);
  const volumeY = rowY(sounds.length + sampleRows.length);
  const height = showVolume ? volumeY + ROW + 6 : volumeY + 6;
  const rowTops = [
    ...sounds.map((_, i) => rowY(i)),
    ...sampleRows.map((_, i) => sampleY(i)),
    ...(showVolume ? [volumeY] : []),
  ];

  const rows = [
    { label: 'Objects', y: OBJECTS_Y },
    ...sounds.map((sound, i) => ({ label: sound, y: rowY(i) + ROW / 2 })),
    ...sampleRows.map((row, i) => ({ label: row.label, y: sampleY(i) + ROW / 2 })),
    ...(showVolume ? [{ label: 'Volume %', y: volumeY + ROW / 2 }] : []),
  ];

  return (
    <Stack gap="xs">
      <LabelledPlot rows={rows} view={[0, height]}>
        <svg
          viewBox={`${LEFT} 0 ${WIDTH - LEFT} ${height}`}
          role="img"
          aria-label="Hitsounds before and after"
          style={{ display: 'block', width: '100%', height: 'auto' }}
        >
          <clipPath id={clipId}>
            <rect x={LEFT} y={0} width={WIDTH - LEFT - RIGHT} height={height} />
          </clipPath>

          <g clipPath={`url(#${clipId})`}>
            {/* The snaps along the top, in the editor's colours. */}
            {grid.map(({ time, divisor }) => {
              const top = startsMeasure(time) ? TICK_MEASURE_TOP : TICK_TOP;

              return (
                <rect
                  key={`tick-${time}`}
                  x={xOf(time) - TICK_WIDTH / 2}
                  y={top}
                  width={TICK_WIDTH}
                  height={TICK_BOTTOM - top}
                  rx={1}
                  fill={SNAP_COLOR[divisor] ?? PALETTE.muted}
                />
              );
            })}

            {/* An empty cell in every row at every column; with no columns, a plain band per row. */}
            {rowTops.map((y) =>
              columns.length > 0 ? (
                columns.map((x) => (
                  <rect
                    key={`slot-${y}-${x}`}
                    x={x - cell / 2}
                    y={y + 2}
                    width={cell}
                    height={ROW - 4}
                    rx={3}
                    fill={PALETTE.grid}
                  />
                ))
              ) : (
                <rect
                  key={`band-${y}`}
                  x={LEFT}
                  y={y}
                  width={WIDTH - LEFT - RIGHT}
                  height={ROW}
                  rx={4}
                  fill={PALETTE.surface}
                />
              )
            )}

            {/* Changed timing lines, behind the cells so their values stay readable. */}
            {lines.map(({ time, changes }) => (
              <Tooltip
                key={time}
                withinPortal
                multiline
                position="top"
                openDelay={80}
                label={
                  <Stack gap={2}>
                    <Text size="xs" fw={700}>
                      Timing line at {formatClock(time)}
                    </Text>
                    {changes.map((change) => (
                      <Text key={change.field} size="xs">
                        {describeChange(change)}
                      </Text>
                    ))}
                  </Stack>
                }
              >
                <g>
                  <line
                    x1={xOf(time)}
                    x2={xOf(time)}
                    y1={4}
                    y2={height - 4}
                    stroke={STATUS_COLOR.changed}
                    strokeWidth={1.5}
                    strokeDasharray="4 3"
                  />
                  <path d={`M${xOf(time) - 5} 2 h10 l-5 6 z`} fill={STATUS_COLOR.changed} />
                  {/* A wider strip to hover, the line itself is thin. */}
                  <rect x={xOf(time) - 5} y={0} width={10} height={height} fill="transparent" />
                </g>
              </Tooltip>
            ))}

            {pairs.map((pair, i) => {
              const object = (pair.after ?? pair.before)!;
              const x = xOf(object.time);
              const lone = !pair.before || !pair.after;
              const slots = slotsOf(pair);
              const notes = describe(pair, slots, sounds);
              // Where the object is did not change, but what it plays did (a volume it inherits
              // from a changed line, say): in this picture that is a changed object.
              const status = pair.status === 'same' && notes.length > 0 ? 'changed' : pair.status;
              const dot = status === 'same' ? PALETTE.dot : STATUS_COLOR[status];

              const column = (
                <g {...link(pair)}>
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
                    sounds.map((sound, r) => {
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
                          width={cell}
                          was={was}
                          now={now}
                          lone={lone}
                          color={color}
                        />
                      );
                    })
                  )}
                  {slots
                    .filter(
                      (slot) =>
                        slot.kind !== 'Body' &&
                        (hasSampleInfo(slot.after) || hasSampleInfo(slot.before))
                    )
                    .map((slot) => {
                      // A removed object shows what it played, faded like the rest of it.
                      const part = hasSampleInfo(slot.after) ? slot.after! : slot.before!;
                      const removed = !hasSampleInfo(slot.after);
                      const width = cell;

                      return sampleRows.map((row, i) => (
                        <g key={`${slot.key}-${row.label}`} opacity={removed ? 0.45 : 1}>
                          <ValueCell
                            x={xOf(slot.time)}
                            y={sampleY(i)}
                            width={width}
                            text={row.value(part)}
                            color={row.color(part)}
                            changed={
                              hasSampleInfo(slot.before) &&
                              hasSampleInfo(slot.after) &&
                              row.value(slot.before!) !== row.value(slot.after!)
                            }
                          />
                        </g>
                      ));
                    })}
                  {showVolume &&
                    (() => {
                      const volume = (pair.after ?? pair.before)?.volume;
                      if (volume == null) return null;

                      return (
                        <g opacity={pair.after ? 1 : 0.45}>
                          <ValueCell
                            x={x}
                            y={volumeY}
                            width={cell}
                            text={String(volume)}
                            color={PALETTE.muted}
                            changed={
                              pair.before?.volume != null &&
                              pair.after?.volume != null &&
                              pair.before.volume !== pair.after.volume
                            }
                          />
                        </g>
                      );
                    })()}
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
                      <Text size="xs" c="dimmed">
                        Click to open in osu!, ctrl+click to copy
                      </Text>
                    </Stack>
                  }
                >
                  {column}
                </Tooltip>
              );
            })}
          </g>
        </svg>
      </LabelledPlot>
      <Legend
        items={[
          { mark: 'block', color: PALETTE.dot, label: 'Same' },
          { mark: 'block', color: STATUS_COLOR.changed, label: 'Changed' },
          { mark: 'block', color: STATUS_COLOR.added, label: 'Added' },
          { mark: 'blockCross', color: STATUS_COLOR.removed, label: 'Removed' },
          { mark: 'letter', color: SAMPLESET_COLOR.Normal, letter: 'N', label: 'Normal' },
          { mark: 'letter', color: SAMPLESET_COLOR.Soft, letter: 'S', label: 'Soft' },
          { mark: 'letter', color: SAMPLESET_COLOR.Drum, letter: 'D', label: 'Drum' },
          ...(lines.length > 0
            ? [{ mark: 'line' as const, color: STATUS_COLOR.changed, label: 'Timing line changed' }]
            : []),
        ]}
        divisors={divisors}
      />
    </Stack>
  );
}
