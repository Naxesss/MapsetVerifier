export const CHART_PLOT_SELECTOR = '[data-chart-plot]';

export function clientXToDomainMs(
  clientX: number,
  plotRect: DOMRect,
  viewMin: number,
  viewMax: number
): number {
  const t = (clientX - plotRect.left) / plotRect.width;
  const clamped = Math.max(0, Math.min(1, t));
  return viewMin + clamped * (viewMax - viewMin);
}

export function domainMsToPlotX(
  ms: number,
  plotRect: DOMRect,
  viewMin: number,
  viewMax: number
): number {
  if (viewMax <= viewMin) {
    return plotRect.left;
  }
  const t = (ms - viewMin) / (viewMax - viewMin);
  return plotRect.left + Math.max(0, Math.min(1, t)) * plotRect.width;
}

export function queryPlotRect(container: HTMLElement): DOMRect | null {
  const plot = container.querySelector(CHART_PLOT_SELECTOR);
  // Plot is an SVG <rect> (SVGGraphicsElement), not an HTMLElement.
  if (!(plot instanceof SVGGraphicsElement)) {
    return null;
  }
  const rect = plot.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return null;
  }
  return rect;
}
