import type { TimeSeriesRow } from "./types.ts";

const DEFAULT_MAX_RENDER_POINTS = 1000;

export function downsampleRows(rows: TimeSeriesRow[], maxPoints = DEFAULT_MAX_RENDER_POINTS): TimeSeriesRow[] {
    if (rows.length <= maxPoints) {
        return rows;
    }

    const step = Math.ceil(rows.length / maxPoints);
    const out = rows.filter((_, index) => index % step === 0);
    const last = rows[rows.length - 1];
    if (out[out.length - 1] !== last) {
        out.push(last);
    }
    return out;
}
