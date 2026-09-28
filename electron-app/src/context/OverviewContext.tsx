import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { OverviewTab } from '../components/navbar/pageHints.tsx';
import type { Mode } from '../Types';

interface OverviewContextValue {
  tab: OverviewTab;
  setTab: (tab: OverviewTab) => void;
  /** Mode shown on Objects, Beatmap and Difficulty; unset until a hybrid mapset picks one. */
  mode: Mode | undefined;
  setMode: (mode: Mode) => void;
  /** Difficulties picked to compare, by version, and the mapset they were picked in. */
  picks: DifficultyPicks;
  setPicks: (picks: DifficultyPicks) => void;
}

export interface DifficultyPicks {
  folder: string | undefined;
  versions: ReadonlySet<string>;
}

const OverviewContext = createContext<OverviewContextValue | undefined>(undefined);

/**
 * The Overview's section, game mode and picked difficulties, kept above the page so they survive
 * switching tabs, going to Checks and back, and (section and mode) switching mapsets. A mode the
 * next mapset doesn't have falls back to its first; picks only apply to the mapset they came from.
 */
export function OverviewProvider({ children }: { children: ReactNode }) {
  const [tab, setTab] = useState<OverviewTab>('Metadata');
  const [mode, setMode] = useState<Mode | undefined>();
  const [picks, setPicks] = useState<DifficultyPicks>({ folder: undefined, versions: new Set() });

  const value = useMemo(
    () => ({ tab, setTab, mode, setMode, picks, setPicks }),
    [tab, mode, picks]
  );

  return <OverviewContext.Provider value={value}>{children}</OverviewContext.Provider>;
}

export function useOverviewState() {
  const context = useContext(OverviewContext);
  if (!context) {
    throw new Error('useOverviewState must be used within OverviewProvider');
  }
  return context;
}
