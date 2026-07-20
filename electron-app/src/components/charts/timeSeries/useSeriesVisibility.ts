import { useCallback, useMemo, useState } from 'react';

export function useSeriesVisibility(seriesIds: string[], resetKey?: string) {
  const [visibilityById, setVisibilityById] = useState<Record<string, boolean>>({});
  // Unset ids already default to visible below, so a new `seriesIds` array (e.g. switching game
  // modes, which uses a different id namespace per mode) doesn't need to wipe anything - only
  // `resetKey` changing (e.g. switching to a different beatmapset) should forget toggles.
  const [prevResetKey, setPrevResetKey] = useState(resetKey);

  if (prevResetKey !== resetKey) {
    setPrevResetKey(resetKey);
    setVisibilityById({});
  }

  const toggleSeries = useCallback((seriesId: string) => {
    setVisibilityById((current) => ({
      ...current,
      [seriesId]: current[seriesId] === false,
    }));
  }, []);

  // Grafana-style legend click: isolate the clicked series (hide the rest), or - if it's
  // already the only visible one - restore everyone. Ctrl/Cmd-click still uses `toggleSeries`
  // for independent per-series toggling.
  const toggleIsolateSeries = useCallback(
    (seriesId: string) => {
      setVisibilityById((current) => {
        const isOnlyThisVisible = seriesIds.every((id) => {
          const visible = current[id] !== false;
          return id === seriesId ? visible : !visible;
        });

        const next: Record<string, boolean> = {};
        for (const id of seriesIds) {
          next[id] = isOnlyThisVisible ? true : id === seriesId;
        }
        return next;
      });
    },
    [seriesIds]
  );

  const isVisible = useCallback(
    (seriesId: string) => visibilityById[seriesId] !== false,
    [visibilityById]
  );

  const visibleSeriesIds = useMemo(() => {
    const ids = new Set<string>();
    for (const id of seriesIds) {
      if (visibilityById[id] !== false) {
        ids.add(id);
      }
    }
    return ids;
  }, [seriesIds, visibilityById]);

  return {
    visibilityById,
    toggleSeries,
    toggleIsolateSeries,
    isVisible,
    visibleSeriesIds,
  };
}
