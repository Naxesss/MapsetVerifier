import {
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
import { IconChevronDown } from '@tabler/icons-react';
import {
  Fragment,
  useEffect,
  useId,
  useRef,
  useState,
  type ReactNode,
  type Ref,
  type RefObject,
} from 'react';
import {
  getDifficultyBorderColor,
  getDifficultyColor,
  getDifficultyMutedColor,
  getDifficultyTextColor,
} from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
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
  /** Summary status of a mode's difficulties, shown in its heading in the menu. */
  modeStatus?: (mode: Mode, difficulties: PickerDifficulty[]) => ReactNode;
  selectedId?: string;
  onSelect: (id: string) => void;
}

/** Rows (difficulties and headings) the menu shows before it scrolls; its height fits that many. */
const MENU_MAX_VISIBLE = 12;
const MENU_MAX_HEIGHT = 360;

const byStarRating = (a: PickerDifficulty, b: PickerDifficulty) =>
  (a.starRating ?? 0) - (b.starRating ?? 0);

/**
 * Picks the difficulty on Checks and Snapshots: General on the left, and on the right one menu with
 * every difficulty, under a heading per game mode on hybrid sets, so any difficulty is two clicks
 * away. Underneath, one segment per difficulty in spread order, coloured by its status, so every
 * difficulty's state stays in view and any of them is one click away. `[` and `]` step through
 * the spread.
 */
function DifficultyPicker({
  difficulties,
  general,
  modeStatus,
  selectedId,
  onSelect,
}: DifficultyPickerProps) {
  const theme = useMantineTheme();
  const [menuOpened, setMenuOpened] = useState(false);
  const listRef = useRef<HTMLDivElement>(null);
  useScrollToPicked(listRef, menuOpened);

  const modes = MODE_ORDER.map((mode) => ({
    mode,
    difficulties: difficulties.filter((d) => d.mode === mode).sort(byStarRating),
  })).filter((group) => group.difficulties.length > 0);
  const isHybrid = modes.length > 1;
  // Spread order: General, then each mode from easiest to hardest.
  const ordered = modes.flatMap((group) => group.difficulties);

  const isGeneral = !selectedId || selectedId === GENERAL_TAB_ID;
  const selected = ordered.find((d) => d.id === selectedId);
  // Every star badge in the menu takes the width of the widest one, so they line up.
  const menuStarRatings = ordered.flatMap((d) =>
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
        <Group gap="xs" wrap="nowrap" style={{ minWidth: 0 }}>
          <Menu
            trigger="hover"
            position="bottom-start"
            withinPortal
            opened={menuOpened}
            onChange={setMenuOpened}
          >
            <Menu.Target>
              <Button
                variant="default"
                size="sm"
                leftSection={
                  selected && (
                    <Group gap={6} wrap="nowrap">
                      {isHybrid && <GameModeIcon mode={selected.mode} size={18} />}
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
              <PickerList ref={listRef} rows={ordered.length + (isHybrid ? modes.length : 0)}>
                {modes.map((group, i) => (
                  <PickerModeGroup
                    key={group.mode}
                    mode={group.mode}
                    count={group.difficulties.length}
                    status={modeStatus?.(group.mode, group.difficulties)}
                    withHeading={isHybrid}
                    divided={i > 0}
                  >
                    {group.difficulties.map((d) => (
                      <Menu.Item
                        key={d.id}
                        disabled={d.disabled}
                        data-picked={d.id === selectedId || undefined}
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
                              <StarRatingBadge
                                rating={d.starRating}
                                size="xs"
                                sizeTo={menuStarRatings}
                              />
                            )
                          )
                        }
                        style={
                          d.id === selectedId
                            ? { background: 'var(--mantine-color-dark-5)' }
                            : undefined
                        }
                        onClick={() => onSelect(d.id)}
                      >
                        {d.label}
                      </Menu.Item>
                    ))}
                  </PickerModeGroup>
                ))}
              </PickerList>
            </Menu.Dropdown>
          </Menu>
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

/**
 * The scrolling list inside a picker's menu. Only a list that can't fit scrolls: always making it
 * scrollable let a few pixels of icon overflow turn into a scrollbar even for one difficulty. The
 * native scrollbar takes its own space, so it never covers the star ratings.
 */
export function PickerList({
  rows,
  ref,
  children,
}: {
  /** Rows in the list, headings included. */
  rows: number;
  ref?: Ref<HTMLDivElement>;
  children: ReactNode;
}) {
  return (
    <Box
      ref={ref}
      mah={MENU_MAX_HEIGHT}
      style={rows > MENU_MAX_VISIBLE ? { overflowY: 'auto', overflowX: 'hidden' } : undefined}
    >
      {children}
    </Box>
  );
}

interface PickerModeGroupProps {
  mode: Mode;
  count: number;
  /** Shown in front of the mode's icon, such as the worst check level among its difficulties. */
  status?: ReactNode;
  /** Hybrid sets only: a set with one mode needs no heading. */
  withHeading: boolean;
  /** A divider above the heading, for every mode after the first. */
  divided: boolean;
  children: ReactNode;
}

/**
 * One game mode's difficulties in a picker's menu, under a heading with the mode's icon, name and
 * count. The heading stays in view while its difficulties scroll past, and labels the group, so a
 * screen reader announces the mode before its difficulties.
 */
export function PickerModeGroup({
  mode,
  count,
  status,
  withHeading,
  divided,
  children,
}: PickerModeGroupProps) {
  const headingId = useId();
  if (!withHeading) return <>{children}</>;

  return (
    <div role="group" aria-labelledby={headingId}>
      {divided && <Menu.Divider />}
      <Menu.Label
        id={headingId}
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 1,
          background: 'var(--mantine-color-dark-6)',
        }}
      >
        <Group gap={6} wrap="nowrap">
          {status}
          <GameModeIcon mode={mode} size={16} />
          <Text span inherit style={{ flex: 1 }}>
            {formatGameModeLabel(mode)}
          </Text>
          <Text span inherit style={{ fontVariantNumeric: 'tabular-nums' }}>
            {count}
          </Text>
        </Group>
      </Menu.Label>
      {children}
    </div>
  );
}

/**
 * Opens a picker's menu scrolled to its pick (the first item marked `data-picked`), which can sit
 * well down the list on a hybrid set.
 */
export function useScrollToPicked(listRef: RefObject<HTMLDivElement | null>, opened: boolean) {
  useEffect(() => {
    if (!opened) return;
    const frame = requestAnimationFrame(() => {
      listRef.current?.querySelector('[data-picked]')?.scrollIntoView({ block: 'nearest' });
    });
    return () => cancelAnimationFrame(frame);
  }, [listRef, opened]);
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
