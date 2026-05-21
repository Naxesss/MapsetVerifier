/** Shorter labels for Y axis ticks and hover values. */
export function formatAxisMetricValue(value: number, suffix?: string): string {
  if (suffix === '%') {
    return `${Math.round(value)}${suffix}`;
  }
  if (suffix === '★') {
    return `${value.toFixed(2)}${suffix}`;
  }
  return `${value.toFixed(2)}${suffix ?? ''}`;
}
