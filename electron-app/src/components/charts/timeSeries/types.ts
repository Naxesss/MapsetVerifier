export type ChartViewport = {
  viewMin: number;
  viewMax: number;
  durationMs: number;
  isZoomed: boolean;
};

export type SeriesConfig = {
  id: string;
  key: string;
  label: string;
  color: string;
};

export type PeakHoverState = {
  timeMs: number;
  values: { seriesId: string; label: string; color: string; value: number }[];
};

/** Hover peak plus anchor position (px) relative to the chart wrapper (crosshair X, cursor Y). */
export type ChartHoverPayload = {
  peak: PeakHoverState;
  anchor: { x: number; y: number };
  /** Crosshair X and Y at top of plot (inline tooltip). */
  tooltipAnchor: { x: number; y: number };
  /** Pointer Y relative to chart wrapper (modal / cursor-following tooltip). */
  cursorY: number;
};

export type DragSelection = {
  startMs: number;
  endMs: number;
};

export type TimeSeriesRow = {
  timeMs: number;
  [seriesKey: string]: number | string | null;
};

export type ChartInterpolation = 'line' | 'step';
