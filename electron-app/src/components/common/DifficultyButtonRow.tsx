import { Group, Tooltip, UnstyledButton } from '@mantine/core';
import { type CSSProperties, type ReactNode, useContext, useEffect } from 'react';
import {
  getDifficultyBorderColor,
  getDifficultyColor,
  getDifficultyMutedColor,
  getDifficultyTextColor,
} from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
import { ModeMapperContext } from './mapsetFrameContext';
import { formatGameModeLabel, groupByMode } from '../../utils/gameMode';
import GameModeIcon from '../icons/GameModeIcon';
import type { Mode } from '../../Types';

export type RowDifficulty = {
  id: string;
  label: string;
  mode: Mode;
  starRating?: number | null;
  /** An icon in front: the level on Checks, changed or not on Snapshots. */
  status?: ReactNode;
  active: boolean;
  /** Greyed out and not clickable; says why on hover. */
  disabledReason?: string;
};

export type RowLead = {
  label: string;
  status?: ReactNode;
  active: boolean;
  disabledReason?: string;
  onClick: () => void;
};

interface DifficultyButtonRowProps {
  /** The button in front of the difficulties: General on Checks and Snapshots, All on Overview. */
  lead: RowLead;
  difficulties: RowDifficulty[];
  onPick: (difficulty: RowDifficulty) => void;
  /** The mode whose difficulties are shown; the first mode when unset or not in this set. */
  shownMode?: Mode;
  onShowMode: (mode: Mode) => void;
  /**
   * A mode button's status, as a badge on the mode icon: the worst level of its difficulties on
   * Checks, say.
   */
  modeStatus?: (mode: Mode, difficulties: RowDifficulty[]) => ReactNode;
  /** Greys out the mode buttons too, such as on the Overview's whole-set pages. */
  modesDisabledReason?: string;
}

const MODE_ICON_SIZE = 18;
const ROW_GAP = 6;

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
 * One mode's difficulties as buttons as wide as their names, wrapping onto more lines when they
 * don't fit, as in 2.1. Only a very long name ends in "…"; the tooltip has it in full. A
 * "mapper's difficulty" name puts the mapper in small text above the difficulty, inside the same
 * height, unless one guest mapper did the whole mode shown: then the MapsetFrame names them next to
 * the host instead. The status shows as an icon in front of the star rating pill, and the picked
 * difficulty is outlined in its star rating colour. A set with more than one mode gets mode
 * buttons at the end, each with a status badge summing up that mode's difficulties, so every
 * mode's state shows while only one mode's difficulties do.
 */
export default function DifficultyButtonRow({
  lead,
  difficulties,
  onPick,
  shownMode,
  onShowMode,
  modeStatus,
  modesDisabledReason,
}: DifficultyButtonRowProps) {
  const lanes = spreadGroups(difficulties);
  const shown = lanes.find((lane) => lane.mode === shownMode) ?? lanes[0];

  const guests = shown?.difficulties.map((d) => guestOf(d.label)) ?? [];
  const sharedGuest = guests[0] && guests.every((g) => g?.mapper === guests[0]?.mapper);
  const modeMapper = sharedGuest ? guests[0]?.who : undefined;

  const setModeMapper = useContext(ModeMapperContext);
  const mode = shown?.mode;
  useEffect(() => {
    if (!setModeMapper) return;
    setModeMapper(mode && modeMapper ? { mode, mapper: modeMapper } : undefined);
    return () => setModeMapper(undefined);
  }, [setModeMapper, mode, modeMapper]);

  return (
    <Group gap={ROW_GAP} wrap="nowrap" align="flex-start">
      <LeadButton lead={lead} />
      <div
        className="mv-difficulty-row"
        role="group"
        aria-label={shown ? formatGameModeLabel(shown.mode) : undefined}
      >
        {shown?.difficulties.map((d) => (
          <DifficultyButton key={d.id} difficulty={d} hideMapper={!!modeMapper} onPick={onPick} />
        ))}
      </div>
      {lanes.length > 1 && (
        <div className="mv-difficulty-modes" role="group" aria-label="Game modes">
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
        </div>
      )}
    </Group>
  );
}

