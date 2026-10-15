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
import { useMemo } from 'react';
import { LABEL_WIDTH, TIMELINE_VIEW_MODE_TRANSITION_MS } from '../constants.ts';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';
import TimelineHorizontalReveal from './TimelineHorizontalReveal.tsx';
import TimelineShiftSeekModeBadge from './TimelineShiftSeekModeBadge.tsx';
import TimelineZoomControls from './TimelineZoomControls.tsx';
import TimelineZoomModeBadge from './TimelineZoomModeBadge.tsx';
import {
  useTimelineController,
  useTimelineDisplay,
  useTimelinePan,
  useTimelineScale,
  useTimelineViewport,
} from '../context/ObjectsTimelineContext.tsx';
import { usePreserveTimelineScrollOnZoom } from '../hooks/usePreserveTimelineScrollOnZoom.ts';
import { useTimelineModifierKeys } from '../hooks/useTimelineModifierKeys.ts';
import { useTimelineScrollTickStep } from '../hooks/useTimelineScrollTickStep.ts';
import { useTimelineWheelSeek } from '../hooks/useTimelineWheelSeek.ts';
import {
  parseTimelineThemeVariant,
  TIMELINE_THEME_VARIANT_OPTIONS,
} from '../timelineTheme/selection.ts';
import {
  buildAllRoundedEdgeTimes,
  buildTimelineSnapTicks,
  getDifficultyKey,
  getTimelineTimeFromX,
} from '../timelineUtils.ts';

export type ObjectsTimelineComparisonContentProps = {
  showScrollModeControls?: boolean;
  scrollModeExtra?: React.ReactNode;
  showModeSelector?: boolean;
  showVisibilityControls?: boolean;
  showThemeControls?: boolean;
  showZoomControls?: boolean;
  headerExtra?: React.ReactNode;
  aboveTimelineExtra?: React.ReactNode;
};

export default function ObjectsTimelineComparisonContent({
  showScrollModeControls = true,
  scrollModeExtra,
  showModeSelector = true,
  showVisibilityControls = true,
  showThemeControls = true,
  showZoomControls = true,
  headerExtra,
  aboveTimelineExtra,
}: ObjectsTimelineComparisonContentProps) {
  const controller = useTimelineController();
  const { scrollRef, isPanningTimeline, handleMouseDown, handleMouseMove, stopDragging } =
    useTimelinePan();
  const { startTimeMs, endTimeMs, timelineWidth } = useTimelineScale();

  const {
    mode: { groupedDifficulties, selectedMode, onModeChange },
    rows: { orderedDifficulties },
    visibility: { visibilityByDifficulty, setManyVisible },
    display: { timelineThemeVariant, setTimelineThemeVariant },
    zoom: { adjustZoom },
    dnd,
  } = controller;

  const { viewMode } = useTimelineDisplay();

  const { tickStep, setTickStep } = useTimelineScrollTickStep();

  const viewport = useTimelineViewport();

  const snapTicksWindow = useMemo(() => {
    const durationMs = Math.max(1, endTimeMs - startTimeMs);
    const clampedStartX = Math.max(0, viewport.startX);
    const clampedEndX = Math.min(timelineWidth, viewport.endX);
    if (clampedEndX <= clampedStartX) {
      return { windowStartMs: startTimeMs, windowEndMs: endTimeMs };
    }
    return {
      windowStartMs: getTimelineTimeFromX(clampedStartX, startTimeMs, durationMs, timelineWidth),
      windowEndMs: getTimelineTimeFromX(clampedEndX, startTimeMs, durationMs, timelineWidth),
    };
  }, [endTimeMs, startTimeMs, timelineWidth, viewport.endX, viewport.startX]);

  const visibleDifficultiesForSnapTicks = useMemo(
    () =>
      orderedDifficulties.filter(
        (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
      ),
    [orderedDifficulties, visibilityByDifficulty]
  );

  // O(objects) — independent of scroll position, so kept out of the (scroll-bound) snapTicks memo.
  const roundedEdgeTimes = useMemo(
    () => buildAllRoundedEdgeTimes(visibleDifficultiesForSnapTicks),
    [visibleDifficultiesForSnapTicks]
  );

  const snapTicks = useMemo(
    () =>
      buildTimelineSnapTicks(
        visibleDifficultiesForSnapTicks,
        roundedEdgeTimes,
        snapTicksWindow.windowStartMs,
        snapTicksWindow.windowEndMs
      ),
    [visibleDifficultiesForSnapTicks, roundedEdgeTimes, snapTicksWindow]
  );

  useTimelineWheelSeek({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    snapTicks,
    snapClampStartMs: snapTicksWindow.windowStartMs,
    snapClampEndMs: snapTicksWindow.windowEndMs,
    tickStepCount: tickStep,
    adjustZoom,
  });

  usePreserveTimelineScrollOnZoom({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
  });

  const contentWidth = timelineWidth + LABEL_WIDTH;

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
    showModeSelector ||
    showVisibilityControls ||
    showThemeControls ||
    showZoomControls ||
    !!headerExtra;

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
              {showScrollModeControls && (
                <>
                  <TimelineZoomModeBadge active={zoomModeActive} />
                  <TimelineShiftSeekModeBadge
                    active={scrollModeActive}
                    tickStep={tickStep}
                    onTickStepChange={setTickStep}
                  />
                </>
              )}
              {scrollModeExtra}
            </Group>
          )}
          {hasRightHeaderControls && (
            <Group gap="sm" align="center" wrap="wrap" justify="flex-end" ml="auto">
              {headerExtra}
              {showModeSelector && (
                <ObjectsGameModeSelector
                  groupedDifficulties={groupedDifficulties}
                  selectedMode={selectedMode}
                  onModeChange={onModeChange}
                />
              )}
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

      <Box>
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
              display: 'flex',
              width: contentWidth,
              minWidth: contentWidth,
            }}
          >
            <Stack
              gap="xs"
              style={{ width: contentWidth, minWidth: contentWidth, flex: '0 0 auto' }}
            >
              <TimelineAxisRow linePosition="bottom" />

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

              {orderedDifficulties.length > 0 && <TimelineAxisRow linePosition="top" />}
            </Stack>
          </Box>
        </Box>
      </Box>
    </Stack>
  );
}
