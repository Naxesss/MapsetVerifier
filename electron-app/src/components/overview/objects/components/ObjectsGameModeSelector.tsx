import { Flex, Group, SegmentedControl, Text } from '@mantine/core';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import type { Mode } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

interface ObjectsGameModeSelectorProps {
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}

export default function ObjectsGameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
}: ObjectsGameModeSelectorProps) {
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
