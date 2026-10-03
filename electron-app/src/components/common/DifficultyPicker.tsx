import {
  ActionIcon,
  Box,
  Button,
  Group,
  Menu,
  Text,
  Tooltip,
  UnstyledButton,
  useMantineTheme,
} from '@mantine/core';
import { useHotkeys } from '@mantine/hooks';
import { IconChevronDown, IconChevronLeft, IconChevronRight } from '@tabler/icons-react';
import { Fragment, useState, type ReactNode } from 'react';
import {
  getDifficultyBorderColor,
  getDifficultyColor,
  getDifficultyMutedColor,
  getDifficultyTextColor,
} from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
import ShortcutLabel from './ShortcutLabel';
import StarRatingBadge from './StarRatingBadge';
import { formatGameModeLabel, MODE_ORDER } from '../../utils/gameMode';
import GameModeIcon from '../icons/GameModeIcon';
import type { Mode } from '../../Types';

export const GENERAL_TAB_ID = 'General';

export type PickerDifficulty = {
  id: string;
  label: string;
  mode: Mode;
  starRating?: number | null;
  /** Status in front of the name: the level on Checks, changed or not on Snapshots. */
  icon: ReactNode;
  /** The same status as a colour, for its segment in the bar. */
  statusColor: string;
  disabled?: boolean;
  disabledReason?: string;
};

interface DifficultyPickerProps {
  difficulties: PickerDifficulty[];
  general: { icon: ReactNode; statusColor: string };
  /** Summary status of a mode's difficulties, shown in the mode menu. */
  modeStatus?: (mode: Mode, difficulties: PickerDifficulty[]) => ReactNode;
  selectedId?: string;
  onSelect: (id: string) => void;
}

/** Difficulties the menu shows before it scrolls; its height fits that many rows. */
const DIFF_MENU_MAX_VISIBLE = 12;
const DIFF_MENU_MAX_HEIGHT = 360;

const byStarRating = (a: PickerDifficulty, b: PickerDifficulty) =>
  (a.starRating ?? 0) - (b.starRating ?? 0);

/**
 * Picks the difficulty on Checks and Snapshots: General, then mode and difficulty as two menus
 * read left to right (osu! / normal), with previous and next. Underneath, one segment per
 * difficulty in spread order, coloured by its status, so every difficulty's state stays in view
 * and any of them is one click away. `[` and `]` step through the spread.
 */
