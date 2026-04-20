import { Flex, Group, SegmentedControl } from '@mantine/core';
import { ApiSnapshotDifficulty, Mode } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon';

interface ModeGroup {
  mode: Mode;
  difficulties: ApiSnapshotDifficulty[];
}

interface SnapshotGameModeSelectorProps {
  groupedDifficulties: ModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}

function SnapshotGameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
}: SnapshotGameModeSelectorProps) {
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
              <GameModeIcon mode={group.mode} size={24} />
            </Flex>
          ),
          value: group.mode,
        }))}
        value={selectedMode}
        onChange={(val) => onModeChange(val as Mode)}
        fullWidth={false}
      />
    </Group>
  );
}

export default SnapshotGameModeSelector;

