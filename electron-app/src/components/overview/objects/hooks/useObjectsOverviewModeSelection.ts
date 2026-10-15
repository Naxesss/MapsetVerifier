import { useMemo, useState } from 'react';
import type { Mode } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

export function useObjectsOverviewModeSelection(groupedDifficulties: ObjectsModeGroup[]) {
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();

  if (groupedDifficulties.length === 0) {
    if (selectedMode !== undefined) {
      setSelectedMode(undefined);
    }
  } else if (!selectedMode || !groupedDifficulties.some((group) => group.mode === selectedMode)) {
    setSelectedMode(groupedDifficulties[0].mode);
  }

  const selectedGroup = useMemo(
    () =>
      groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0],
    [groupedDifficulties, selectedMode]
  );

  return { selectedMode, setSelectedMode, selectedGroup };
}