function DifficultyPicker({
  difficulties,
  general,
  modeStatus,
  selectedId,
  onSelect,
}: DifficultyPickerProps) {
  const theme = useMantineTheme();
  const [browsedMode, setBrowsedMode] = useState<Mode>();
  const [diffMenuOpened, setDiffMenuOpened] = useState(false);

  const modes = MODE_ORDER.map((mode) => ({
    mode,
    difficulties: difficulties.filter((d) => d.mode === mode).sort(byStarRating),
  })).filter((group) => group.difficulties.length > 0);
  // Spread order: General, then each mode from easiest to hardest.
  const ordered = modes.flatMap((group) => group.difficulties);

  const isGeneral = !selectedId || selectedId === GENERAL_TAB_ID;
  const selected = ordered.find((d) => d.id === selectedId);
  // The mode shown in the breadcrumb: the selected difficulty's, or the last one browsed to.
  const activeMode =
    selected?.mode ??
    (browsedMode && modes.some((g) => g.mode === browsedMode) ? browsedMode : modes[0]?.mode);
  const activeGroup = modes.find((g) => g.mode === activeMode);
  // Every star badge in the menu takes the width of the widest one, so they line up.
  const menuStarRatings = activeGroup?.difficulties.flatMap((d) =>
    d.starRating != null && !d.disabled ? [d.starRating] : []
  );

  const steps = [GENERAL_TAB_ID, ...ordered.filter((d) => !d.disabled).map((d) => d.id)];
  const stepIndex = steps.indexOf(isGeneral ? GENERAL_TAB_ID : (selectedId ?? ''));
  const step = (dir: 1 | -1) => {
    const next = steps[stepIndex + dir];
    if (next) onSelect(next);
  };

  useHotkeys([
    ['[', () => step(-1)],
    [']', () => step(1)],
  ]);

  // Choosing a mode moves off a difficulty of another mode (its easiest one takes over) and opens
  // the difficulty menu, since picking a difficulty is what comes next.
  const selectMode = (mode: Mode) => {
    setBrowsedMode(mode);
    if (selected && selected.mode !== mode) {
      const easiest = modes.find((g) => g.mode === mode)?.difficulties.find((d) => !d.disabled);
      onSelect(easiest?.id ?? GENERAL_TAB_ID);
    }
    setDiffMenuOpened(true);
  };

  return (
    <Box>
      <Group gap="xs" wrap="nowrap">
        <Button
          variant="default"
          size="sm"
          leftSection={general.icon}
          aria-pressed={isGeneral}
          style={{ flexShrink: 0, borderColor: isGeneral ? theme.colors.dark[2] : undefined }}
          onClick={() => onSelect(GENERAL_TAB_ID)}
        >
          General
        </Button>
        {modes.length > 1 && (
          <>
            <PickerSeparator />
            <ModeMenu
              groups={modes.map((group) => ({
                mode: group.mode,
                count: group.difficulties.length,
                status: modeStatus?.(group.mode, group.difficulties),
              }))}
              active={activeMode}
              onSelect={selectMode}
            />
          </>
        )}
        <PickerSeparator />
        <Menu
          position="bottom-start"
          withinPortal
          opened={diffMenuOpened}
          onChange={setDiffMenuOpened}
        >
          <Menu.Target>
            <Button
              variant="default"
              size="sm"
              leftSection={
                selected && (
                  <Group gap={6} wrap="nowrap">
                    {selected.icon}
                    <DifficultyColorPill color={getDifficultyColor(selected.starRating ?? 0)} />
                  </Group>
                )
              }
              rightSection={PICKER_CHEVRON}
              maw={340}
              // Shrinks before anything else in a narrow window, cutting the name off with "…".
              style={{ minWidth: 0 }}
              styles={{ inner: { minWidth: 0 }, label: { minWidth: 0, overflow: 'hidden' } }}
            >
              <Text span inherit truncate c={selected ? undefined : 'dimmed'} display="block">
                {selected ? selected.label : 'Pick a difficulty'}
              </Text>
            </Button>
          </Menu.Target>
          <Menu.Dropdown>
            {/* Only a list that can't fit scrolls. Always making it scrollable let a few pixels of
                icon overflow turn into a scrollbar even for one difficulty. The native scrollbar
                takes its own space, so it never covers the star ratings. */}
            <Box
              mah={DIFF_MENU_MAX_HEIGHT}
              style={
                (activeGroup?.difficulties.length ?? 0) > DIFF_MENU_MAX_VISIBLE
                  ? { overflowY: 'auto', overflowX: 'hidden' }
                  : undefined
              }
            >
              {activeGroup?.difficulties.map((d) => (
                <Menu.Item
                  key={d.id}
                  disabled={d.disabled}
                  leftSection={
                    <Group gap={6} wrap="nowrap">
                      {d.icon}
                      <DifficultyColorPill color={getDifficultyColor(d.starRating ?? 0)} />
                    </Group>
                  }
                  rightSection={
                    d.disabled ? (
                      <Text size="xs" c="dimmed">
                        {d.disabledReason}
                      </Text>
                    ) : (
                      d.starRating != null && (
                        <StarRatingBadge rating={d.starRating} size="xs" sizeTo={menuStarRatings} />
                      )
                    )
                  }
                  style={
                    d.id === selectedId ? { background: 'var(--mantine-color-dark-5)' } : undefined
                  }
                  onClick={() => onSelect(d.id)}
                >
                  {d.label}
                </Menu.Item>
              ))}
            </Box>
          </Menu.Dropdown>
        </Menu>
        <Group gap="xs" wrap="nowrap" ml="auto" style={{ flexShrink: 0 }}>
          <Tooltip label={<ShortcutLabel label="Previous difficulty" keys={['[']} />}>
            <ActionIcon
              variant="default"
              size="input-sm"
              aria-label="Previous difficulty"
              disabled={stepIndex <= 0}
              onClick={() => step(-1)}
            >
              <IconChevronLeft size={18} stroke={1.5} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={<ShortcutLabel label="Next difficulty" keys={[']']} />}>
            <ActionIcon
              variant="default"
              size="input-sm"
              aria-label="Next difficulty"
              disabled={stepIndex < 0 || stepIndex >= steps.length - 1}
              onClick={() => step(1)}
            >
              <IconChevronRight size={18} stroke={1.5} />
            </ActionIcon>
          </Tooltip>
        </Group>
      </Group>
      {/* One segment per difficulty in spread order, a wider gap after General and between modes. */}
      <StatusSegmentBar>
        <StatusSegment
          label="General"
          color={general.statusColor}
          active={isGeneral}
          onClick={() => onSelect(GENERAL_TAB_ID)}
        />
        {modes.map((group) => (
          <Fragment key={group.mode}>
            {group.difficulties.map((d, i) => (
              <StatusSegment
                key={d.id}
                groupStart={i === 0}
                label={
                  d.disabled
                    ? `${d.label}: ${d.disabledReason}`
                    : `${d.label}${d.starRating != null ? ` · ${d.starRating.toFixed(2)}★` : ''}`
                }
                starRating={d.disabled ? undefined : d.starRating}
                color={d.disabled ? theme.colors.dark[4] : d.statusColor}
                active={d.id === selectedId}
                disabled={d.disabled}
                onClick={() => onSelect(d.id)}
              />
            ))}
          </Fragment>
        ))}
      </StatusSegmentBar>
    </Box>
  );
}

export const PICKER_CHEVRON = <IconChevronDown size={14} stroke={1.5} />;

