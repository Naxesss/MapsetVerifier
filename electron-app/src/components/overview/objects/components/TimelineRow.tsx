import { Box, useMantineTheme } from '@mantine/core';
import { memo, useCallback, useMemo, useState, type MouseEvent as ReactMouseEvent } from 'react';
import TimelineObjectContextMenu from './TimelineObjectContextMenu.tsx';
import TimelineObjectHeadHovercard from './TimelineObjectHeadHovercard.tsx';
import AutoResizeCanvas from '../../../common/AutoResizeCanvas.tsx';
import {
  TIMELINE_VIEW_MODE_TRANSITION_EASING,
  TIMELINE_VIEW_MODE_TRANSITION_MS,
} from '../constants.ts';
import {
  useTimelineDisplay,
  useTimelinePan,
  useTimelineScale,
  useTimelineViewport,
} from '../context/ObjectsTimelineContext.tsx';
import { type HitsoundLayerVisibility, type TimelineViewMode } from '../hitsoundUtils.ts';
import {
  drawTimelineRow,
  findTimelineObjectHeadAtX,
  getTimelineTimestampAtX,
  type TimelineObjectHeadHit,
} from '../timelineDrawing.ts';
import {
  buildTimelineRowDrawCache,
  formatEditorTimestamp,
  getTimelineCanvasTiles,
  getTimelineHighlightX,
  type TimelineRowDrawCache,
} from '../timelineUtils.ts';
import type { ObjectsOverviewDifficulty } from '../../../../Types';
import type { TimelineThemeVariant } from '../timelineTheme/types.ts';
import type { MantineTheme } from '@mantine/core';

interface TimelineRowProps {
  difficulty: ObjectsOverviewDifficulty;
  height: number;
}

type TimelineCanvasTileProps = {
  tile: { startX: number; width: number };
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  height: number;
  theme: MantineTheme;
  visualThemeVariant: TimelineThemeVariant;
  viewMode: TimelineViewMode;
  hitsoundLayers?: HitsoundLayerVisibility;
  rowDrawCache: TimelineRowDrawCache;
};

const TimelineCanvasTile = memo(function TimelineCanvasTile({
  tile,
  difficulty,
  startTimeMs,
  endTimeMs,
  width,
  height,
  theme,
  visualThemeVariant,
  viewMode,
  hitsoundLayers,
  rowDrawCache,
}: TimelineCanvasTileProps) {
  const draw = useCallback(
    (ctx: CanvasRenderingContext2D) => {
      drawTimelineRow(ctx, {
        difficulty,
        startTimeMs,
        endTimeMs,
        timelineWidth: width,
        viewportStartX: tile.startX,
        viewportWidth: tile.width,
        height,
        theme,
        visualThemeVariant,
        viewMode,
        hitsoundLayers,
        rowDrawCache,
      });
    },
    [
      difficulty,
      endTimeMs,
      height,
      hitsoundLayers,
      rowDrawCache,
      startTimeMs,
      theme,
      tile.startX,
      tile.width,
      viewMode,
      visualThemeVariant,
      width,
    ]
  );

  const redrawDeps = useMemo(
    () =>
      [
        difficulty,
        endTimeMs,
        height,
        hitsoundLayers,
        rowDrawCache,
        startTimeMs,
        theme,
        tile.startX,
        tile.width,
        viewMode,
        visualThemeVariant,
        width,
      ] as const,
    [
      difficulty,
      endTimeMs,
      height,
      hitsoundLayers,
      rowDrawCache,
      startTimeMs,
      theme,
      tile.startX,
      tile.width,
      viewMode,
      visualThemeVariant,
      width,
    ]
  );

  return (
    <Box
      style={{
        position: 'absolute',
        left: tile.startX,
        top: 0,
        width: tile.width,
        height,
      }}
    >
      <AutoResizeCanvas
        fixedWidth={tile.width}
        fixedHeight={height}
        draw={draw}
        redrawDeps={redrawDeps}
      />
    </Box>
  );
});

