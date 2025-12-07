import { alpha, Button, Flex, Group, Text, useMantineTheme } from '@mantine/core';
import { ApiSnapshotDifficulty } from '../../Types';
import { getDifficultyColor } from '../common/DifficultyColor';

interface SnapshotDifficultySelectorProps {
  difficulties: ApiSnapshotDifficulty[];
  selectedDifficulty?: string;
  onSelectDifficulty: (difficulty: string) => void;
}

function SnapshotDifficultySelector({
  difficulties,
  selectedDifficulty,
  onSelectDifficulty,
}: SnapshotDifficultySelectorProps) {
  const theme = useMantineTheme();
  let sortedDifficulties = difficulties.sort((a, b) => a.starRating! - b.starRating!);

  return (
    <Group gap="xs">
      <Group
        p="xs"
        gap="xs"
        bg="hsl(200deg 10% 10% / 50%)"
        style={{ borderRadius: theme.radius.md }}
      >
        {/* General button */}
        <Button
          h="fit-content"
          p="xs"
          variant="light"
          style={{
            '--button-bg': `${alpha(theme.colors.blue[7], 0.25)}`,
            '--button-hover': `${alpha(theme.colors.blue[7], 0.35)}`,
          }}
          onClick={() => onSelectDifficulty('General')}
          bd={
            selectedDifficulty === 'General'
              ? `1px solid ${theme.colors.blue[7]}`
              : '1px solid transparent'
          }
        >
          <Flex gap="xs" align="center">
            <Text c="white">General</Text>
          </Flex>
        </Button>
        {/* Difficulty buttons */}
        {sortedDifficulties.map((diff) => {
          const color = getDifficultyColor(diff.starRating ?? 0);
          return (
            <Button
              key={diff.name}
              h="fit-content"
              p="xs"
              variant="light"
              style={{
                '--button-bg': `${alpha(color, 0.25)}`,
                '--button-hover': `${alpha(color, 0.35)}`,
              }}
              onClick={() => onSelectDifficulty(diff.name)}
              bd={
                selectedDifficulty === diff.name
                  ? `1px solid ${color}`
                  : '1px solid transparent'
              }
            >
              <Flex gap="xs" align="center">
                <Text
                  c="white"
                  style={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                    maxWidth: 400,
                  }}
                >
                  {diff.name}
                </Text>
              </Flex>
            </Button>
          );
        })}
      </Group>
    </Group>
  );
}

export default SnapshotDifficultySelector;

