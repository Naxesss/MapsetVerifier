import { clientXToDomainMs } from './plotGeometry.ts';
import type { PeakHoverState, SeriesConfig, TimeSeriesRow } from './types.ts';

export function filterRowsInViewport(
  rows: TimeSeriesRow[],
  viewMin: number,
  viewMax: number
): TimeSeriesRow[] {
  return rows.filter((row) => row.timeMs >= viewMin && row.timeMs <= viewMax);
}

export function findNearestPeakIndex(rows: TimeSeriesRow[], domainMs: number): number {
  if (rows.length === 0) {
    return -1;
  }

  let lo = 0;
  let hi = rows.length - 1;

  while (lo < hi) {
    const mid = Math.floor((lo + hi) / 2);
    if (rows[mid].timeMs < domainMs) {
      lo = mid + 1;
    } else {
      hi = mid;
    }
  }

  if (lo === 0) {
    return 0;
  }

  const prev = lo - 1;
  const distLo = Math.abs(rows[lo].timeMs - domainMs);
  const distPrev = Math.abs(rows[prev].timeMs - domainMs);
  return distPrev <= distLo ? prev : lo;
}

export function buildPeakHoverState(
  rows: TimeSeriesRow[],
  index: number,
  series: SeriesConfig[],
  visibleSeriesIds: Set<string>
): PeakHoverState | null {
  const row = rows[index];
  if (!row) {
    return null;
  }

  const values: PeakHoverState['values'] = [];
  const valueIndexById = new Map<string, number>();

  for (const item of series) {
    if (!visibleSeriesIds.has(item.visibilityId ?? item.id)) {
      continue;
    }
    const raw = row[item.hoverKey ?? item.key];
    if (typeof raw !== 'number') {
      continue;
    }

    // Companion series (e.g. the spike line) fold into their primary series' row instead of
    // showing up as a separate tooltip line.
    if (item.hideFromLegend && item.visibilityId) {
      const primaryIndex = valueIndexById.get(item.visibilityId);
      if (primaryIndex !== undefined) {
        values[primaryIndex].secondaryValue = raw;
        continue;
      }
    }

    valueIndexById.set(item.id, values.length);
    values.push({
      seriesId: item.id,
      label: item.label,
      color: item.color,
      value: raw,
    });
  }

  return {
    timeMs: row.timeMs,
    values,
  };
}

export function resolvePeakFromPointer(
  rows: TimeSeriesRow[],
  clientX: number,
  plotRect: DOMRect,
  viewMin: number,
  viewMax: number,
  series: SeriesConfig[],
  visibleSeriesIds: Set<string>
): PeakHoverState | null {
  if (rows.length === 0 || viewMax <= viewMin) {
    return null;
  }

  const domainMs = clientXToDomainMs(clientX, plotRect, viewMin, viewMax);
  const index = findNearestPeakIndex(rows, domainMs);
  if (index < 0) {
    return null;
  }

  return buildPeakHoverState(rows, index, series, visibleSeriesIds);
}
