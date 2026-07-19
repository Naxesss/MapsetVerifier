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
  dashed?: boolean;
  /** Id used for visibility/toggle lookups when it differs from `id` (e.g. a companion line that
   *  should show/hide together with another series instead of getting its own legend toggle). */
  visibilityId?: string;
  /** Hide this series from the legend entirely; it still renders and follows `visibilityId`. */
  hideFromLegend?: boolean;
  /** Row key holding a forward-filled value, used for hover lookups on sparse series that are
   *  still rendered as a smooth line (so hovering between samples still shows a value). */
  hoverKey?: string;
};

export type PeakHoverState = {
  timeMs: number;
  values: {
    seriesId: string;
    label: string;
    color: string;
    value: number;
    /** Value of a companion series (e.g. spike) folded into this row instead of shown on its own. */
    secondaryValue?: number;
  }[];
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
