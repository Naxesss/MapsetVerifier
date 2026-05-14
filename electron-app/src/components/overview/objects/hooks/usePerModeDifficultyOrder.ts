import { arrayMove } from '@dnd-kit/sortable';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { areStringArraysEqual, getDifficultyKey } from '../timelineUtils.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

export function usePerModeDifficultyOrder({
  groupedDifficulties,
  activeMode,
  difficulties,
}: {
  groupedDifficulties: ObjectsModeGroup[];
  activeMode: Mode | undefined;
  difficulties: ObjectsOverviewDifficulty[];
}) {
  const [difficultyOrderByMode, setDifficultyOrderByMode] = useState<
    Partial<Record<Mode, string[]>>
  >({});

  useEffect(() => {
    setDifficultyOrderByMode((current) => {
      let changed = false;
      const next: Partial<Record<Mode, string[]>> = { ...current };
      const availableModes = new Set(groupedDifficulties.map((group) => group.mode));

      for (const mode of Object.keys(next) as Mode[]) {
        if (!availableModes.has(mode)) {
          delete next[mode];
          changed = true;
        }
      }

      for (const group of groupedDifficulties) {
        const currentKeys = group.difficulties.map(getDifficultyKey);
        const existingOrder = next[group.mode] ?? [];
        const preservedOrder = existingOrder.filter((key) => currentKeys.includes(key));
        const missingKeys = currentKeys.filter((key) => !preservedOrder.includes(key));
        const mergedOrder = [...preservedOrder, ...missingKeys];

        if (!areStringArraysEqual(existingOrder, mergedOrder)) {
          next[group.mode] = mergedOrder;
          changed = true;
        }
      }

      return changed ? next : current;
    });
  }, [groupedDifficulties]);

  const orderedDifficulties = useMemo(() => {
    const difficultyMap = new Map(
      difficulties.map((difficulty) => [getDifficultyKey(difficulty), difficulty])
    );
    const currentOrder = activeMode ? difficultyOrderByMode[activeMode] : undefined;
    const fallbackOrder = difficulties.map(getDifficultyKey);
    const resolvedOrder = currentOrder && currentOrder.length > 0 ? currentOrder : fallbackOrder;
    const ordered = resolvedOrder
      .map((key) => difficultyMap.get(key))
      .filter((difficulty): difficulty is ObjectsOverviewDifficulty => difficulty !== undefined);
    const orderedKeys = new Set(ordered.map(getDifficultyKey));
    const missing = difficulties.filter(
      (difficulty) => !orderedKeys.has(getDifficultyKey(difficulty))
    );

    return [...ordered, ...missing];
  }, [activeMode, difficulties, difficultyOrderByMode]);

  const moveDifficulty = useCallback(
    (sourceKey: string, targetKey: string) => {
      if (!activeMode) return;

      setDifficultyOrderByMode((current) => {
        const currentOrder = current[activeMode] ?? orderedDifficulties.map(getDifficultyKey);
        const oldIndex = currentOrder.indexOf(sourceKey);
        const newIndex = currentOrder.indexOf(targetKey);
        if (oldIndex === -1 || newIndex === -1) {
          return current;
        }

        const nextOrder = arrayMove(currentOrder, oldIndex, newIndex);

        if (areStringArraysEqual(currentOrder, nextOrder)) {
          return current;
        }

        return {
          ...current,
          [activeMode]: nextOrder,
        };
      });
    },
    [activeMode, orderedDifficulties],
  );

  return { orderedDifficulties, moveDifficulty };
}
