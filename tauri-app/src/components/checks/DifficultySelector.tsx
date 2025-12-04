import { alpha, Button, Flex, Group, Text, useMantineTheme } from '@mantine/core';
import { ApiCategoryCheckResult, Level } from '../../Types';
import { getDifficultyColor } from '../common/DifficultyColor';
import LevelIcon from '../icons/LevelIcon';

interface DifficultySelectorProps {
  difficulties: ApiCategoryCheckResult[];
  selectedCategory?: string;
  hoveredDifficulty?: ApiCategoryCheckResult;
  categoryHighestLevels: Record<string, Level>;
  onSelectCategory: (category: string) => void;
  onHoverDifficulty: (diff: ApiCategoryCheckResult | undefined) => void;
  selectedDifficulty?: ApiCategoryCheckResult;
}

function DifficultySelector({
  difficulties,
  selectedCategory,
  hoveredDifficulty,
  categoryHighestLevels,
  onSelectCategory,
  onHoverDifficulty,
  selectedDifficulty,
}: DifficultySelectorProps) {
  const theme = useMantineTheme();

  return (
    <Group gap="xs">
      <Group
        p="xs"
        gap="xs"
        bg="hsl(200deg 10% 10% / 50%)"
        style={{ borderRadius: theme.radius.md }}
      >
        <Button
          h="fit-content"
          p="xs"
          variant="light"
          style={{
            '--button-bg': `${alpha(theme.colors.blue[7], 0.25)}`,
            '--button-hover': `${alpha(theme.colors.blue[7], 0.25)}`,
          }}
          onClick={() => onSelectCategory('General')}
          onMouseEnter={() => onHoverDifficulty(undefined)}
          onMouseLeave={() => onHoverDifficulty(selectedDifficulty)}
          bd={
            selectedCategory === 'General' || hoveredDifficulty === undefined
              ? '1px solid var(--mantine-color-blue-6)'
              : '1px solid transparent'
          }
        >
          <Flex gap="xs" align="center">
            <LevelIcon level={categoryHighestLevels['General'] || 'Info'} size={24} />
            <Text c="white">General</Text>
          </Flex>
        </Button>
        {difficulties.map((diff) => (
          <Button
            key={diff.category}
            onClick={() => onSelectCategory(diff.category)}
            variant="light"
            style={{
              '--button-bg': `${alpha(getDifficultyColor(diff.starRating ?? 0), 0.25)}`,
              '--button-hover': `${alpha(getDifficultyColor(diff.starRating ?? 0), 0.25)}`,
            }}
            size="compact-md"
            h="fit-content"
            p="xs"
            color={getDifficultyColor(diff.starRating ?? 0)}
            onMouseEnter={() => onHoverDifficulty(diff)}
            onMouseLeave={() => onHoverDifficulty(selectedDifficulty)}
            bd={
              hoveredDifficulty === diff || selectedCategory === diff.category
                ? `1px solid ${getDifficultyColor(diff.starRating ?? 0)}`
                : '1px solid transparent'
            }
          >
            <Flex gap="xs" align="center">
              <LevelIcon level={categoryHighestLevels[diff.category] || 'Info'} size={24} />
              <Text
                c="white"
                style={{
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                  maxWidth: 400,
                }}
              >
                {diff.category}
              </Text>
            </Flex>
          </Button>
        ))}
      </Group>
    </Group>
  );
}

export default DifficultySelector;

