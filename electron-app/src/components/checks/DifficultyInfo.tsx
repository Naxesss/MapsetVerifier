import { Badge, Tooltip } from '@mantine/core';
import { getDifficultyBadgeColor } from './DifficultyLevelOverride';
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
  /**
   * A control for the interpreted level, shown on the right of the row. It replaces the static
   * level badge, so only the star rating stays next to the name.
   */
  levelControl?: ReactNode;
}

function DifficultyInfo({
  difficulty,
  categoryHighestLevels,
  currentOverrideResult,
  levelControl,
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
        actions={levelControl}
        badges={
          <>
            {!levelControl && difficulty.difficultyLevel && (
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
