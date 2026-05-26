import { useCallback, useMemo, useState } from 'react';
import { MIN_TIMELINE_WIDTH } from '../constants.ts';
import { clampZoom, formatZoom, getTimelineIntervalMs, getZoomStep } from '../timelineUtils.ts';

export function useTimelineZoom(durationMs: number) {
  const [zoom, setZoomState] = useState(9.0);

  const setZoom = useCallback((value: number) => {
    setZoomState(clampZoom(value));
  }, []);

  const tickIntervalMs = useMemo(() => getTimelineIntervalMs(durationMs, zoom), [durationMs, zoom]);
  const timelineWidth = useMemo(
    () => Math.max(MIN_TIMELINE_WIDTH, Math.round(durationMs * 0.02 * zoom)),
    [durationMs, zoom]
  );

  const adjustZoom = useCallback((direction: -1 | 1) => {
    setZoomState((value) => clampZoom(value + getZoomStep(value) * direction));
  }, []);

  return {
    zoom,
    setZoom,
    tickIntervalMs,
    timelineWidth,
    adjustZoom,
    formatZoomLabel: formatZoom,
  };
}
