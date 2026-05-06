import { useEffect, useState } from 'react';
import { getDifficultyKey } from '../timelineUtils.ts';
import type { ObjectsOverviewDifficulty } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

export function useDifficultyRowVisibility(groupedDifficulties: ObjectsModeGroup[]) {
  const [visibilityByDifficulty, setVisibilityByDifficulty] = useState<Record<string, boolean>>({});

  useEffect(() => {
    const allDifficultyKeys = groupedDifficulties.flatMap((group) =>
      group.difficulties.map(getDifficultyKey)
    );

    setVisibilityByDifficulty((current) => {
      let changed = false;
      const next = { ...current };

      for (const key of allDifficultyKeys) {
        if (!(key in next)) {
          next[key] = true;
          changed = true;
        }
      }

      return changed ? next : current;
    });
  }, [groupedDifficulties]);

  const toggleDifficultyVisibility = (difficulty: ObjectsOverviewDifficulty) => {
    const difficultyKey = getDifficultyKey(difficulty);

    setVisibilityByDifficulty((current) => ({
      ...current,
      [difficultyKey]: current[difficultyKey] === false,
    }));
  };

  const setManyVisible = (difficulties: ObjectsOverviewDifficulty[], visible: boolean) => {
    setVisibilityByDifficulty((current) => {
      const next = { ...current };
      for (const difficulty of difficulties) {
        next[getDifficultyKey(difficulty)] = visible;
      }
      return next;
    });
  };

  return { visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible };
}
