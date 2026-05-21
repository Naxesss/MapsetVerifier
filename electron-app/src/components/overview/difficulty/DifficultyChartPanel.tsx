import { Box, Stack, Text } from '@mantine/core';
import { useCallback, useLayoutEffect, useRef, useState } from 'react';
import { formatAxisMetricValue } from '../../charts/timeSeries/sampleFormat.ts';
import TimeSeriesChart from '../../charts/timeSeries/TimeSeriesChart.tsx';
import TimeSeriesHoverTooltip from '../../charts/timeSeries/TimeSeriesHoverTooltip.tsx';
import TimeSeriesLegend from '../../charts/timeSeries/TimeSeriesLegend.tsx';
import { TimestampContextMenu } from '../../charts/timeSeries/TimestampContextMenu.tsx';
import { formatEditorTimestamp } from '../objects/timelineUtils.ts';
import type { DifficultyChartState } from './hooks/useDifficultyChartState.ts';
import type {
  ChartHoverPayload,
  ChartInterpolation,
  SeriesConfig,
  TimeSeriesRow,
} from '../../charts/timeSeries/types.ts';

const INLINE_PLOT_HEIGHT = 200;
const MODAL_PLOT_HEIGHT = 480;
const TOOLTIP_EDGE_PADDING = 8;

export type DifficultyChartPanelProps = {
  data: TimeSeriesRow[];
  series: SeriesConfig[];
  durationMs: number;
  valueSuffix: string | undefined;
  interpolation?: ChartInterpolation;
  showDataPoints?: boolean;
  plotHeight?: number;
  chartState: DifficultyChartState;
  hover: ChartHoverPayload | null;
  onHover: (hover: ChartHoverPayload | null) => void;
  showZoomHint?: boolean;
  /** Inline card: crosshair tooltip above plot. Full view uses ChartHoverFloatingPanel instead. */
  showInlineHoverTooltip?: boolean;
};

export function DifficultyChartPanel({
  data,
  series,
  durationMs,
  valueSuffix,
  interpolation = 'line',
  showDataPoints = false,
  plotHeight = INLINE_PLOT_HEIGHT,
  chartState,
  hover,
  onHover,
  showZoomHint = true,
  showInlineHoverTooltip = true,
}: DifficultyChartPanelProps) {
  const chartAreaRef = useRef<HTMLDivElement>(null);
  const tooltipMeasureRef = useRef<HTMLDivElement>(null);
  const [tooltipLeft, setTooltipLeft] = useState(0);
  const {
    viewMin,
    viewMax,
    spanMs,
    applyDragZoom,
    visibleSeriesIds,
    isVisible,
    toggleSeries,
    setAllVisible,
    isZoomed,
    resetZoom,
  } = chartState;

  const [contextMenu, setContextMenu] = useState<{
    anchorX: number;
    anchorY: number;
    timeMs: number;
    timestampLabel: string;
  } | null>(null);

  const axisValueFormatter = useCallback(
    (value: number) => formatAxisMetricValue(value, valueSuffix),
    [valueSuffix]
  );

  const handleContextMenuPeak = useCallback((payload: ChartHoverPayload) => {
    const { peak, anchor } = payload;
    setContextMenu({
      anchorX: anchor.x,
      anchorY: anchor.y,
      timeMs: peak.timeMs,
      timestampLabel: formatEditorTimestamp(peak.timeMs),
    });
  }, []);

  const copyTimestamp = async () => {
    if (!contextMenu) return;
    await navigator.clipboard.writeText(formatEditorTimestamp(contextMenu.timeMs));
    setContextMenu(null);
  };

  const goToTimestamp = () => {
    if (!contextMenu) return;
    window.location.href = `osu://edit/${formatEditorTimestamp(contextMenu.timeMs)}`;
    setContextMenu(null);
  };

  useLayoutEffect(() => {
    if (!hover || !showInlineHoverTooltip) {
      return;
    }

    const containerWidth = chartAreaRef.current?.clientWidth ?? 0;
    const tooltipWidth = tooltipMeasureRef.current?.offsetWidth ?? 0;
    const anchorX = hover.tooltipAnchor.x;

    if (containerWidth <= 0 || tooltipWidth <= 0) {
      setTooltipLeft(anchorX);
      return;
    }

    const half = tooltipWidth / 2;
    const minLeft = half + TOOLTIP_EDGE_PADDING;
    const maxLeft = containerWidth - half - TOOLTIP_EDGE_PADDING;
    setTooltipLeft(Math.min(Math.max(anchorX, minLeft), maxLeft));
  }, [hover, showInlineHoverTooltip, valueSuffix]);

  if (data.length === 0) {
    return (
      <Text c="dimmed" ta="center" py="xl">
        No chart data available
      </Text>
    );
  }

  return (
    <Stack gap="sm">
      <Box
        ref={chartAreaRef}
        pos="relative"
        w="100%"
        style={{ overflow: 'visible' }}
        onMouseDown={showInlineHoverTooltip ? undefined : (event) => event.stopPropagation()}
      >
        {hover && showInlineHoverTooltip ? (
          <Box
            ref={tooltipMeasureRef}
            pos="absolute"
            left={tooltipLeft}
            top={hover.tooltipAnchor.y}
            style={{
              transform: 'translate(-50%, calc(-100% - 6px))',
              zIndex: 15,
              pointerEvents: 'none',
              width: 'max-content',
              maxWidth: 360,
            }}
          >
            <TimeSeriesHoverTooltip hover={hover.peak} valueSuffix={valueSuffix} />
          </Box>
        ) : null}
        <TimeSeriesChart
          data={data}
          series={series}
          viewMin={viewMin}
          viewMax={viewMax}
          spanMs={spanMs}
          durationMs={durationMs}
          plotHeight={plotHeight}
          visibleSeriesIds={visibleSeriesIds}
          valueFormatter={axisValueFormatter}
          interpolation={interpolation}
          showDataPoints={showDataPoints}
          hover={hover?.peak ?? null}
          onHover={onHover}
          onDragZoom={applyDragZoom}
          onContextMenuPeak={handleContextMenuPeak}
          onPlotPointerDown={() => setContextMenu(null)}
          isZoomed={isZoomed}
          onResetZoom={resetZoom}
          retainHoverOnLeave={!showInlineHoverTooltip}
        />
        <TimestampContextMenu
          opened={contextMenu !== null}
          anchorX={contextMenu?.anchorX ?? null}
          anchorY={contextMenu?.anchorY ?? null}
          timestampLabel={contextMenu?.timestampLabel}
          onClose={() => setContextMenu(null)}
          onGoToTimestamp={goToTimestamp}
          onCopyTimestamp={copyTimestamp}
          goToLabel="Go to timestamp"
          withinPortal={false}
        />
      </Box>
      {showZoomHint && !isZoomed ? (
        <Text size="xs" c="dimmed" ta="center" style={{ lineHeight: 1.35 }}>
          Click and drag on the chart to zoom in. Right-click a peak for timestamp actions.
        </Text>
      ) : null}
      <TimeSeriesLegend
        series={series}
        isVisible={isVisible}
        onToggle={toggleSeries}
        onSelectAll={() => setAllVisible(true)}
        onUnselectAll={() => setAllVisible(false)}
      />
    </Stack>
  );
}

export { INLINE_PLOT_HEIGHT, MODAL_PLOT_HEIGHT };
