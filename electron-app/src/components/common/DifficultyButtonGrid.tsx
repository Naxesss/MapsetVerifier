import { Group, Stack, Tooltip, UnstyledButton } from '@mantine/core';
import { type ReactNode, useEffect, useLayoutEffect, useRef, useState } from 'react';
import {
  getDifficultyBorderColor,
  getDifficultyColor,
  getDifficultyMutedColor,
  getDifficultyTextColor,
} from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
import { formatGameModeLabel, groupByMode } from '../../utils/gameMode';
import GameModeIcon from '../icons/GameModeIcon';
import type { Mode } from '../../Types';

export type GridDifficulty = {
  id: string;
  label: string;
  mode: Mode;
  starRating?: number | null;
  /** An icon at the end: the level on Checks, changed or not on Snapshots. */
  status?: ReactNode;
  active: boolean;
  /** Greyed out and not clickable; says why on hover. */
  disabledReason?: string;
};

export type GridLead = {
  label: string;
  status?: ReactNode;
  active: boolean;
  disabledReason?: string;
  onClick: () => void;
};

interface DifficultyButtonGridProps {
  /** The button left of the grid: General on Checks and Snapshots, All on the Overview. */
  lead: GridLead;
  difficulties: GridDifficulty[];
  onPick: (difficulty: GridDifficulty) => void;
  /** The mode whose difficulties are shown; the first mode when unset or not in this set. */
  shownMode?: Mode;
  onShowMode: (mode: Mode) => void;
  /** A mode button's status icon: the worst level of its difficulties on Checks, say. */
  modeStatus?: (mode: Mode, difficulties: GridDifficulty[]) => ReactNode;
  /** Greys out the mode buttons too, such as on the Overview's whole-set pages. */
  modesDisabledReason?: string;
}

/** The gap between cells, as in `.mv-difficulty-grid` (where the cell size is set). */
const CELL_GAP = 3;
/** A name wider than this many cells ends in "…". */
const MAX_CELLS = 3;
const MODE_ICON_SIZE = 16;
const COLUMN_GAP = 6;

const byStarRating = (a: { starRating?: number | null }, b: { starRating?: number | null }) =>
  (a.starRating ?? 0) - (b.starRating ?? 0);

/** Difficulties in spread order: each mode in the usual mode order, easiest to hardest. */
export function spreadGroups<T extends { mode: string; starRating?: number | null }>(
  difficulties: readonly T[]
) {
  return groupByMode(difficulties).map((group) => ({
    ...group,
    difficulties: [...group.difficulties].sort(byStarRating),
  }));
}

/**
 * Cells per difficulty on one line after another, in spread order. A difficulty that doesn't fit
 * in what's left of a line starts the next one, and the difficulty before it takes the leftover
 * cells, so every full line ends flush.
 */
export function packSpans(needs: readonly number[], columns: number) {
  const spans = [...needs];
  let used = 0;
  needs.forEach((need, i) => {
    if (used > 0 && used + need > columns) {
      spans[i - 1] += columns - used;
      used = 0;
    }
    used += need;
    if (used === columns) used = 0;
  });
  return spans;
}

/**
 * One mode's difficulties as buttons with their full names, on a grid of equal cells so they line
 * up across lines. A name takes as many cells as it needs, up to three; past that it ends in "…".
 * A "mapper's difficulty" name puts the mapper in small text above the difficulty, inside the same
 * height. The status shows as an icon at the end, the only colour besides the star rating. A set with more than one mode gets a column of mode
 * buttons right of the grid, each status summing up that mode's difficulties, so every mode's
 * state shows while only one mode's difficulties do, and the grid keeps the full height.
 */