/** The "/" between a picker's buttons, which read left to right like a path. */
export function PickerSeparator() {
  return (
    <Text c="dimmed" fw={700} aria-hidden>
      /
    </Text>
  );
}

interface ModeMenuProps {
  /** The modes to choose from, in order; `status` shows in front of the mode's icon. */
  groups: { mode: Mode; count: number; status?: ReactNode }[];
  active: Mode | undefined;
  onSelect: (mode: Mode) => void;
}

/** The game mode menu of the difficulty pickers: the active mode, and each mode's difficulty count. */
export function ModeMenu({ groups, active, onSelect }: ModeMenuProps) {
  const activeGroup = groups.find((group) => group.mode === active);

  return (
    <Menu position="bottom-start" withinPortal>
      <Menu.Target>
        <Button
          variant="default"
          size="sm"
          leftSection={
            <Group gap={6} wrap="nowrap">
              {activeGroup?.status}
              {active && <GameModeIcon mode={active} size={18} />}
            </Group>
          }
          rightSection={PICKER_CHEVRON}
          style={{ flexShrink: 0 }}
        >
          {active && formatGameModeLabel(active)}
        </Button>
      </Menu.Target>
      <Menu.Dropdown>
        {groups.map((group) => (
          <Menu.Item
            key={group.mode}
            leftSection={
              <Group gap={6} wrap="nowrap">
                {group.status}
                <GameModeIcon mode={group.mode} size={18} />
              </Group>
            }
            rightSection={
              <Text size="xs" c="dimmed" style={{ fontVariantNumeric: 'tabular-nums' }}>
                {group.count}
              </Text>
            }
            onClick={() => onSelect(group.mode)}
          >
            {formatGameModeLabel(group.mode)}
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  );
}

const SEGMENT_GAP = 3;
const SEGMENT_GROUP_GAP = 10;
/** Height of each segment's click area; its bar is 6px, or 10px when selected or hovered. */
const SEGMENT_HIT_HEIGHT = 20;
/** Room between the tallest bar and the click area's edge, above and below. */
const SEGMENT_HIT_SLACK = (SEGMENT_HIT_HEIGHT - 10) / 2;

/**
 * The row of segments under a picker: the same small gap between every segment. Each segment's
 * click area is taller than its bar; the row's margins take that extra height back, so the bars
 * keep the header's usual row gap (sm) above and below.
 */
export function StatusSegmentBar({ children }: { children: ReactNode }) {
  return (
    <Group
      gap={SEGMENT_GAP}
      wrap="nowrap"
      align="center"
      h={SEGMENT_HIT_HEIGHT}
      mt={`calc(var(--mantine-spacing-sm) - ${SEGMENT_HIT_SLACK}px)`}
      mb={-SEGMENT_HIT_SLACK}
      aria-label="Difficulties at a glance"
    >
      {children}
    </Group>
  );
}

interface StatusSegmentProps {
  label: string;
  /** First segment of a mode, which gets the wider gap in front of it. */
  groupStart?: boolean;
  /** Colours the tooltip like the star-rating badge when the label includes a rating. */
  starRating?: number | null;
  color: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
}

/**
 * A bar in a taller click area (`.mv-status-segment` in global.scss). Hovering grows and brightens
 * the bar to the selected look, so it reads as clickable. The selected bar is taller rather than
 * outlined: an outline spilled into the gaps next to it.
 */
export function StatusSegment({
  label,
  groupStart,
  starRating,
  color,
  active,
  disabled,
  onClick,
}: StatusSegmentProps) {
  const difficultyColor = starRating != null ? getDifficultyColor(starRating) : undefined;
  const difficultyTextColor =
    starRating == null
      ? undefined
      : starRating < 6.5
        ? '#ffffff'
        : getDifficultyTextColor(starRating);
  const difficultyBorderColor =
    starRating != null ? getDifficultyBorderColor(starRating) : undefined;

  return (
    <Tooltip
      label={label}
      styles={{
        tooltip: {
          // The star in a difficulty label grows the line box. A fixed line height and the same
          // padding keep General the same height as those tooltips.
          padding: 'calc(var(--mantine-spacing-xs) / 2) var(--mantine-spacing-xs)',
          lineHeight: 1.45,
          fontWeight: 500,
          ...(difficultyColor
            ? {
                backgroundColor: getDifficultyMutedColor(difficultyColor),
                color: difficultyTextColor,
                border: `1px solid ${difficultyBorderColor}`,
              }
            : {}),
        },
      }}
    >
      <UnstyledButton
        className="mv-status-segment"
        aria-label={label}
        aria-current={active || undefined}
        aria-disabled={disabled || undefined}
        data-active={active || undefined}
        onClick={disabled ? undefined : onClick}
        style={{
          marginLeft: groupStart ? SEGMENT_GROUP_GAP - SEGMENT_GAP : undefined,
          '--segment-color': color,
        }}
      >
        <span className="mv-status-segment__bar" />
      </UnstyledButton>
    </Tooltip>
  );
}

export default DifficultyPicker;
