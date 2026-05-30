import { Flex, Group, SegmentedControl } from '@mantine/core';
import { ApiCategoryCheckResult, Level, Mode } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';
import { getHighestLevel } from './utils/levelUtils';

interface ModeGroup {
  mode: Mode;
  difficulties: ApiCategoryCheckResult[];
}

interface GameModeSelectorProps {
  groupedDifficulties: ModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  categoryHighestLevels: Record<string, Level>;
  levelLoading?: boolean;
}

function GameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
  categoryHighestLevels,
  levelLoading = false,
}: GameModeSelectorProps) {
  return (
    <Group ml="auto" w="unset" gap="md" align="center">
      <SegmentedControl
        radius="md"
        p="xs"
        data={groupedDifficulties.map((group) => {
          const groupLevels = group.difficulties.map(
            (d) => categoryHighestLevels[d.category] ?? 'Check'
          );
          const groupHighestLevel = getHighestLevel(groupLevels);
          return {
            label: (
              <Flex gap="xs" align="center">
                <LevelIcon level={groupHighestLevel} size={24} loading={levelLoading} />
                <GameModeIcon mode={group.mode} size={24} />
              </Flex>
            ),
            value: group.mode,
          };
        })}
        value={selectedMode}
        onChange={(val) => onModeChange(val as Mode)}
        fullWidth={false}
      />
    </Group>
  );
}

export default GameModeSelector;