export default function DifficultyButtonGrid({
  lead,
  difficulties,
  onPick,
  shownMode,
  onShowMode,
  modeStatus,
  modesDisabledReason,
}: DifficultyButtonGridProps) {
  const lanes = spreadGroups(difficulties);
  const shown = lanes.find((lane) => lane.mode === shownMode) ?? lanes[0];

  // Read from the rendered grid after every render: the columns the browser made, and each
  // button's own width (its full name, even when cut off, plus padding and pill). Reading the real
  // grid keeps the spans in step with what's on screen.
  const gridRef = useRef<HTMLDivElement>(null);
  const [measured, setMeasured] = useState<Measured>({ columns: 1, cellWidth: 0, natural: {} });
  useLayoutEffect(() => {
    const root = gridRef.current;
    if (!root) return;
    const next = measure(root);
    setMeasured((previous) => (sameMeasure(previous, next) ? previous : next));
  });
  // Width changes and the font loading change those measurements; render again to re-read them.
  const [, remeasure] = useState(0);
  useEffect(() => {
    const root = gridRef.current;
    if (!root) return;
    const bump = () => remeasure((n) => n + 1);
    const observer = new ResizeObserver(bump);
    observer.observe(root);
    document.fonts.addEventListener('loadingdone', bump);
    return () => {
      observer.disconnect();
      document.fonts.removeEventListener('loadingdone', bump);
    };
  }, []);

  const { columns, cellWidth, natural } = measured;
  const cellsFor = (d: GridDifficulty) => {
    const width = natural[d.id];
    if (!width || cellWidth <= 0) return 1;
    // Half a pixel of slack, so rounding never pushes a name that fits into a second cell.
    const cells = Math.ceil((width - 0.5 + CELL_GAP) / (cellWidth + CELL_GAP));
    return Math.min(Math.max(cells, 1), MAX_CELLS, columns);
  };

  const spans = packSpans((shown?.difficulties ?? []).map(cellsFor), columns);

  return (
    <Group gap={COLUMN_GAP} wrap="nowrap" align="flex-start">
      <LeadButton lead={lead} />
      <div ref={gridRef} style={{ flex: 1, minWidth: 0 }}>
        {shown && (
          <div
            className="mv-difficulty-grid"
            role="group"
            aria-label={formatGameModeLabel(shown.mode)}
          >
            {shown.difficulties.map((d, i) => (
              <DifficultyButton key={d.id} difficulty={d} span={spans[i]} onPick={onPick} />
            ))}
          </div>
        )}
      </div>
      {lanes.length > 1 && (
        // The grid's row gap, so each mode button lines up with a row of difficulties.
        <Stack gap={CELL_GAP} role="group" aria-label="Game modes" style={{ flexShrink: 0 }}>
          {lanes.map((lane) => (
            <ModeButton
              key={lane.mode}
              mode={lane.mode}
              status={modeStatus?.(lane.mode, lane.difficulties)}
              active={lane.mode === shown?.mode}
              disabledReason={modesDisabledReason}
              onClick={() => onShowMode(lane.mode)}
            />
          ))}
        </Stack>
      )}
    </Group>
  );
}

type Measured = { columns: number; cellWidth: number; natural: Record<string, number> };

function measure(root: HTMLElement): Measured {
  const grid = root.querySelector<HTMLElement>('.mv-difficulty-grid');
  const tracks = grid
    ? getComputedStyle(grid)
        .gridTemplateColumns.split(' ')
        .filter((track) => track.endsWith('px'))
        .map(parseFloat)
    : [];
  const natural: Record<string, number> = {};
  root.querySelectorAll<HTMLElement>('[data-difficulty-id]').forEach((button) => {
    const style = getComputedStyle(button);
    const name = button.querySelector<HTMLElement>('.mv-difficulty-button__name');
    const pill = name?.previousElementSibling as HTMLElement | null;
    const status = name?.nextElementSibling as HTMLElement | null;
    const gap = parseFloat(style.columnGap || '0');
    const text = name ? Math.max(0, ...Array.from(name.children, (line) => line.scrollWidth)) : 0;
    natural[button.dataset.difficultyId ?? ''] =
      parseFloat(style.paddingLeft) +
      parseFloat(style.paddingRight) +
      parseFloat(style.borderLeftWidth) +
      parseFloat(style.borderRightWidth) +
      (pill ? pill.offsetWidth + gap : 0) +
      (status ? status.offsetWidth + gap : 0) +
      text;
  });
  return { columns: Math.max(1, tracks.length), cellWidth: tracks[0] ?? 0, natural };
}

function sameMeasure(a: Measured, b: Measured) {
  const ids = Object.keys(b.natural);
  return (
    a.columns === b.columns &&
    Math.abs(a.cellWidth - b.cellWidth) < 0.5 &&
    ids.length === Object.keys(a.natural).length &&
    ids.every((id) => Math.abs((a.natural[id] ?? -1) - b.natural[id]) < 0.5)
  );
}

