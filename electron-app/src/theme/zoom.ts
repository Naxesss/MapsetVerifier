export const DEFAULT_UI_ZOOM_PERCENT = 100;

const UI_ZOOM_PERCENTS = [75, 80, 90, 100, 110, 125, 150, 175, 200];

export const UI_ZOOM_OPTIONS: { value: string; label: string }[] = UI_ZOOM_PERCENTS.map(
  (percent) => ({
    value: String(percent),
    label: percent === DEFAULT_UI_ZOOM_PERCENT ? `${percent}% (Default)` : `${percent}%`,
  })
);

export function parseUiZoomPercent(value: unknown): number {
  const percent = Number(value);
  return UI_ZOOM_PERCENTS.includes(percent) ? percent : DEFAULT_UI_ZOOM_PERCENT;
}
