import { useEffect, useMemo, useState } from 'react';
import type { Mode } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

export function useObjectsOverviewModeSelection(groupedDifficulties: ObjectsModeGroup[]) {
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();

  useEffect(() => {
    if (groupedDifficulties.length === 0) {
      setSelectedMode(undefined);
      return;
    }

    if (!selectedMode || !groupedDifficulties.some((group) => group.mode === selectedMode)) {
      setSelectedMode(groupedDifficulties[0].mode);
    }
  }, [groupedDifficulties, selectedMode]);

  const selectedGroup = useMemo(
    () =>
      groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0],
    [groupedDifficulties, selectedMode]
  );

  return { selectedMode, setSelectedMode, selectedGroup };
}
