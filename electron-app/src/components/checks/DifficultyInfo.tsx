import { Badge, Flex, Text, Tooltip } from '@mantine/core';
import { ApiCategoryCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import DifficultyName from '../common/DifficultyName';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';

interface DifficultyInfoProps {
  hoveredDifficulty?: ApiCategoryCheckResult;
  selectedCategory?: string;
  categoryHighestLevels: Record<string, Level>;
  currentOverrideResult?: ApiCategoryOverrideCheckResult;
}

const getDifficultyBadgeColor = (difficulty: string) => {
  switch (difficulty) {
    case 'Easy':
      return 'blue';
    case 'Normal':
      return 'green';
    case 'Hard':
      return 'yellow';
    case 'Insane':
      return 'red';
    case 'Expert':
      return 'grape';
    default:
      return 'grape';
  }
};

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
          <Tooltip label="Interpreted difficulty level">
            <Badge
              size="xs"
              color={getDifficultyBadgeColor(
                currentOverrideResult?.categoryResult.difficultyLevel ?? hoveredDifficulty.difficultyLevel,
              )}
              variant="light"
            >
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
          </Tooltip>
        )}
        {hoveredDifficulty.starRating != null && hoveredDifficulty.starRating > 0 && (
          <StarRatingBadge rating={hoveredDifficulty.starRating} />
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
