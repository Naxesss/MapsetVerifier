import { DndContext } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import {
  ActionIcon,
  Box,
  Collapse,
  Group,
  Paper,
  Select,
  Stack,
  Text,
  Tooltip,
} from '@mantine/core';
import { IconEye, IconEyeOff } from '@tabler/icons-react';
import { useCallback, useMemo } from 'react';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';
import TimelineHorizontalReveal from './TimelineHorizontalReveal.tsx';
import TimelinePlaybackControls from './TimelinePlaybackControls.tsx';
import TimelinePlaybackGuard from './TimelinePlaybackGuard.tsx';
import TimelineZoomControls from './TimelineZoomControls.tsx';
import { TIMELINE_VIEW_MODE_TRANSITION_MS } from '../constants.ts';
import TimelineShortcutHints from './TimelineShortcutHints.tsx';
import {
  useTimelineController,
  useTimelineDisplay,
  useTimelineEdges,
  useTimelinePan,
  useTimelineScale,
} from '../context/ObjectsTimelineContext.tsx';
import { usePreserveTimelineScrollOnZoom } from '../hooks/usePreserveTimelineScrollOnZoom.ts';
import { useTimelineModifierKeys } from '../hooks/useTimelineModifierKeys.ts';
import { useTimelinePlayback } from '../hooks/useTimelinePlayback.ts';
import { useTimelineScrollTickStep } from '../hooks/useTimelineScrollTickStep.ts';
import { readTimelineOverscanWindowMs } from '../hooks/useTimelineViewportRange.ts';
import { useTimelineWheelSeek } from '../hooks/useTimelineWheelSeek.ts';
import {
  parseTimelineThemeVariant,
  TIMELINE_THEME_VARIANT_OPTIONS,
} from '../timelineTheme/selection.ts';
import {
  buildAllRoundedEdgeTimes,
  buildTimelineSnapTicks,
  getAdjacentTimingSnapTickBySteps,
  getDifficultyKey,
} from '../timelineUtils.ts';

export type ObjectsTimelineComparisonContentProps = {
  showScrollModeControls?: boolean;
  scrollModeExtra?: React.ReactNode;
  showVisibilityControls?: boolean;
  showThemeControls?: boolean;
  showZoomControls?: boolean;
  headerExtra?: React.ReactNode;
  aboveTimelineExtra?: React.ReactNode;
  audioUrl?: string;
};