function LeadButton({ lead }: { lead: RowLead }) {
  return (
    <Tooltip label={lead.disabledReason} disabled={!lead.disabledReason}>
      <UnstyledButton
        className="mv-picker-button"
        aria-pressed={lead.active}
        aria-disabled={!!lead.disabledReason || undefined}
        data-active={lead.active || undefined}
        data-disabled={!!lead.disabledReason || undefined}
        onClick={lead.disabledReason ? undefined : lead.onClick}
      >
        <Status icon={lead.status} />
        {lead.label}
      </UnstyledButton>
    </Tooltip>
  );
}

/** A mode's icon with its status as a badge on the corner, its name on hover. */
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
  const label = formatGameModeLabel(mode);

  return (
    <Tooltip label={disabledReason ?? label}>
      <UnstyledButton
        className="mv-difficulty-modes__button"
        aria-label={label}
        aria-pressed={active}
        aria-disabled={!!disabledReason || undefined}
        data-active={active || undefined}
        data-disabled={!!disabledReason || undefined}
        onClick={disabledReason ? undefined : onClick}
      >
        <span className="mv-difficulty-modes__icon">
          <GameModeIcon mode={mode} size={MODE_ICON_SIZE} color="currentColor" />
          {status && <span className="mv-difficulty-modes__badge">{status}</span>}
        </span>
      </UnstyledButton>
    </Tooltip>
  );
}

function DifficultyButton({
  difficulty: d,
  hideMapper,
  onPick,
}: {
  difficulty: RowDifficulty;
  hideMapper: boolean;
  onPick: (difficulty: RowDifficulty) => void;
}) {
  const rating = d.starRating != null ? ` · ${d.starRating.toFixed(2)}★` : '';
  const label = d.disabledReason ?? `${d.label}${rating}`;
  // The outline of the pick and the hover tint, in the star rating's colour.
  const color = d.starRating != null ? getDifficultyColor(d.starRating) : undefined;

  return (
    <Tooltip
      label={label}
      styles={{ tooltip: d.disabledReason ? undefined : starRatingTooltipStyle(d.starRating) }}
    >
      <UnstyledButton
        className="mv-picker-button mv-difficulty-button"
        aria-pressed={d.active}
        aria-label={d.label}
        aria-disabled={!!d.disabledReason || undefined}
        data-active={d.active || undefined}
        data-disabled={!!d.disabledReason || undefined}
        style={color ? ({ '--star': color } as CSSProperties) : undefined}
        onClick={d.disabledReason ? undefined : () => onPick(d)}
      >
        <ButtonContent difficulty={d} hideMapper={hideMapper} />
      </UnstyledButton>
    </Tooltip>
  );
}

/**
 * A guest difficulty's name split into the mapper and the difficulty: "Asahina's Hard", and
 * "Sebas' Another" for a mapper whose name ends in s. Straight or curly apostrophes.
 */
const GUEST_DIFFICULTY = /^(.+?(?:['’]s|s['’])) (.+)$/;

/** The mapper ("Sebas'"), who that is ("Sebas") and the difficulty, for a guest difficulty. */
function guestOf(label: string) {
  const match = label.match(GUEST_DIFFICULTY);
  if (!match) return undefined;
  return { mapper: match[1], who: match[1].replace(/['’]s?$/, ''), name: match[2] };
}

/** A button's status icon, in front of its name. */
function Status({ icon }: { icon?: ReactNode }) {
  return icon ? <span className="mv-difficulty-button__status">{icon}</span> : null;
}

/** The status, the star rating pill and the name (the mapper of a guest difficulty above it). */
function ButtonContent({
  difficulty: d,
  hideMapper,
}: {
  difficulty: RowDifficulty;
  hideMapper: boolean;
}) {
  const guest = guestOf(d.label);

  return (
    <>
      <Status icon={d.status} />
      {d.starRating != null && <DifficultyColorPill color={getDifficultyColor(d.starRating)} />}
      <span className="mv-difficulty-button__name">
        {guest && !hideMapper && (
          <span className="mv-difficulty-button__mapper">{guest.mapper}</span>
        )}
        <span>{guest ? guest.name : d.label}</span>
      </span>
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
