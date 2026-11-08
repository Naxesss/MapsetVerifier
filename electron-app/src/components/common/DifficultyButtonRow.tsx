import { FloatingIndicator, Group, Tooltip, UnstyledButton } from '@mantine/core';
import {
  type CSSProperties,
  type ReactNode,
  type Ref,
  useContext,
  useEffect,
  useState,
} from 'react';
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
import type { ManiaPlaystyle, Mode } from '../../Types';

export type RowDifficulty = {
  id: string;
  label: string;
  mode: Mode;
  starRating?: number | null;
  /** Mania: the key mode or playstyle, which splits the mode's difficulties. */
  playstyle?: ManiaPlaystyle | null;
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

type Spreadable = {
  mode: string;
  starRating?: number | null;
  playstyle?: ManiaPlaystyle | null;
};

/** Fewer keys first, then by name (7K before 7K+1, both before 8K), then easiest to hardest. */
const bySpread = (a: Spreadable, b: Spreadable) =>
  (a.playstyle?.keys ?? 0) - (b.playstyle?.keys ?? 0) ||
  (a.playstyle?.label ?? '').localeCompare(b.playstyle?.label ?? '') ||
  (a.starRating ?? 0) - (b.starRating ?? 0);

/**
 * Difficulties in spread order: each mode in the usual mode order, easiest to hardest. Mania goes
 * key mode by key mode (or playstyle by playstyle).
 */
export function spreadGroups<T extends Spreadable>(difficulties: readonly T[]) {
  return groupByMode(difficulties).map((group) => ({
    ...group,
    difficulties: [...group.difficulties].sort(bySpread),
  }));
}

/** A mode's difficulties in spread order, split where the key mode or playstyle changes. */
function playstyleClusters<T extends Spreadable>(difficulties: readonly T[]) {
  const clusters: { label?: string; difficulties: T[] }[] = [];
  for (const d of difficulties) {
    const label = d.playstyle?.label;
    const last = clusters[clusters.length - 1];
    if (last && last.label === label) last.difficulties.push(d);
    else clusters.push({ label, difficulties: [d] });
  }
  return clusters;
}

/**
 * One mode's difficulties as buttons as wide as their names, wrapping onto more lines when they
 * don't fit, as in 2.1. Only a very long name ends in "…"; the tooltip has it in full. A
 * "mapper's difficulty" name puts the mapper in small text above the difficulty, inside the same
 * height, unless one guest mapper did the whole mode shown: then the MapsetFrame names them next to
 * the host instead. The status shows as an icon in front of the star rating pill, and the picked
 * difficulty is outlined in its star rating colour. Mania difficulties of more than one key mode
 * or playstyle are split into a cluster each, labelled ("4K", "7K", "DP") in front; their names,
 * which then have to denote it, show without that marker (in full on hover). A name with the
 * wrong one keeps it, so the mistake shows. A guest mapper never shows with a marker: it goes
 * with the difficulty instead (see shownName). A set with more than one mode gets mode
 * buttons at the end, each with a status badge summing up that mode's difficulties, so every
 * mode's state shows while only one mode's difficulties do. A floating indicator slides between
 * the mode buttons as the pick.
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
  const [modesRoot, setModesRoot] = useState<HTMLElement | null>(null);
  const [activeModeButton, setActiveModeButton] = useState<HTMLElement | null>(null);

  const lanes = spreadGroups(difficulties);
  const shown = lanes.find((lane) => lane.mode === shownMode) ?? lanes[0];
  const clusters = playstyleClusters(shown?.difficulties ?? []);
  const clustered = clusters.length > 1;
  const guests = shown?.difficulties.map((d) => shownName(d, clustered).guest) ?? [];
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
        {clusters.map((cluster) =>
          cluster.difficulties.map((d, index) => {
            const button = (
              <DifficultyButton
                key={d.id}
                difficulty={d}
                name={shownName(d, clustered)}
                hideMapper={!!modeMapper}
                onPick={onPick}
              />
            );
            // The label sits with the cluster's first difficulty, so it never ends a line alone.
            return clustered && index === 0 ? (
              <span key={`cluster-${cluster.label}`} className="mv-difficulty-cluster">
                <span className="mv-difficulty-cluster__label">{cluster.label}</span>
                {button}
              </span>
            ) : (
              button
            );
          })
        )}
      </div>
      {lanes.length > 1 && (
        <div
          ref={setModesRoot}
          className="mv-difficulty-modes"
          role="group"
          aria-label="Game modes"
        >
          <FloatingIndicator
            className="mv-difficulty-modes__indicator"
            parent={modesRoot}
            target={activeModeButton}
            data-disabled={!!modesDisabledReason || undefined}
          />
          {lanes.map((lane) => (
            <ModeButton
              key={lane.mode}
              mode={lane.mode}
              status={modeStatus?.(lane.mode, lane.difficulties)}
              active={lane.mode === shown?.mode}
              buttonRef={lane.mode === shown?.mode ? setActiveModeButton : undefined}
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
  buttonRef,
  disabledReason,
  onClick,
}: {
  mode: Mode;
  status?: ReactNode;
  active: boolean;
  buttonRef?: Ref<HTMLButtonElement>;
  disabledReason?: string;
  onClick: () => void;
}) {
  const label = formatGameModeLabel(mode);

  return (
    <Tooltip label={disabledReason ?? label}>
      <UnstyledButton
        ref={buttonRef}
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
  name,
  hideMapper,
  onPick,
}: {
  difficulty: RowDifficulty;
  name: ShownName;
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
        <ButtonContent difficulty={d} name={name} hideMapper={hideMapper} />
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

type ShownName = { guest?: ReturnType<typeof guestOf>; name: string };

/**
 * What a button shows: a guest difficulty's mapper on its own, without a key mode marker or its
 * divider ("[7K] - Mapper's Insane" is Mapper's), and the difficulty. The marker stays with the
 * difficulty ("[7K] Insane") unless the cluster's label already says it.
 */
function shownName(d: RowDifficulty, clustered: boolean): ShownName {
  const playstyle = d.playstyle;
  const marker = playstyle?.marker && !(clustered && playstyle.denoted) ? playstyle.marker : '';
  const guest = guestOf(playstyle?.shortName ?? d.label);
  if (guest) return { guest, name: marker ? `${marker} ${guest.name}` : guest.name };
  return { name: marker ? d.label : (playstyle?.shortName ?? d.label) };
}

/** A button's status icon, in front of its name. */
function Status({ icon }: { icon?: ReactNode }) {
  return icon ? <span className="mv-difficulty-button__status">{icon}</span> : null;
}

/** The status, the star rating pill and the name (the mapper of a guest difficulty above it). */
function ButtonContent({
  difficulty: d,
  name,
  hideMapper,
}: {
  difficulty: RowDifficulty;
  name: ShownName;
  hideMapper: boolean;
}) {
  const { guest } = name;

  return (
    <>
      <Status icon={d.status} />
      {d.starRating != null && <DifficultyColorPill color={getDifficultyColor(d.starRating)} />}
      <span className="mv-difficulty-button__name">
        {guest && !hideMapper && (
          <span className="mv-difficulty-button__mapper">{guest.mapper}</span>
        )}
        <span>{name.name}</span>
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
