import { Badge, Flex, Text } from '@mantine/core';
import { IconStarFilled } from '@tabler/icons-react';
import { ApiCategoryCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import DifficultyName from '../common/DifficultyName';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';

interface DifficultyInfoProps {
  hoveredDifficulty?: ApiCategoryCheckResult;
  selectedCategory?: string;
  categoryHighestLevels: Record<string, Level>;
  currentOverrideResult?: ApiCategoryOverrideCheckResult;
}

function DifficultyInfo({
  hoveredDifficulty,
  selectedCategory,
  categoryHighestLevels,
  currentOverrideResult,
}: DifficultyInfoProps) {
  if (
    hoveredDifficulty &&
    (selectedCategory !== 'General' || hoveredDifficulty.category !== selectedCategory)
  ) {
    return (
      <Flex gap="xs" align="center">
        <LevelIcon level={categoryHighestLevels[hoveredDifficulty.category] || 'Info'} size={32} />
        <GameModeIcon
          mode={hoveredDifficulty.mode!}
          size={32}
          starRating={hoveredDifficulty.starRating}
        />
        <Text maw="60%">{hoveredDifficulty.category}</Text>
        {hoveredDifficulty.difficultyLevel && (
          <Badge size="xs" color="grape" variant="light">
            {currentOverrideResult ? (
              <DifficultyName
                difficulty={currentOverrideResult.categoryResult.difficultyLevel}
                mode={currentOverrideResult.categoryResult.mode}
              />
            ) : (
              <DifficultyName
                difficulty={hoveredDifficulty.difficultyLevel}
                mode={hoveredDifficulty.mode}
              />
            )}
          </Badge>
        )}
        {hoveredDifficulty.starRating != null && hoveredDifficulty.starRating > 0 && (
          <Badge size="xs" color="blue" variant="light" leftSection={<IconStarFilled size={10} />}>
            {hoveredDifficulty.starRating.toFixed(2)}
          </Badge>
        )}
      </Flex>
    );
  }

  return (
    <Flex gap="xs" align="center">
      <LevelIcon level={categoryHighestLevels['General'] || 'Info'} size={32} />
      <Text>General</Text>
    </Flex>
  );
}

export default DifficultyInfo;

