import { Flex, Group, SegmentedControl, Text } from '@mantine/core';
import { countWord } from '../../utils/countWord.ts';
import { formatGameModeLabel } from '../../utils/gameMode.ts';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import type { Mode } from '../../Types';

/** Difficulties of one game mode; only the count is shown. */
interface ModeGroup {
  mode: Mode;
  difficulties: readonly unknown[];
}

interface GameModeSelectorProps {
  groupedDifficulties: readonly ModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}

/**
 * Picks the game mode of a hybrid mapset, once at the top of an Overview section: each mode's icon
 * and difficulty count, then the selected mode's name. Nothing for a single-mode mapset.
 */
export default function GameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
}: GameModeSelectorProps) {
  if (groupedDifficulties.length <= 1) {
    return null;
  }

  const selectedGroup = groupedDifficulties.find((group) => group.mode === selectedMode);

  return (
    <Group gap="sm" align="center">
      <SegmentedControl
        radius="md"
        p="xs"
        aria-label="Game mode"
        data={groupedDifficulties.map((group) => ({
          label: (
            <Flex gap="xs" align="center" aria-label={formatGameModeLabel(group.mode)}>
              <GameModeIcon mode={group.mode} size={22} color="currentColor" />
              <Text size="xs" fw={600}>
                {group.difficulties.length}
              </Text>
            </Flex>
          ),
          value: group.mode,
        }))}
        value={selectedMode}
        onChange={(value) => onModeChange(value as Mode)}
        fullWidth={false}
      />
      {selectedGroup && (
        <Text fw={700}>
          {formatGameModeLabel(selectedGroup.mode)}{' '}
          <Text span c="dimmed" fw={600}>
            · {countWord(selectedGroup.difficulties.length, 'difficulty')}
          </Text>
        </Text>
      )}
    </Group>
  );
}
