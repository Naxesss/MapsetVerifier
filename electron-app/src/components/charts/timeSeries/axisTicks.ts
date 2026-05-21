import { formatChartTime, getAdaptiveTimeInterval } from '../../common/TimeAxis.tsx';

export function buildMsTicks(viewMin: number, viewMax: number, spanMs: number): number[] {
  const spanSec = spanMs / 1000;
  let stepSec = getAdaptiveTimeInterval(spanSec);
  if (spanSec < stepSec * 1.5) {
    stepSec = Math.max(0.5, spanSec / 6);
  }
  const stepMs = stepSec * 1000;
  const ticks: number[] = [];
  let t = Math.ceil(viewMin / stepMs) * stepMs;
  let guard = 0;
  while (t <= viewMax && guard < 24) {
    ticks.push(t);
    t += stepMs;
    guard += 1;
  }
  if (ticks.length === 0) {
    return [viewMin, viewMax];
  }
  if (ticks[ticks.length - 1] < viewMax - stepMs * 0.05) {
    ticks.push(viewMax);
  }
  return ticks;
}

export function formatAxisTickMs(ms: number, spanMs: number): string {
  if (spanMs <= 45_000) {
    const mins = Math.floor(ms / 60000);
    const secs = Math.floor((ms % 60000) / 1000);
    const frac = Math.round(ms % 1000);
    if (spanMs < 12_000) {
      return `${mins}:${String(secs).padStart(2, '0')}.${String(frac).padStart(3, '0')}`;
    }
    return `${mins}:${String(secs).padStart(2, '0')}`;
  }
  return formatChartTime(ms / 1000);
}
