import { Badge, Tooltip } from '@mantine/core';
import { ApiCategoryCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import DifficultyName from '../common/DifficultyName';
import SelectedDifficultyRow from '../common/SelectedDifficultyRow.tsx';
import StarRatingBadge from '../common/StarRatingBadge.tsx';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';
import type { ReactNode } from 'react';

interface DifficultyInfoProps {
  /** The selected difficulty; undefined while General is selected. */
  difficulty?: ApiCategoryCheckResult;
  categoryHighestLevels: Record<string, Level>;
  currentOverrideResult?: ApiCategoryOverrideCheckResult;
  /** Controls for the selected difficulty, shown on the right of the same line. */
  actions?: ReactNode;
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
  difficulty,
  categoryHighestLevels,
  currentOverrideResult,
  actions,
}: DifficultyInfoProps) {
  if (difficulty) {
    return (
      <SelectedDifficultyRow
        icons={
          <>
            <LevelIcon level={categoryHighestLevels[difficulty.category] ?? 'Check'} size={32} />
            <GameModeIcon mode={difficulty.mode!} size={32} starRating={difficulty.starRating} />
          </>
        }
        name={difficulty.category}
        actions={actions}
        badges={
          <>
            {difficulty.difficultyLevel && (
              <Tooltip label="Interpreted difficulty level">
                <Badge
                  color={getDifficultyBadgeColor(
                    currentOverrideResult?.categoryResult.difficultyLevel ??
                      difficulty.difficultyLevel
                  )}
                >
                  {currentOverrideResult ? (
                    <DifficultyName
                      difficulty={currentOverrideResult.categoryResult.difficultyLevel}
                      mode={currentOverrideResult.categoryResult.mode}
                    />
                  ) : (
                    <DifficultyName
                      difficulty={difficulty.difficultyLevel}
                      mode={difficulty.mode}
                    />
                  )}
                </Badge>
              </Tooltip>
            )}
            {difficulty.starRating != null && difficulty.starRating > 0 && (
              <StarRatingBadge rating={difficulty.starRating} />
            )}
          </>
        }
      />
    );
  }

  return (
    <SelectedDifficultyRow
      icons={<LevelIcon level={categoryHighestLevels['General'] ?? 'Check'} size={32} />}
      name="General"
    />
  );
}

export default DifficultyInfo;
