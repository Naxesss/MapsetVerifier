import { useOverviewState } from '../../context/OverviewContext.tsx';
import type { Mode } from '../../Types';

/**
 * The mode shown on Objects, Beatmap and Difficulty, shared between them and kept while switching
 * sections, pages and mapsets. A mode this mapset doesn't have falls back to its first mode.
 */
export function useOverviewMode<G extends { mode: Mode }>(groupedDifficulties: readonly G[]) {
  const { mode, setMode } = useOverviewState();
  const selectedGroup =
    groupedDifficulties.find((group) => group.mode === mode) ?? groupedDifficulties[0];

  return { selectedMode: selectedGroup?.mode, setSelectedMode: setMode, selectedGroup };
}
