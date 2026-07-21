import { Box, Button, useMantineTheme } from '@mantine/core';
import { useResizeObserver } from '@mantine/hooks';
import { IconZoomReset } from '@tabler/icons-react';
import { AxisBottom, AxisLeft, AxisRight } from '@visx/axis';
import { curveStepAfter } from '@visx/curve';
import { GridColumns, GridRows } from '@visx/grid';
import { Group } from '@visx/group';
import { scaleLinear } from '@visx/scale';
import { LinePath } from '@visx/shape';
import { memo, useCallback, useMemo, useRef, useState, type PointerEvent } from 'react';
import { buildMsTicks, formatAxisTickMs } from './axisTicks.ts';
import { downsampleRows } from './downsampleSeries.ts';
import { filterRowsInViewport, resolvePeakFromPointer } from './peakHitTest.ts';
import { clientXToDomainMs, queryPlotRect } from './plotGeometry.ts';
import type {
  ChartInterpolation,
  DragSelection,
  ChartHoverPayload,
  PeakHoverState,
  SeriesConfig,
  TimeSeriesRow,
} from './types.ts';

const MARGIN = { top: 8, right: 16, bottom: 32, left: 52 };
const MARGIN_WITH_SECONDARY_AXIS = { ...MARGIN, right: 48 };
const MAX_FULL_RES_RENDER_ROWS = 1000;
const MAX_PEAK_DOTS = 100;

type TimeSeriesChartProps = {
  data: TimeSeriesRow[];
  series: SeriesConfig[];
  viewMin: number;
  viewMax: number;
  spanMs: number;
  durationMs: number;
  plotHeight: number;
  visibleSeriesIds: Set<string>;
  valueFormatter: (value: number) => string;
  interpolation?: ChartInterpolation;
  /** Draw circles at each sample when the viewport has at most MAX_PEAK_DOTS rows. */
  showDataPoints?: boolean;
  /** 'zeroBased' (default) always anchors the axis at 0. 'fitToData' zooms to the visible value
   *  range instead, for charts whose values stay within a narrow band near the top of the scale. */
  yAxisMode?: 'zeroBased' | 'fitToData';
  /** Formats ticks for the secondary (right) axis, used by series with `useSecondaryAxis`. */
  secondaryValueFormatter?: (value: number) => string;
  hover: PeakHoverState | null;
  onHover: (payload: ChartHoverPayload | null) => void;
  onDragZoom: (startMs: number, endMs: number) => void;
  onContextMenuPeak: (payload: ChartHoverPayload) => void;
  isZoomed?: boolean;
  onResetZoom?: () => void;
  /** Keep hover when the pointer leaves the plot (e.g. moving to a floating tooltip). */
  retainHoverOnLeave?: boolean;
  /** Fired on left-button pointer down on the plot (used to dismiss overlays). */
  onPlotPointerDown?: () => void;
};

type LinePoint = { timeMs: number; value: number };

