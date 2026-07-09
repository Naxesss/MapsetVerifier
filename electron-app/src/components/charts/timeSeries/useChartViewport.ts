import { useCallback, useMemo, useState } from 'react';

export const MIN_DRAG_ZOOM_MS = 600;

type ZoomRange = { min: number; max: number };

export function useChartViewport(durationMs: number, resetKey?: string) {
  const [zoom, setZoom] = useState<ZoomRange | null>(null);
  const [prevResetToken, setPrevResetToken] = useState({ durationMs, resetKey });

  if (prevResetToken.durationMs !== durationMs || prevResetToken.resetKey !== resetKey) {
    setPrevResetToken({ durationMs, resetKey });
    setZoom(null);
  }

  const viewMin = zoom?.min ?? 0;
  const viewMax = zoom?.max ?? Math.max(durationMs, 1);
  const spanMs = Math.max(1, viewMax - viewMin);
  const isZoomed = zoom !== null;

  const resetZoom = useCallback(() => {
    setZoom(null);
  }, []);

  const applyDragZoom = useCallback(
    (startMs: number, endMs: number) => {
      const lo = Math.min(startMs, endMs);
      const hi = Math.max(startMs, endMs);
      const minSpan = Math.max(MIN_DRAG_ZOOM_MS, spanMs * 0.015);
      if (hi - lo < minSpan) {
        return;
      }
      const nextMin = Math.max(0, lo);
      const nextMax = Math.min(durationMs, hi);
      if (nextMax - nextMin >= minSpan) {
        setZoom({ min: nextMin, max: nextMax });
      }
    },
    [durationMs, spanMs]
  );

  return useMemo(
    () => ({
      viewMin,
      viewMax,
      spanMs,
      durationMs,
      isZoomed,
      resetZoom,
      applyDragZoom,
    }),
    [applyDragZoom, durationMs, isZoomed, resetZoom, spanMs, viewMax, viewMin]
  );
}
