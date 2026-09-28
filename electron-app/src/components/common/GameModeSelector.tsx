import { Flex, Group, SegmentedControl, Text } from '@mantine/core';
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

/** Picks a game mode for a mapset with several, showing each mode's difficulty count. */
export default function GameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
}: GameModeSelectorProps) {
  if (groupedDifficulties.length <= 1) {
    return null;
  }

  return (
    <Group ml="auto" w="unset" gap="md" align="center">
      <SegmentedControl
        radius="md"
        p="xs"
        data={groupedDifficulties.map((group) => ({
          label: (
            <Flex gap="xs" align="center">
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
    </Group>
  );
}
