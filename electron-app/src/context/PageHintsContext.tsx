import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { OverviewTab } from '../components/navbar/pageHints.tsx';

interface PageHintsContextValue {
  overviewTab: OverviewTab | null;
  setOverviewTab: (tab: OverviewTab | null) => void;
  objectsHasHitsoundModes: boolean;
  setObjectsHasHitsoundModes: (value: boolean) => void;
}

const PageHintsContext = createContext<PageHintsContextValue | undefined>(undefined);

export function PageHintsProvider({ children }: { children: ReactNode }) {
  const [overviewTab, setOverviewTab] = useState<OverviewTab | null>(null);
  const [objectsHasHitsoundModes, setObjectsHasHitsoundModes] = useState(false);

  const value = useMemo(
    () => ({
      overviewTab,
      setOverviewTab,
      objectsHasHitsoundModes,
      setObjectsHasHitsoundModes,
    }),
    [overviewTab, objectsHasHitsoundModes]
  );

  return <PageHintsContext.Provider value={value}>{children}</PageHintsContext.Provider>;
}

export function usePageHints() {
  const context = useContext(PageHintsContext);
  if (!context) {
    throw new Error('usePageHints must be used within PageHintsProvider');
  }
  return context;
}
