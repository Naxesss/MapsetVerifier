import { useMemo, useState } from 'react';
import { MAX_TIMELINE_WIDTH, MIN_TIMELINE_WIDTH } from '../constants.ts';
import { clampZoom, formatZoom, getTimelineIntervalMs, getZoomStep } from '../timelineUtils.ts';

export function useTimelineZoom(durationMs: number) {
  const [zoom, setZoom] = useState(8.0);

  const tickIntervalMs = useMemo(() => getTimelineIntervalMs(durationMs, zoom), [durationMs, zoom]);
  const timelineWidth = useMemo(
    () => Math.max(MIN_TIMELINE_WIDTH, Math.min(MAX_TIMELINE_WIDTH, Math.round(durationMs * 0.02 * zoom))),
    [durationMs, zoom]
  );

  const adjustZoom = (direction: -1 | 1) => {
    setZoom((value) => clampZoom(value + getZoomStep(value) * direction));
  };

  return {
    zoom,
    setZoom,
    tickIntervalMs,
    timelineWidth,
    adjustZoom,
    formatZoomLabel: formatZoom,
  };
}
