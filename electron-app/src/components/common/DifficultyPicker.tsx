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
import { getDifficultyColor } from './DifficultyColor';
import DifficultyColorPill from './DifficultyColorPill';
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

  const separator = (
    <Text c="dimmed" fw={700} aria-hidden>
      /
    </Text>
  );
  const chevron = <IconChevronDown size={14} stroke={1.5} />;

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
            {separator}
            <Menu position="bottom-start" withinPortal>
              <Menu.Target>
                <Button
                  variant="default"
                  size="sm"
                  leftSection={
                    <Group gap={6} wrap="nowrap">
                      {activeGroup && modeStatus?.(activeGroup.mode, activeGroup.difficulties)}
                      {activeMode && <GameModeIcon mode={activeMode} size={18} />}
                    </Group>
                  }
                  rightSection={chevron}
                  style={{ flexShrink: 0 }}
                >
                  {activeMode && formatGameModeLabel(activeMode)}
                </Button>
              </Menu.Target>
              <Menu.Dropdown>
                {modes.map((group) => (
                  <Menu.Item
                    key={group.mode}
                    leftSection={
                      <Group gap={6} wrap="nowrap">
                        {modeStatus?.(group.mode, group.difficulties)}
                        <GameModeIcon mode={group.mode} size={18} />
                      </Group>
                    }
                    rightSection={
                      <Text size="xs" c="dimmed" style={{ fontVariantNumeric: 'tabular-nums' }}>
                        {group.difficulties.length}
                      </Text>
                    }
                    onClick={() => selectMode(group.mode)}
                  >
                    {formatGameModeLabel(group.mode)}
                  </Menu.Item>
                ))}
              </Menu.Dropdown>
            </Menu>
          </>
        )}
        {separator}
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
              rightSection={chevron}
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
                    <Text size="xs" c="dimmed" style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {d.disabled ? d.disabledReason : d.starRating?.toFixed(2)}
                    </Text>
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
          <Tooltip label="Previous difficulty ([)">
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
          <Tooltip label="Next difficulty (])">
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
      {/* One segment per difficulty in spread order: the same small gap between every segment, a
          wider one after General and between modes. The row sits the header's usual row gap (sm)
          under the controls. */}
      <Group
        gap={SEGMENT_GAP}
        wrap="nowrap"
        align="center"
        mt="sm"
        h={SEGMENT_ACTIVE_HEIGHT}
        aria-label="Difficulties at a glance"
      >
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
                color={d.disabled ? theme.colors.dark[4] : d.statusColor}
                active={d.id === selectedId}
                disabled={d.disabled}
                onClick={() => onSelect(d.id)}
              />
            ))}
          </Fragment>
        ))}
      </Group>
    </Box>
  );
}

const SEGMENT_GAP = 3;
const SEGMENT_GROUP_GAP = 10;
const SEGMENT_HEIGHT = 6;
const SEGMENT_ACTIVE_HEIGHT = 10;

interface StatusSegmentProps {
  label: string;
  /** First segment of a mode, which gets the wider gap in front of it. */
  groupStart?: boolean;
  color: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
}

/**
 * The selected segment is taller rather than outlined: an outline spills into the gaps next to it,
 * which made them look uneven.
 */
function StatusSegment({
  label,
  groupStart,
  color,
  active,
  disabled,
  onClick,
}: StatusSegmentProps) {
  return (
    <Tooltip label={label} openDelay={100}>
      <UnstyledButton
        aria-label={label}
        aria-current={active || undefined}
        aria-disabled={disabled || undefined}
        onClick={disabled ? undefined : onClick}
        style={{
          flex: 1,
          height: active ? SEGMENT_ACTIVE_HEIGHT : SEGMENT_HEIGHT,
          marginLeft: groupStart ? SEGMENT_GROUP_GAP - SEGMENT_GAP : undefined,
          borderRadius: 2,
          backgroundColor: color,
          opacity: active ? 1 : 0.55,
          cursor: disabled ? 'not-allowed' : 'pointer',
        }}
      />
    </Tooltip>
  );
}

export default DifficultyPicker;