export default function ObjectsTimelineComparisonContent({
  showScrollModeControls = true,
  scrollModeExtra,
  showVisibilityControls = true,
  showThemeControls = true,
  showZoomControls = true,
  headerExtra,
  aboveTimelineExtra,
  audioUrl,
}: ObjectsTimelineComparisonContentProps) {
  const controller = useTimelineController();
  const { scrollRef, isPanningTimeline, handleMouseDown, handleMouseMove, stopDragging } =
    useTimelinePan();
  const { startTimeMs, endTimeMs, timelineWidth } = useTimelineScale();
  const { originX, endPaddingPx } = useTimelineEdges();

  const {
    rows: { orderedDifficulties },
    visibility: { visibilityByDifficulty, setManyVisible },
    display: { timelineThemeVariant, setTimelineThemeVariant },
    zoom: { adjustZoom },
    dnd,
  } = controller;

  const { viewMode } = useTimelineDisplay();

  const { tickStep, setTickStep } = useTimelineScrollTickStep();

  const visibleDifficultiesForSnapTicks = useMemo(
    () =>
      orderedDifficulties.filter(
        (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
      ),
    [orderedDifficulties, visibilityByDifficulty]
  );

  // O(objects) — independent of scroll. Snap ticks are built from this at seek time.
  const roundedEdgeTimes = useMemo(
    () => buildAllRoundedEdgeTimes(visibleDifficultiesForSnapTicks),
    [visibleDifficultiesForSnapTicks]
  );

  const resolveSnapTicks = useCallback(() => {
    const scrollElement = scrollRef.current;
    const snapWindow = scrollElement
      ? readTimelineOverscanWindowMs(scrollElement, timelineWidth, startTimeMs, endTimeMs, originX)
      : { windowStartMs: startTimeMs, windowEndMs: endTimeMs };

    return {
      snapTicks: buildTimelineSnapTicks(
        visibleDifficultiesForSnapTicks,
        roundedEdgeTimes,
        snapWindow.windowStartMs,
        snapWindow.windowEndMs
      ),
      snapClampStartMs: snapWindow.windowStartMs,
      snapClampEndMs: snapWindow.windowEndMs,
    };
  }, [
    endTimeMs,
    originX,
    roundedEdgeTimes,
    scrollRef,
    startTimeMs,
    timelineWidth,
    visibleDifficultiesForSnapTicks,
  ]);

  useTimelineWheelSeek({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    originX,
    resolveSnapTicks,
    tickStepCount: tickStep,
    adjustZoom,
  });

  usePreserveTimelineScrollOnZoom({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    originX,
  });

  const playback = useTimelinePlayback({
    audioUrl,
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    originX,
    endPaddingPx,
    stopPanning: stopDragging,
  });
  const { cursorRef, getTimeMs, isPlaying, isUnavailable, seek, setVolume, toggle } = playback;

  // Steps from the cursor rather than the view's centre, which differ near the ends of the map.
  const stepPlayback = useCallback(
    (direction: 1 | -1) => {
      const { snapTicks, snapClampStartMs, snapClampEndMs } = resolveSnapTicks();
      if (snapTicks.length > 0) {
        seek(
          getAdjacentTimingSnapTickBySteps(
            snapTicks,
            getTimeMs(),
            direction,
            tickStep,
            snapClampStartMs,
            snapClampEndMs
          )
        );
      }
    },
    [getTimeMs, resolveSnapTicks, seek, tickStep]
  );

  const contentWidth = originX + timelineWidth + endPaddingPx;

  const visibleCount = orderedDifficulties.filter(
    (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
  ).length;
  const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
  const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

  const setSelectedDifficultyVisibility = (visible: boolean) => {
    setManyVisible(orderedDifficulties, visible);
  };

  const { shiftHeld, ctrlHeld } = useTimelineModifierKeys();
  const scrollModeActive = shiftHeld && !ctrlHeld;
  const zoomModeActive = ctrlHeld && !shiftHeld;

  const hasRightHeaderControls =
    showVisibilityControls || showThemeControls || showZoomControls || !!headerExtra;

  const showHeaderRow = showScrollModeControls || !!scrollModeExtra || hasRightHeaderControls;

  return (
    <Stack gap="md">
      {showHeaderRow && (
        <Group gap="sm" align="center" wrap="wrap" w="100%">
          {(showScrollModeControls || scrollModeExtra) && (
            <Group
              gap="sm"
              wrap="wrap"
              align="center"
              data-stop-timeline-pan="true"
              data-timeline-wheel-ignore="true"
            >
              {audioUrl && (
                <TimelinePlaybackControls
                  isPlaying={isPlaying}
                  isUnavailable={isUnavailable}
                  toggle={toggle}
                  setVolume={setVolume}
                />
              )}
              {showScrollModeControls && (
                <TimelineShortcutHints
                  zoomActive={zoomModeActive}
                  scrollActive={scrollModeActive}
                  tickStep={tickStep}
                  onTickStepChange={setTickStep}
                />
              )}
              {scrollModeExtra}
            </Group>
          )}
          {hasRightHeaderControls && (
            <Group gap="sm" align="center" wrap="wrap" justify="flex-end" ml="auto">
              {headerExtra}
              {showVisibilityControls && (
                <Group gap="xs" align="center" wrap="nowrap">
                  <Tooltip label="Show all difficulties">
                    <ActionIcon
                      variant="default"
                      aria-label="Show all difficulties"
                      disabled={orderedDifficulties.length === 0 || allVisible}
                      onClick={() => setSelectedDifficultyVisibility(true)}
                    >
                      <IconEye size={16} />
                    </ActionIcon>
                  </Tooltip>
                  <Tooltip label="Hide all difficulties">
                    <ActionIcon
                      variant="default"
                      aria-label="Hide all difficulties"
                      disabled={orderedDifficulties.length === 0 || allHidden}
                      onClick={() => setSelectedDifficultyVisibility(false)}
                    >
                      <IconEyeOff size={16} />
                    </ActionIcon>
                  </Tooltip>
                </Group>
              )}
              {showThemeControls && (
                <TimelineHorizontalReveal
                  visible={viewMode === 'structure'}
                  spacing="var(--mantine-spacing-sm)"
                >
                  <Tooltip label="Timeline object style">
                    <Select
                      aria-label="Timeline object style"
                      size="xs"
                      w={108}
                      data={TIMELINE_THEME_VARIANT_OPTIONS}
                      value={timelineThemeVariant}
                      allowDeselect={false}
                      comboboxProps={{ withinPortal: true }}
                      onChange={(value) =>
                        setTimelineThemeVariant(parseTimelineThemeVariant(value))
                      }
                    />
                  </Tooltip>
                </TimelineHorizontalReveal>
              )}
              {showZoomControls && <TimelineZoomControls />}
            </Group>
          )}
        </Group>
      )}

      {aboveTimelineExtra && (
        <Collapse
          in={viewMode === 'hitsounding'}
          transitionDuration={TIMELINE_VIEW_MODE_TRANSITION_MS}
          animateOpacity
        >
          {aboveTimelineExtra}
        </Collapse>
      )}

      <Box pos="relative">
        {isPlaying && <TimelinePlaybackGuard adjustZoom={adjustZoom} onStep={stepPlayback} />}
        <Box
          ref={scrollRef}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={stopDragging}
          onMouseLeave={stopDragging}
          style={{
            overflowX: 'auto',
            overflowY: 'hidden',
            cursor: isPanningTimeline ? 'grabbing' : 'grab',
            userSelect: 'none',
          }}
        >
          <Box
            style={{
              position: 'relative',
              display: 'flex',
              width: contentWidth,
              minWidth: contentWidth,
            }}
          >
            {audioUrl && (
              <Box
                ref={cursorRef}
                aria-hidden
                style={{
                  position: 'absolute',
                  top: 0,
                  bottom: 0,
                  // Centred one pixel in, where the border of the rows puts their objects.
                  left: 0,
                  width: 2,
                  // Above the sticky labels, as the start of the timeline sits right on their edge.
                  zIndex: 4,
                  background: 'var(--mantine-color-primary-3)',
                  boxShadow:
                    '0 0 3px color-mix(in srgb, var(--mantine-color-primary-4) 40%, transparent)',
                  pointerEvents: 'none',
                  willChange: 'transform',
                }}
              />
            )}
            <Stack
              gap="xs"
              style={{ width: contentWidth, minWidth: contentWidth, flex: '0 0 auto' }}
            >
              <TimelineAxisRow linePosition="bottom" onSeek={audioUrl ? seek : undefined} />

              {orderedDifficulties.length === 0 && (
                <Paper p="md" radius="md" withBorder>
                  <Text size="sm" c="dimmed">
                    No difficulties available for the selected mode.
                  </Text>
                </Paper>
              )}

              <DndContext
                sensors={dnd.sensors}
                collisionDetection={dnd.collisionDetection}
                autoScroll={dnd.autoScroll}
                measuring={dnd.measuring}
                onDragStart={dnd.onDragStart}
                onDragEnd={dnd.onDragEnd}
                onDragCancel={dnd.onDragCancel}
              >
                <SortableContext
                  items={orderedDifficulties.map(getDifficultyKey)}
                  strategy={verticalListSortingStrategy}
                >
                  {orderedDifficulties.map((difficulty) => {
                    const difficultyKey = getDifficultyKey(difficulty);
                    const isVisible = visibilityByDifficulty[difficultyKey] !== false;

                    return (
                      <SortableTimelineDifficultyRow
                        key={difficultyKey}
                        difficulty={difficulty}
                        difficultyKey={difficultyKey}
                        isVisible={isVisible}
                        contentWidth={contentWidth}
                      />
                    );
                  })}
                </SortableContext>
              </DndContext>

              {orderedDifficulties.length > 0 && (
                <TimelineAxisRow linePosition="top" onSeek={audioUrl ? seek : undefined} />
              )}
            </Stack>
          </Box>
        </Box>
      </Box>
    </Stack>
  );
}