function TimelineRow({ difficulty, height }: TimelineRowProps) {
  const theme = useMantineTheme();
  const { startTimeMs, endTimeMs, timelineWidth } = useTimelineScale();
  const { isPanningTimeline } = useTimelinePan();
  const { timelineThemeVariant, viewMode, hitsoundLayers } = useTimelineDisplay();
  const viewport = useTimelineViewport();
  const canvasTiles = useMemo(() => getTimelineCanvasTiles(timelineWidth), [timelineWidth]);
  const visibleCanvasTiles = useMemo(
    () =>
      canvasTiles.filter(
        (tile) => tile.startX + tile.width > viewport.startX && tile.startX < viewport.endX
      ),
    [canvasTiles, viewport.startX, viewport.endX]
  );
  const rowDrawCache = useMemo(
    () =>
      buildTimelineRowDrawCache(
        difficulty.timelineObjects,
        difficulty.timelineSamples,
        viewMode === 'hitsounding'
      ),
    [difficulty.timelineObjects, difficulty.timelineSamples, viewMode]
  );
  const [contextMenuState, setContextMenuState] = useState<{
    localX: number;
    localY: number;
    timestampMs: number;
  } | null>(null);
  const [headHover, setHeadHover] = useState<TimelineObjectHeadHit | null>(null);

  const updateHeadHover = useCallback(
    (event: ReactMouseEvent<HTMLDivElement>) => {
      if (isPanningTimeline || contextMenuState) {
        setHeadHover(null);
        return;
      }

      const bounds = event.currentTarget.getBoundingClientRect();
      const localX = event.clientX - bounds.left;
      const hit = findTimelineObjectHeadAtX({
        difficulty,
        startTimeMs,
        endTimeMs,
        timelineWidth,
        x: localX,
        visualThemeVariant: timelineThemeVariant,
      });
      setHeadHover(hit);
    },
    [
      contextMenuState,
      difficulty,
      endTimeMs,
      isPanningTimeline,
      startTimeMs,
      timelineThemeVariant,
      timelineWidth,
    ]
  );

  const clearHeadHover = useCallback(() => {
    setHeadHover(null);
  }, []);

  const handleContextMenu = (event: React.MouseEvent<HTMLDivElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    const localX = event.clientX - bounds.left;
    const timestampMs = getTimelineTimestampAtX({
      difficulty,
      startTimeMs,
      endTimeMs,
      timelineWidth,
      x: localX,
      visualThemeVariant: timelineThemeVariant,
    });

    if (timestampMs === null) {
      setContextMenuState(null);
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    setContextMenuState({
      localX,
      localY: event.clientY - bounds.top,
      timestampMs,
    });
  };

  const copyEditorTimestamp = async () => {
    if (!contextMenuState) return;
    const timestamp = formatEditorTimestamp(contextMenuState.timestampMs);
    await navigator.clipboard.writeText(timestamp);
    setContextMenuState(null);
  };

  const goToObject = () => {
    if (!contextMenuState) return;
    const timestamp = formatEditorTimestamp(contextMenuState.timestampMs);
    window.location.href = `osu://edit/${timestamp}`;
    setContextMenuState(null);
  };

  const selectedTimestampX = contextMenuState
    ? getTimelineHighlightX(
        contextMenuState.timestampMs,
        difficulty,
        startTimeMs,
        endTimeMs,
        timelineWidth
      )
    : undefined;

  return (
    <Box
      onContextMenu={handleContextMenu}
      onMouseMove={updateHeadHover}
      onMouseLeave={clearHeadHover}
      style={{
        position: 'relative',
        width: timelineWidth,
        height,
        overflow: 'hidden',
        transition: `height ${TIMELINE_VIEW_MODE_TRANSITION_MS}ms ${TIMELINE_VIEW_MODE_TRANSITION_EASING}`,
      }}
    >
      {visibleCanvasTiles.map((tile) => (
        <TimelineCanvasTile
          key={tile.startX}
          tile={tile}
          difficulty={difficulty}
          startTimeMs={startTimeMs}
          endTimeMs={endTimeMs}
          width={timelineWidth}
          height={height}
          theme={theme}
          visualThemeVariant={timelineThemeVariant}
          viewMode={viewMode}
          hitsoundLayers={hitsoundLayers}
          rowDrawCache={rowDrawCache}
        />
      ))}
      {contextMenuState && (
        <>
          <Box
            style={{
              pointerEvents: 'none',
              position: 'absolute',
              left: selectedTimestampX ?? undefined,
              top: 0,
              bottom: 0,
              width: 0,
              transform: 'translateX(-50%)',
              borderLeft: `1px solid ${theme.colors.blue[4]}`,
              boxShadow: `0 0 0 1px ${theme.colors.blue[3]}, 0 0 12px ${theme.colors.blue[7]}`,
              zIndex: 8,
            }}
          />
          <Box
            style={{
              pointerEvents: 'none',
              position: 'absolute',
              left: selectedTimestampX ?? undefined,
              top: '50%',
              width: 12,
              height: 12,
              borderRadius: '50%',
              transform: 'translate(-50%, -50%)',
              border: `2px solid ${theme.colors.blue[1]}`,
              background: theme.colors.blue[6],
              boxShadow: `0 0 0 2px ${theme.colors.blue[8]}, 0 0 14px ${theme.colors.blue[7]}`,
              zIndex: 9,
            }}
          />
        </>
      )}
      <TimelineObjectContextMenu
        opened={contextMenuState !== null}
        anchorX={contextMenuState?.localX ?? null}
        anchorY={contextMenuState?.localY ?? null}
        onClose={() => setContextMenuState(null)}
        onGoToTimestamp={goToObject}
        onCopyTimestamp={copyEditorTimestamp}
        goToLabel="Go to object"
        timestampLabel={formatEditorTimestamp(contextMenuState?.timestampMs ?? 0)}
      />
      <TimelineObjectHeadHovercard
        difficulty={difficulty}
        hover={headHover}
        opened={headHover !== null && !isPanningTimeline && contextMenuState === null}
        anchorY={height / 2}
        viewMode={viewMode}
        hitsoundLayers={hitsoundLayers}
      />
    </Box>
  );
}

export default memo(TimelineRow);
