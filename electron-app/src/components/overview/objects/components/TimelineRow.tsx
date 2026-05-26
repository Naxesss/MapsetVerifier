import { Box, useMantineTheme } from '@mantine/core';
import { memo, useCallback, useMemo, useState, type MouseEvent as ReactMouseEvent } from 'react';
import TimelineObjectContextMenu from './TimelineObjectContextMenu.tsx';
import TimelineObjectHeadHovercard from './TimelineObjectHeadHovercard.tsx';
import AutoResizeCanvas from '../../../common/AutoResizeCanvas.tsx';
import {
  useTimelineDisplay,
  useTimelinePan,
  useTimelineScale,
} from '../context/ObjectsTimelineContext.tsx';
import {
  buildHitsoundDrawCache,
  type HitsoundDrawCache,
  type HitsoundLayerVisibility,
  type TimelineViewMode,
} from '../hitsoundUtils.ts';
import {
  drawTimelineRow,
  findTimelineObjectHeadAtX,
  getTimelineTimestampAtX,
  type TimelineObjectHeadHit,
} from '../timelineDrawing.ts';
import { formatEditorTimestamp, getTimelineCanvasTiles, getTimelineX } from '../timelineUtils.ts';
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
  hitsoundDrawCache?: HitsoundDrawCache;
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
  hitsoundDrawCache,
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
        hitsoundDrawCache,
      });
    },
    [
      difficulty,
      endTimeMs,
      height,
      hitsoundDrawCache,
      hitsoundLayers,
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
        hitsoundDrawCache,
        hitsoundLayers,
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
      hitsoundDrawCache,
      hitsoundLayers,
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
  const { timelineThemeVariant, viewMode, hitsoundLayers, snapPlayheadToTimestamp } =
    useTimelineDisplay();
  const canvasTiles = useMemo(() => getTimelineCanvasTiles(timelineWidth), [timelineWidth]);
  const hitsoundDrawCache = useMemo(() => {
    if (viewMode !== 'hitsounding') {
      return undefined;
    }

    return buildHitsoundDrawCache(
      difficulty.timelineObjects,
      difficulty.timelineSamples ?? []
    );
  }, [difficulty.timelineObjects, difficulty.timelineSamples, viewMode]);
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

  const handleMouseDown = (event: ReactMouseEvent<HTMLDivElement>) => {
    if (event.detail >= 2) {
      event.stopPropagation();
    }
  };

  const handleDoubleClick = (event: ReactMouseEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();

    if (viewMode !== 'hitsounding' || !snapPlayheadToTimestamp) {
      return;
    }

    const bounds = event.currentTarget.getBoundingClientRect();
    const localX = event.clientX - bounds.left;
    const headHit = findTimelineObjectHeadAtX({
      difficulty,
      startTimeMs,
      endTimeMs,
      timelineWidth,
      x: localX,
      visualThemeVariant: timelineThemeVariant,
    });

    if (headHit) {
      snapPlayheadToTimestamp(headHit.timeMs);
      return;
    }

    const timestampMs = getTimelineTimestampAtX({
      difficulty,
      startTimeMs,
      endTimeMs,
      timelineWidth,
      x: localX,
      visualThemeVariant: timelineThemeVariant,
    });

    if (timestampMs !== null) {
      snapPlayheadToTimestamp(timestampMs);
    }
  };

  const selectedTimestampX = contextMenuState
    ? getTimelineX(
        contextMenuState.timestampMs,
        startTimeMs,
        Math.max(1, endTimeMs - startTimeMs),
        timelineWidth
      )
    : undefined;

  return (
    <Box
      onContextMenu={handleContextMenu}
      onMouseDown={handleMouseDown}
      onDoubleClick={handleDoubleClick}
      onMouseMove={updateHeadHover}
      onMouseLeave={clearHeadHover}
      style={{ position: 'relative', width: timelineWidth, height, overflow: 'hidden' }}
    >
      {canvasTiles.map((tile) => (
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
          hitsoundDrawCache={hitsoundDrawCache}
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
              borderLeft: `2px solid ${theme.colors.blue[4]}`,
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
      />
    </Box>
  );
}

export default memo(TimelineRow);
