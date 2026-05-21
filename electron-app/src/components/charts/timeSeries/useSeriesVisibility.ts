import { useCallback, useEffect, useMemo, useState } from 'react';

export function useSeriesVisibility(seriesIds: string[], resetKey?: string) {
  const [visibilityById, setVisibilityById] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setVisibilityById(Object.fromEntries(seriesIds.map((id) => [id, true])));
  }, [resetKey, seriesIds]);

  useEffect(() => {
    setVisibilityById((current) => {
      let changed = false;
      const next = { ...current };

      for (const id of seriesIds) {
        if (!(id in next)) {
          next[id] = true;
          changed = true;
        }
      }

      return changed ? next : current;
    });
  }, [seriesIds]);

  const toggleSeries = useCallback((seriesId: string) => {
    setVisibilityById((current) => ({
      ...current,
      [seriesId]: current[seriesId] === false,
    }));
  }, []);

  const setAllVisible = useCallback(
    (visible: boolean) => {
      setVisibilityById((current) => {
        const next = { ...current };
        for (const id of seriesIds) {
          next[id] = visible;
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
    setAllVisible,
    isVisible,
    visibleSeriesIds,
  };
}
