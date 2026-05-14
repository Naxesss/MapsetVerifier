import { Box, useMantineTheme } from '@mantine/core';
import { useContext, useMemo, useState } from 'react';
import TimelineObjectContextMenu from './TimelineObjectContextMenu.tsx';
import AutoResizeCanvas from '../../../common/AutoResizeCanvas.tsx';
import { TimelineTileVisibilityContext } from '../context/TimelineTileVisibilityContext.tsx';
import { drawTimelineRow, getTimelineTimestampAtX } from '../timelineDrawing.ts';
import { formatEditorTimestamp, getTimelineCanvasTiles, getTimelineX } from '../timelineUtils.ts';
import type { ObjectsOverviewDifficulty } from '../../../../Types';

interface TimelineRowProps {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  height: number;
}

export default function TimelineRow({
  difficulty,
  startTimeMs,
  endTimeMs,
  width,
  height,
}: TimelineRowProps) {
  const theme = useMantineTheme();
  const tileVisibility = useContext(TimelineTileVisibilityContext);
  const canvasTiles = useMemo(() => getTimelineCanvasTiles(width), [width]);

  const canvasVisible = (tileIndex: number) => {
    if (tileVisibility == null) return true;
    const { start, end } = tileVisibility;
    if (end < start) return true;
    return tileIndex >= start && tileIndex <= end;
  };
  const [contextMenuState, setContextMenuState] = useState<{
    localX: number;
    localY: number;
    timestampMs: number;
  } | null>(null);

  const handleContextMenu = (event: React.MouseEvent<HTMLDivElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    const localX = event.clientX - bounds.left;
    const timestampMs = getTimelineTimestampAtX({
      difficulty,
      startTimeMs,
      endTimeMs,
      timelineWidth: width,
      x: localX,
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
    ? getTimelineX(
        contextMenuState.timestampMs,
        startTimeMs,
        Math.max(1, endTimeMs - startTimeMs),
        width
      )
    : undefined;

  return (
    <Box
      onContextMenu={handleContextMenu}
      style={{ position: 'relative', width, height, overflow: 'hidden' }}
    >
      {canvasTiles.map((tile, tileIndex) => (
        <Box
          key={tile.startX}
          style={{
            position: 'absolute',
            left: tile.startX,
            top: 0,
            width: tile.width,
            height,
          }}
        >
          {canvasVisible(tileIndex) ? (
            <AutoResizeCanvas
              fixedWidth={tile.width}
              fixedHeight={height}
              draw={(ctx) => {
                drawTimelineRow(ctx, {
                  difficulty,
                  startTimeMs,
                  endTimeMs,
                  timelineWidth: width,
                  viewportStartX: tile.startX,
                  viewportWidth: tile.width,
                  height,
                  theme,
                });
              }}
            />
          ) : null}
        </Box>
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
        onGoToObject={goToObject}
        onCopyTimestamp={copyEditorTimestamp}
      />
    </Box>
  );
}
