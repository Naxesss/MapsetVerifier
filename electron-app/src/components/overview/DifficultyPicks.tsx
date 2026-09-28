import { Box, Group } from '@mantine/core';
import { useEffect, useRef, useState } from 'react';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useOverviewState } from '../../context/OverviewContext.tsx';
import { Z_INDEX } from '../../theme/layers.ts';
import { getDifficultyColor } from '../common/DifficultyColor.ts';
import DifficultyColorPill from '../common/DifficultyColorPill.tsx';
import FilterChip from '../common/FilterChip.tsx';

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

/**
 * Chips to pick which difficulties the tab compares, each in its star rating colour, with "All" to
 * show every one again. Nothing for two or fewer difficulties, which fit side by side anyway.
 * The row sticks to the top of the tab while it scrolls (`.mv-sticky-picks` in global.scss), and
 * casts a shadow only once content passes under it.
 */
export default function DifficultyPicks({
  difficulties,
}: {
  difficulties: readonly PickableDifficulty[];
}) {
  const { showAll, picked, toggle, clear } = useDifficultyPicks(difficulties);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const [stuck, setStuck] = useState(false);
  const shown = difficulties.length > 2;

  // A marker just above the row: once it has scrolled above the page's scroll area, the row is
  // stuck to the top.
  useEffect(() => {
    const sentinel = sentinelRef.current;
    const scroller = sentinel?.closest('.mantine-ScrollArea-viewport');
    if (!sentinel || !scroller) return;
    const update = () =>
      setStuck(sentinel.getBoundingClientRect().top < scroller.getBoundingClientRect().top);
    update();
    scroller.addEventListener('scroll', update, { passive: true });
    return () => scroller.removeEventListener('scroll', update);
  }, [shown]);

  if (!shown) {
    return null;
  }

  return (
    <>
      <div ref={sentinelRef} aria-hidden style={{ height: 0 }} />
      <Box
        className="mv-sticky-picks"
        data-stuck={stuck || undefined}
        style={{ zIndex: Z_INDEX.stickyControls }}
      >
        <Group gap="xs" role="group" aria-label="Difficulties to compare">
          <FilterChip label="All" color="blue" active={showAll} onClick={clear} />
          {difficulties.map((difficulty) => (
            <FilterChip
              key={difficulty.version}
              label={difficulty.version}
              color="blue"
              icon={
                <DifficultyColorPill
                  color={getDifficultyColor(difficulty.starRating ?? 0)}
                  height={14}
                />
              }
              active={!showAll && picked.has(difficulty.version)}
              onClick={() => toggle(difficulty.version)}
            />
          ))}
        </Group>
      </Box>
    </>
  );
}