function LeadButton({ lead }: { lead: GridLead }) {
  return (
    <Tooltip label={lead.disabledReason} disabled={!lead.disabledReason}>
      <UnstyledButton
        className="mv-difficulty-button mv-difficulty-button--lead"
        aria-pressed={lead.active}
        aria-disabled={!!lead.disabledReason || undefined}
        data-active={lead.active || undefined}
        data-disabled={!!lead.disabledReason || undefined}
        onClick={lead.disabledReason ? undefined : lead.onClick}
      >
        {lead.label}
        <Status icon={lead.status} />
      </UnstyledButton>
    </Tooltip>
  );
}

function ModeButton({
  mode,
  status,
  active,
  disabledReason,
  onClick,
}: {
  mode: Mode;
  status?: ReactNode;
  active: boolean;
  disabledReason?: string;
  onClick: () => void;
}) {
  return (
    <Tooltip label={disabledReason} disabled={!disabledReason}>
      <UnstyledButton
        className="mv-difficulty-button"
        aria-pressed={active}
        aria-disabled={!!disabledReason || undefined}
        data-active={active || undefined}
        data-disabled={!!disabledReason || undefined}
        onClick={disabledReason ? undefined : onClick}
      >
        <GameModeIcon mode={mode} size={MODE_ICON_SIZE} color="currentColor" />
        {formatGameModeLabel(mode)}
        <Status icon={status} />
      </UnstyledButton>
    </Tooltip>
  );
}

function DifficultyButton({
  difficulty: d,
  span,
  onPick,
}: {
  difficulty: GridDifficulty;
  span: number;
  onPick: (difficulty: GridDifficulty) => void;
}) {
  const rating = d.starRating != null ? ` · ${d.starRating.toFixed(2)}★` : '';
  const label = d.disabledReason ?? `${d.label}${rating}`;

  return (
    <Tooltip
      label={label}
      styles={{ tooltip: d.disabledReason ? undefined : starRatingTooltipStyle(d.starRating) }}
    >
      <UnstyledButton
        className="mv-difficulty-button"
        data-difficulty-id={d.id}
        aria-pressed={d.active}
        aria-label={d.label}
        aria-disabled={!!d.disabledReason || undefined}
        data-active={d.active || undefined}
        data-disabled={!!d.disabledReason || undefined}
        style={{ gridColumn: `span ${span}` }}
        onClick={d.disabledReason ? undefined : () => onPick(d)}
      >
        <ButtonContent difficulty={d} />
      </UnstyledButton>
    </Tooltip>
  );
}

/**
 * A guest difficulty's name split into the mapper and the difficulty: "Asahina's Hard", and
 * "Sebas' Another" for a mapper whose name ends in s. Straight or curly apostrophes.
 */
const GUEST_DIFFICULTY = /^(.+?(?:['’]s|s['’])) (.+)$/;

/** A button's status icon, pushed to its end. */
function Status({ icon }: { icon?: ReactNode }) {
  return icon ? <span className="mv-difficulty-button__status">{icon}</span> : null;
}

/** The star rating pill, the name (the mapper of a guest difficulty on its own line) and status. */
function ButtonContent({ difficulty: d }: { difficulty: GridDifficulty }) {
  const guest = d.label.match(GUEST_DIFFICULTY);

  return (
    <>
      {d.starRating != null && <DifficultyColorPill color={getDifficultyColor(d.starRating)} />}
      <span className="mv-difficulty-button__name">
        {guest && <span className="mv-difficulty-button__mapper">{guest[1]}</span>}
        <span>{guest ? guest[2] : d.label}</span>
      </span>
      <Status icon={d.status} />
    </>
  );
}

/** A tooltip coloured like the star rating badge. */
function starRatingTooltipStyle(starRating?: number | null) {
  // The star in a label grows the line box. A fixed line height keeps every tooltip the same height.
  const base = {
    padding: 'calc(var(--mantine-spacing-xs) / 2) var(--mantine-spacing-xs)',
    lineHeight: 1.45,
    fontWeight: 500,
  };
  if (starRating == null) return base;

  const color = getDifficultyColor(starRating);
  return {
    ...base,
    backgroundColor: getDifficultyMutedColor(color),
    color: starRating < 6.5 ? '#ffffff' : getDifficultyTextColor(starRating),
    border: `1px solid ${getDifficultyBorderColor(starRating)}`,
  };
}
