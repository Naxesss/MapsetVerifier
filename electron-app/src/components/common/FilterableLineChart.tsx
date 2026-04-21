import { LineChart } from "@mantine/charts";
import {
    Box,
    Button,
    ColorSwatch,
    getThemeColor,
    Group,
    Stack,
    Text,
    useMantineTheme,
} from "@mantine/core";
import { IconZoomReset } from "@tabler/icons-react";
import { useCallback, useEffect, useMemo, useRef, useState, type ComponentProps, type ReactNode } from "react";
import { formatChartTime, formatTooltipTimeMs, getAdaptiveTimeInterval } from "./TimeAxis.tsx";

type MantineLineChartProps = ComponentProps<typeof LineChart>;

const DEFAULT_VIEWPORT_POINTS = 300;
const CHART_MARGIN = { top: 6, right: 16, left: 54, bottom: 10 };
const MIN_DRAG_ZOOM_MS = 600;

function buildViewportData<T extends Record<string, unknown>>(
    full: T[],
    viewMin: number,
    viewMax: number,
    timeMsKey: string,
    maxPoints: number,
): T[] {
    const filtered = full.filter((row) => {
        const t = row[timeMsKey];
        return typeof t === "number" && t >= viewMin && t <= viewMax;
    });
    if (filtered.length <= maxPoints) {
        return filtered;
    }
    const step = Math.ceil(filtered.length / maxPoints);
    const out = filtered.filter((_, i) => i % step === 0);
    const last = filtered[filtered.length - 1];
    if (out[out.length - 1] !== last) {
        out.push(last);
    }
    return out;
}