function TimeSeriesChartInner({
  data,
  series,
  viewMin,
  viewMax,
  spanMs,
  plotHeight,
  visibleSeriesIds,
  valueFormatter,
  interpolation = 'line',
  showDataPoints = false,
  yAxisMode = 'zeroBased',
  secondaryValueFormatter,
  hover,
  onHover,
  onDragZoom,
  onContextMenuPeak,
  isZoomed = false,
  onResetZoom,
  retainHoverOnLeave = false,
  onPlotPointerDown,
  width,
}: TimeSeriesChartProps & { width: number }) {
  const lineCurve = interpolation === 'step' ? curveStepAfter : undefined;
  const theme = useMantineTheme();
  const wrapRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<DragSelection | null>(null);
  const [drag, setDrag] = useState<DragSelection | null>(null);

  const hasPrimaryAxis = series.some((item) => !item.useSecondaryAxis);
  const hasSecondaryAxis = series.some((item) => item.useSecondaryAxis);
  const margin = hasSecondaryAxis ? MARGIN_WITH_SECONDARY_AXIS : MARGIN;

  const innerWidth = Math.max(0, width - margin.left - margin.right);
  const innerHeight = Math.max(0, plotHeight - margin.top - margin.bottom);

  const viewportRows = useMemo(
    () => filterRowsInViewport(data, viewMin, viewMax),
    [data, viewMax, viewMin]
  );

  const renderRows = useMemo(
    () =>
      viewportRows.length <= MAX_FULL_RES_RENDER_ROWS ? viewportRows : downsampleRows(viewportRows),
    [viewportRows]
  );

  const buildSeriesPoints = useCallback(
    (rows: TimeSeriesRow[]) => {
      const paths: {
        id: string;
        color: string;
        dashed?: boolean;
        useSecondaryAxis?: boolean;
        points: LinePoint[];
      }[] = [];

      for (const item of series) {
        if (!visibleSeriesIds.has(item.visibilityId ?? item.id)) {
          continue;
        }

        const points: LinePoint[] = [];
        for (const row of rows) {
          const value = row[item.key];
          if (typeof value === 'number') {
            points.push({ timeMs: row.timeMs, value });
          }
        }

        if (points.length > 0) {
          paths.push({
            id: item.id,
            color: item.color,
            dashed: item.dashed,
            useSecondaryAxis: item.useSecondaryAxis,
            points,
          });
        }
      }

      return paths;
    },
    [series, visibleSeriesIds]
  );

  const seriesPaths = useMemo(() => buildSeriesPoints(renderRows), [buildSeriesPoints, renderRows]);
  const primaryPaths = useMemo(
    () => seriesPaths.filter((path) => !path.useSecondaryAxis),
    [seriesPaths]
  );
  const secondaryPaths = useMemo(
    () => seriesPaths.filter((path) => path.useSecondaryAxis),
    [seriesPaths]
  );

  const showDots = showDataPoints && viewportRows.length <= MAX_PEAK_DOTS;
  const dotPaths = useMemo(
    () => (showDots ? buildSeriesPoints(viewportRows) : []),
    [buildSeriesPoints, showDots, viewportRows]
  );

  function computeYDomain(
    paths: { points: LinePoint[] }[],
    mode: 'zeroBased' | 'fitToData'
  ): [number, number] {
    let min = Infinity;
    let max = 0;
    for (const path of paths) {
      for (const point of path.points) {
        if (point.value > max) max = point.value;
        if (point.value < min) min = point.value;
      }
    }

    if (mode !== 'fitToData' || !Number.isFinite(min)) {
      return [0, max > 0 ? max * 1.05 : 1];
    }

    const range = max - min;
    const padding = range > 0 ? range * 0.1 : Math.max(max * 0.1, 1);
    return [Math.max(0, min - padding), max + padding];
  }

  const [yMin, yMax] = useMemo(
    () => computeYDomain(primaryPaths, yAxisMode),
    [primaryPaths, yAxisMode]
  );

  // The secondary axis (e.g. raw strain) is always zoomed to its own data, same as the
  // standalone skill-strain charts elsewhere in the app.
  const [secondaryYMin, secondaryYMax] = useMemo(
    () => computeYDomain(secondaryPaths, 'fitToData'),
    [secondaryPaths]
  );

  const xScale = useMemo(
    () =>
      scaleLinear<number>({
        domain: [viewMin, viewMax],
        range: [0, innerWidth],
        clamp: true,
      }),
    [innerWidth, viewMax, viewMin]
  );

  const yScale = useMemo(
    () =>
      scaleLinear<number>({
        domain: [yMin, yMax],
        range: [innerHeight, 0],
        nice: true,
      }),
    [innerHeight, yMax, yMin]
  );

  const secondaryYScale = useMemo(
    () =>
      scaleLinear<number>({
        domain: [secondaryYMin, secondaryYMax],
        range: [innerHeight, 0],
        nice: true,
      }),
    [innerHeight, secondaryYMax, secondaryYMin]
  );

  const xTicks = useMemo(() => buildMsTicks(viewMin, viewMax, spanMs), [spanMs, viewMax, viewMin]);

  const getPlotRect = useCallback(() => {
    const root = wrapRef.current;
    if (!root) {
      return null;
    }
    return queryPlotRect(root);
  }, []);

  const buildHoverPayload = useCallback(
    (peak: PeakHoverState, _clientX: number, clientY: number): ChartHoverPayload => {
      const plotX = xScale(peak.timeMs) ?? 0;
      const bounds = wrapRef.current?.getBoundingClientRect();
      const cursorY = bounds ? clientY - bounds.top : margin.top;

      return {
        peak,
        anchor: {
          x: margin.left + plotX,
          y: cursorY,
        },
        tooltipAnchor: {
          x: margin.left + plotX,
          y: margin.top,
        },
        cursorY,
      };
    },
    [innerHeight, xScale]
  );

  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (dragRef.current) {
      const plotRect = getPlotRect();
      if (!plotRect) {
        return;
      }
      const ms = clientXToDomainMs(event.clientX, plotRect, viewMin, viewMax);
      const next = { startMs: dragRef.current.startMs, endMs: ms };
      dragRef.current = next;
      setDrag(next);
      return;
    }

    const plotRect = getPlotRect();
    if (!plotRect) {
      onHover(null);
      return;
    }

    const peak = resolvePeakFromPointer(
      viewportRows,
      event.clientX,
      plotRect,
      viewMin,
      viewMax,
      series,
      visibleSeriesIds
    );
    onHover(peak ? buildHoverPayload(peak, event.clientX, event.clientY) : null);
  };

  const handlePointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) {
      return;
    }
    const target = event.target as Element;
    if (target.closest('button,a,input,textarea,[role="slider"]')) {
      return;
    }

    onPlotPointerDown?.();

    const plotRect = getPlotRect();
    if (!plotRect) {
      return;
    }

    event.preventDefault();
    const ms = clientXToDomainMs(event.clientX, plotRect, viewMin, viewMax);
    const next = { startMs: ms, endMs: ms };
    dragRef.current = next;
    setDrag(next);
    event.currentTarget.setPointerCapture(event.pointerId);
  };

  const endDrag = (event: PointerEvent<HTMLDivElement>) => {
    const live = dragRef.current;
    dragRef.current = null;
    if (live) {
      onDragZoom(live.startMs, live.endMs);
      setDrag(null);
    }
    try {
      event.currentTarget.releasePointerCapture(event.pointerId);
    } catch {
      /* ignore */
    }
  };

  const handleContextMenu = (event: PointerEvent<HTMLDivElement>) => {
    const plotRect = getPlotRect();
    if (!plotRect) {
      return;
    }

    const peak = resolvePeakFromPointer(
      viewportRows,
      event.clientX,
      plotRect,
      viewMin,
      viewMax,
      series,
      visibleSeriesIds
    );
    if (!peak) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    onContextMenuPeak(buildHoverPayload(peak, event.clientX, event.clientY));
  };

  const crosshairPlotX = hover && innerWidth > 0 ? xScale(hover.timeMs) : null;

  const dragX1 = drag ? xScale(Math.min(drag.startMs, drag.endMs)) : null;
  const dragX2 = drag ? xScale(Math.max(drag.startMs, drag.endMs)) : null;

  const gridStroke = theme.colors.dark[4];
  const axisColor = theme.colors.dark[2];
  const plotBg = 'rgba(0,0,0,0.02)';

  return (
    <Box
      ref={wrapRef}
      pos="relative"
      h={plotHeight}
      w="100%"
      style={{
        userSelect: drag ? 'none' : undefined,
        touchAction: drag ? 'none' : 'pan-y',
        cursor: drag ? 'grabbing' : 'crosshair',
      }}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={endDrag}
      onPointerCancel={endDrag}
      onPointerLeave={() => {
        if (!dragRef.current && !retainHoverOnLeave) {
          onHover(null);
        }
      }}
      onContextMenu={handleContextMenu}
    >
      {isZoomed && onResetZoom ? (
        <Button
          type="button"
          size="xs"
          variant="light"
          radius="sm"
          pos="absolute"
          top={8}
          right={8}
          style={{ zIndex: 12 }}
          leftSection={<IconZoomReset size={14} />}
          onPointerDown={(e) => e.stopPropagation()}
          onClick={(e) => {
            e.stopPropagation();
            onResetZoom();
          }}
        >
          Reset zoom
        </Button>
      ) : null}

      <svg width={width} height={plotHeight} style={{ display: 'block', overflow: 'visible' }}>
        <rect
          data-chart-plot=""
          x={margin.left}
          y={margin.top}
          width={innerWidth}
          height={innerHeight}
          fill={plotBg}
        />
        <Group left={margin.left} top={margin.top} pointerEvents="none">
          <GridRows
            scale={hasPrimaryAxis ? yScale : secondaryYScale}
            width={innerWidth}
            stroke={gridStroke}
            strokeOpacity={0.6}
            numTicks={5}
          />
          <GridColumns
            scale={xScale}
            height={innerHeight}
            stroke={gridStroke}
            strokeOpacity={0.6}
            tickValues={xTicks}
          />
          {seriesPaths.map((path) => (
            <LinePath
              key={path.id}
              data={path.points}
              x={(d) => xScale(d.timeMs) ?? 0}
              y={(d) => (path.useSecondaryAxis ? secondaryYScale(d.value) : yScale(d.value)) ?? 0}
              curve={lineCurve}
              stroke={path.color}
              strokeWidth={2.5}
              strokeDasharray={path.dashed ? '6,4' : undefined}
              fill="none"
            />
          ))}
          {showDots
            ? dotPaths.flatMap((path) =>
                path.points.map((point) => (
                  <circle
                    key={`${path.id}-${point.timeMs}`}
                    cx={xScale(point.timeMs) ?? 0}
                    cy={
                      (path.useSecondaryAxis
                        ? secondaryYScale(point.value)
                        : yScale(point.value)) ?? 0
                    }
                    r={3.5}
                    fill={path.color}
                    stroke={theme.colors.dark[7]}
                    strokeWidth={1}
                  />
                ))
              )
            : null}
          {drag && dragX1 != null && dragX2 != null ? (
            <rect
              x={Math.min(dragX1, dragX2)}
              y={0}
              width={Math.abs(dragX2 - dragX1)}
              height={innerHeight}
              fill="rgba(120, 190, 255, 0.18)"
            />
          ) : null}
          {crosshairPlotX != null ? (
            <line
              x1={crosshairPlotX}
              x2={crosshairPlotX}
              y1={0}
              y2={innerHeight}
              stroke={theme.colors.blue[4]}
              strokeWidth={1.5}
            />
          ) : null}
        </Group>
        {hasPrimaryAxis ? (
          <AxisLeft
            left={margin.left}
            top={margin.top}
            scale={yScale}
            numTicks={5}
            tickFormat={(v) => valueFormatter(Number(v))}
            stroke={axisColor}
            tickStroke={axisColor}
            tickLabelProps={{
              fill: axisColor,
              fontSize: 10,
              fontFamily: theme.fontFamily,
              textAnchor: 'end',
              dx: -4,
            }}
          />
        ) : null}
        {hasSecondaryAxis ? (
          <AxisRight
            left={margin.left + innerWidth}
            top={margin.top}
            scale={secondaryYScale}
            numTicks={5}
            tickFormat={(v) => (secondaryValueFormatter ?? String)(Number(v))}
            stroke={axisColor}
            tickStroke={axisColor}
            tickLabelProps={{
              fill: axisColor,
              fontSize: 10,
              fontFamily: theme.fontFamily,
              textAnchor: 'start',
              dx: 4,
            }}
          />
        ) : null}
        <AxisBottom
          top={margin.top + innerHeight}
          left={margin.left}
          scale={xScale}
          tickValues={xTicks}
          tickFormat={(v) => formatAxisTickMs(Number(v), spanMs)}
          stroke={axisColor}
          tickStroke={axisColor}
          tickLabelProps={{
            fill: axisColor,
            fontSize: 10,
            fontFamily: theme.fontFamily,
            textAnchor: 'middle',
          }}
        />
      </svg>
    </Box>
  );
}

function TimeSeriesChart(props: TimeSeriesChartProps) {
  const [containerRef, rect] = useResizeObserver<HTMLDivElement>();
  const width = rect.width ?? 0;

  return (
    <Box ref={containerRef} w="100%" h={props.plotHeight}>
      {width > 0 ? <TimeSeriesChartInner {...props} width={width} /> : null}
    </Box>
  );
}

export default memo(TimeSeriesChart);
