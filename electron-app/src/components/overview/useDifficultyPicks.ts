import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useOverviewState } from '../../context/OverviewContext.tsx';

interface PickableDifficulty {
  version: string;
  starRating: number | null;
}

/**
 * The difficulties picked to compare on the Overview, shared by its tabs. Nothing picked shows
 * every difficulty. Picks belong to the mapset they were made in, and ones that aren't in the
 * current mode are ignored rather than dropped, so switching back to that mode keeps them.
 */
export function useDifficultyPicks(difficulties: readonly PickableDifficulty[]) {
  const { selectedFolder } = useBeatmap();
  const { picks, setPicks } = useOverviewState();
  const picked = picks.folder === selectedFolder ? picks.versions : new Set<string>();
  const showAll = !difficulties.some((difficulty) => picked.has(difficulty.version));

  const isShown = (version: string) => showAll || picked.has(version);
  const toggle = (version: string) => {
    const next = new Set(picked);
    if (next.has(version)) next.delete(version);
    else next.add(version);
    setPicks({ folder: selectedFolder, versions: next });
  };
  const clear = () => setPicks({ folder: selectedFolder, versions: new Set() });

  return { isShown, showAll, picked, toggle, clear };
}