function buildMsTicks(viewMin: number, viewMax: number, spanMs: number): number[] {
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

function formatAxisTickMs(ms: number, spanMs: number): string {
    if (spanMs <= 45_000) {
        const mins = Math.floor(ms / 60000);
        const secs = Math.floor((ms % 60000) / 1000);
        const frac = Math.round(ms % 1000);
        if (spanMs < 12_000) {
            return `${mins}:${String(secs).padStart(2, "0")}.${String(frac).padStart(3, "0")}`;
        }
        return `${mins}:${String(secs).padStart(2, "0")}`;
    }
    return formatChartTime(ms / 1000);
}

export type FilterableLineChartProps = Omit<MantineLineChartProps, "series" | "withLegend"> & {
    series: NonNullable<MantineLineChartProps["series"]>;
    /**
     * When set with `withDragZoom`, enables click-drag to zoom and higher point density in the visible window.
     */
    durationMs?: number;
    withDragZoom?: boolean;
    /** Max points passed to Recharts after viewport filter (default 300). */
    maxViewportPoints?: number;
    /** Row field for X axis in ms (default `timeMs`). */
    timeMsKey?: string;
};

function mergeLineChartChildren(userChildren: ReactNode | undefined, extra: ReactNode | null): ReactNode {
    if (extra == null) {
        return userChildren;
    }
    if (userChildren == null || userChildren === false) {
        return extra;
    }
    return (
        <>
            {userChildren}
            {extra}
        </>
    );
}

/**
 * Wraps Mantine LineChart: with multiple series, legend is clickable to show one series;
 * click the active series again to show all. Single-series charts use the default Mantine legend.
 * With `durationMs` + `withDragZoom`, click-drag on the plot zooms; reset clears the window.
 */
export function FilterableLineChart({
    series,
    children: userChildren,
    data,
    dataKey,
    durationMs: durationMsProp,
    withDragZoom = true,
    maxViewportPoints = DEFAULT_VIEWPORT_POINTS,
    timeMsKey = "timeMs",
    ...rest
}: FilterableLineChartProps) {
    const theme = useMantineTheme();
    const wrapRef = useRef<HTMLDivElement>(null);
    const dragRef = useRef<{ startMs: number; endMs: number } | null>(null);
    const [isolatedName, setIsolatedName] = useState<string | null>(null);
    const [zoom, setZoom] = useState<{ min: number; max: number } | null>(null);
    const [drag, setDrag] = useState<{ startMs: number; endMs: number } | null>(null);

    const {
        xAxisProps: passthroughXAxis,
        lineChartProps: passthroughLine,
        tooltipProps: passthroughTooltip,
        ...lineChartRest
    } = rest;

    const inferredDuration =
        typeof durationMsProp === "number" && durationMsProp > 0
            ? durationMsProp
            : Array.isArray(data) && data.length > 0
              ? Math.max(
                    ...data.map((row) => {
                        const t = (row as Record<string, unknown>)[timeMsKey];
                        return typeof t === "number" ? t : 0;
                    }),
                ) + 400
              : 0;

    const hasTimeMs =
        Array.isArray(data) && data.length > 0 && typeof (data[0] as Record<string, unknown>)[timeMsKey] === "number";

    const dragZoomActive = Boolean(withDragZoom && inferredDuration > 0 && hasTimeMs && dataKey === timeMsKey);

    useEffect(() => {
        if (isolatedName !== null && !series.some((s) => s.name === isolatedName)) {
            setIsolatedName(null);
        }
    }, [series, isolatedName]);

    useEffect(() => {
        dragRef.current = null;
        setZoom(null);
        setDrag(null);
    }, [data, durationMsProp]);

    const viewMin = zoom?.min ?? 0;
    const viewMax = zoom?.max ?? inferredDuration;
    const spanMs = Math.max(1, viewMax - viewMin);

    const displayData = useMemo(() => {
        if (!Array.isArray(data) || !dragZoomActive) {
            return data;
        }
        return buildViewportData(
            data as Record<string, unknown>[],
            viewMin,
            viewMax,
            timeMsKey,
            maxViewportPoints,
        ) as typeof data;
    }, [data, dragZoomActive, viewMin, viewMax, timeMsKey, maxViewportPoints]);

    const clientXToMs = useCallback(
        (clientX: number): number | null => {
            const el = wrapRef.current;
            if (!el || viewMax <= viewMin) {
                return null;
            }
            const rect = el.getBoundingClientRect();
            const margin = { ...CHART_MARGIN, ...passthroughLine?.margin };
            const plotW = rect.width - margin.left - margin.right;
            if (plotW <= 0) {
                return null;
            }
            const x = clientX - rect.left - margin.left;
            const clamped = Math.max(0, Math.min(plotW, x));
            return viewMin + (clamped / plotW) * (viewMax - viewMin);
        },
        [viewMin, viewMax, passthroughLine?.margin],
    );

    const onPointerDown = (e: React.PointerEvent) => {
        if (!dragZoomActive || e.button !== 0) {
            return;
        }
        const target = e.target as HTMLElement;
        if (target.closest('button,a,input,textarea,[role="slider"]')) {
            return;
        }
        const ms = clientXToMs(e.clientX);
        if (ms == null) {
            return;
        }
        e.preventDefault();
        const next = { startMs: ms, endMs: ms };
        dragRef.current = next;
        setDrag(next);
        (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    };

    const onPointerMove = (e: React.PointerEvent) => {
        if (!dragRef.current) {
            return;
        }
        const ms = clientXToMs(e.clientX);
        if (ms == null) {
            return;
        }
        const next = { startMs: dragRef.current.startMs, endMs: ms };
        dragRef.current = next;
        setDrag(next);
    };

    const endDrag = (e: React.PointerEvent) => {
        const live = dragRef.current;
        dragRef.current = null;
        if (live) {
            const lo = Math.min(live.startMs, live.endMs);
            const hi = Math.max(live.startMs, live.endMs);
            const minSpan = Math.max(MIN_DRAG_ZOOM_MS, spanMs * 0.015);
            if (hi - lo >= minSpan) {
                const nextMin = Math.max(0, lo);
                const nextMax = Math.min(inferredDuration, hi);
                if (nextMax - nextMin >= minSpan) {
                    setZoom({ min: nextMin, max: nextMax });
                }
            }
            setDrag(null);
        }
        try {
            (e.currentTarget as HTMLElement).releasePointerCapture(e.pointerId);
        } catch {
            /* ignore */
        }
    };

    let selectionOverlay: { left: number; width: number; top: number; height: number } | null = null;
    if (drag && dragZoomActive && wrapRef.current) {
        const rect = wrapRef.current.getBoundingClientRect();
        const margin = { ...CHART_MARGIN, ...passthroughLine?.margin };
        const plotW = rect.width - margin.left - margin.right;
        const plotH = rect.height - margin.top - margin.bottom;
        if (plotW > 0 && plotH > 0) {
            const map = (ms: number) => margin.left + ((ms - viewMin) / (viewMax - viewMin)) * plotW;
            const px1 = map(Math.min(drag.startMs, drag.endMs));
            const px2 = map(Math.max(drag.startMs, drag.endMs));
            selectionOverlay = {
                left: Math.min(px1, px2),
                width: Math.max(2, Math.abs(px2 - px1)),
                top: margin.top,
                height: plotH,
            };
        }
    }

    const ticks = useMemo(() => buildMsTicks(viewMin, viewMax, spanMs), [viewMin, viewMax, spanMs]);

    const mergedXAxis = useMemo(
        () =>
            dragZoomActive
                ? {
                      ...passthroughXAxis,
                      type: "number" as const,
                      domain: [viewMin, viewMax] as [number, number],
                      allowDecimals: true,
                      ticks,
                      tickFormatter: (v: number) => formatAxisTickMs(v, spanMs),
                  }
                : { ...passthroughXAxis },
        [dragZoomActive, passthroughXAxis, spanMs, ticks, viewMax, viewMin],
    );

    const mergedTooltip = useMemo(
        () => ({
            ...passthroughTooltip,
            labelFormatter: (label: any, payload: readonly any[]) => {
                const row = payload?.[0]?.payload as Record<string, unknown> | undefined;
                const ms = row?.[timeMsKey];
                if (typeof ms === "number") {
                    return formatTooltipTimeMs(ms);
                }
                return passthroughTooltip?.labelFormatter
                    ? passthroughTooltip.labelFormatter(label, payload)
                    : String(label);
            },
        }),
        [passthroughTooltip, timeMsKey],
    );

    const mergedLine = useMemo(
        () => ({
            ...passthroughLine,
            margin: { ...CHART_MARGIN, ...passthroughLine?.margin },
        }),
        [passthroughLine],
    );

    const mergedChildren = mergeLineChartChildren(userChildren, null);

    const displaySeries =
        series.length > 1 && isolatedName !== null ? series.filter((s) => s.name === isolatedName) : series;

    const chart = (
        <Box
            ref={wrapRef}
            pos="relative"
            style={{
                userSelect: drag ? "none" : undefined,
                touchAction: drag ? "none" : dragZoomActive ? "pan-y" : undefined,
                cursor: dragZoomActive ? (drag ? "grabbing" : "crosshair") : undefined,
            }}
            onPointerDown={dragZoomActive ? onPointerDown : undefined}
            onPointerMove={dragZoomActive ? onPointerMove : undefined}
            onPointerUp={dragZoomActive ? endDrag : undefined}
            onPointerCancel={dragZoomActive ? endDrag : undefined}>
            <LineChart
                series={displaySeries}
                withLegend={false}
                data={displayData}
                dataKey={dataKey}
                {...lineChartRest}
                xAxisProps={mergedXAxis}
                lineChartProps={mergedLine}
                tooltipProps={mergedTooltip}>
                {mergedChildren}
            </LineChart>

            {selectionOverlay && (
                <Box
                    pos="absolute"
                    left={selectionOverlay.left}
                    top={selectionOverlay.top}
                    w={selectionOverlay.width}
                    h={selectionOverlay.height}
                    style={{
                        pointerEvents: "none",
                        background: "rgba(120, 190, 255, 0.18)",
                        border: "1px solid rgba(160, 210, 255, 0.45)",
                        borderRadius: 2,
                        zIndex: 6,
                    }}
                />
            )}

            {dragZoomActive && zoom && (
                <Button
                    type="button"
                    size="xs"
                    variant="light"
                    radius="sm"
                    pos="absolute"
                    top={8}
                    right={8}
                    style={{ zIndex: 10 }}
                    leftSection={<IconZoomReset />}
                    onPointerDown={(e) => e.stopPropagation()}
                    onClick={() => setZoom(null)}>
                    Reset zoom
                </Button>
            )}
        </Box>
    );

    const zoomHint =
        dragZoomActive && !zoom ? (
            <Text size="xs" c="dimmed" ta="center" style={{ lineHeight: 1.35 }}>
                Click and drag on the chart to zoom in
            </Text>
        ) : null;

    const multiSeries = series.length > 1;

    const toggle = (name: string) => {
        if (!multiSeries) {
            return;
        }
        setIsolatedName((prev) => (prev === name ? null : name));
    };

    return (
        <Stack gap="sm">
            <Stack gap={4}>
                {chart}
                {zoomHint}
            </Stack>
            <Group
                gap="xs"
                wrap="wrap"
                justify="flex-end"
                align="flex-start"
                w="100%"
                style={{ overflow: "visible" }}>
                {series.map((s) => {
                    const pressed = isolatedName === s.name;
                    const swatchColor =
                        typeof s.color === "string" && !s.color.startsWith("var(")
                            ? s.color
                            : getThemeColor(s.color ?? "blue", theme);

                    return (
                        <Button
                            key={s.name}
                            type="button"
                            variant={multiSeries && pressed ? "light" : "subtle"}
                            color="gray"
                            size="compact-sm"
                            justify="flex-start"
                            onClick={() => toggle(s.name)}
                            aria-pressed={multiSeries ? pressed : undefined}
                            leftSection={
                                <ColorSwatch
                                    color={swatchColor}
                                    size={12}
                                    withShadow={false}
                                    style={{ flexShrink: 0, marginTop: 2 }}
                                />
                            }
                            styles={{
                                root: {
                                    opacity: multiSeries && isolatedName !== null && !pressed ? 0.55 : 1,
                                    transition: "opacity 120ms ease",
                                    height: "auto",
                                    minHeight: "unset",
                                    maxWidth: "100%",
                                    alignItems: "flex-start",
                                    paddingTop: theme.spacing.xs,
                                    paddingBottom: theme.spacing.xs,
                                },
                                inner: {
                                    flexWrap: "wrap",
                                    alignItems: "flex-start",
                                    justifyContent: "flex-start",
                                },
                                label: {
                                    whiteSpace: "normal",
                                    wordBreak: "break-word",
                                    lineHeight: 1.35,
                                    textAlign: "left",
                                },
                            }}>
                            {s.label ?? s.name}
                        </Button>
                    );
                })}
            </Group>
        </Stack>
    );
}
