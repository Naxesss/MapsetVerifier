import { useHotkeys } from '@mantine/hooks';
import { IconChevronDown } from '@tabler/icons-react';
import { type ReactNode, useState } from 'react';
import DifficultyButtonGrid, { spreadGroups } from './DifficultyButtonGrid';
import type { Mode } from '../../Types';

export const GENERAL_TAB_ID = 'General';
/** The size of the status icons on the picker's buttons. */
export const PICKER_ICON_SIZE = 16;

export type PickerDifficulty = {
  id: string;
  label: string;
  mode: Mode;
  starRating?: number | null;
  /** The status as an icon after the name: the level on Checks, changed or not on Snapshots. */
  status: ReactNode;
};

interface DifficultyPickerProps {
  difficulties: PickerDifficulty[];
  general: { status: ReactNode };
  /** A mode button's status icon, from the ids of that mode's difficulties. */
  modeStatus: (mode: Mode, ids: string[]) => ReactNode;
  selectedId?: string;
  onSelect: (id: string) => void;
}

/**
 * Picks the difficulty on Checks and Snapshots: General on the left, then one mode's difficulties
 * as buttons with their names (DifficultyButtonGrid), each one's status as an icon. The mode
 * shown is the picked difficulty's; with General picked, the one last shown. Switching modes with
 * a difficulty picked picks that mode's easiest. `[` and `]` step through the whole spread,
 * switching modes on the way.
 */
function DifficultyPicker({
  difficulties,
  general,
  modeStatus,
  selectedId,
  onSelect,
}: DifficultyPickerProps) {
  const isGeneral = !selectedId || selectedId === GENERAL_TAB_ID;
  // Spread order: General, then each mode from easiest to hardest.
  const groups = spreadGroups(difficulties);
  const ordered = groups.flatMap((group) => group.difficulties);

  const [generalMode, setGeneralMode] = useState<Mode>();
  const selected = difficulties.find((d) => d.id === selectedId);
  const shownMode = selected?.mode ?? generalMode;
  const showMode = (mode: Mode) => {
    setGeneralMode(mode);
    if (isGeneral) return;
    const easiest = groups.find((group) => group.mode === mode)?.difficulties[0];
    if (easiest) onSelect(easiest.id);
  };

  const steps = [GENERAL_TAB_ID, ...ordered.map((d) => d.id)];
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
    <DifficultyButtonGrid
      lead={{
        label: 'General',
        status: general.status,
        active: isGeneral,
        onClick: () => onSelect(GENERAL_TAB_ID),
      }}
      difficulties={difficulties.map((d) => ({ ...d, active: d.id === selectedId }))}
      onPick={(d) => onSelect(d.id)}
      shownMode={shownMode}
      onShowMode={showMode}
      modeStatus={(mode, diffs) =>
        modeStatus(
          mode,
          diffs.map((d) => d.id)
        )
      }
    />
  );
}

export const PICKER_CHEVRON = <IconChevronDown size={14} stroke={1.5} />;

export default DifficultyPicker;
