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
import {
  useTimelineController,
  useTimelineDisplay,
  useTimelinePan,
  useTimelineScale,
} from '../context/ObjectsTimelineContext.tsx';
import { usePreserveTimelineScrollOnZoom } from '../hooks/usePreserveTimelineScrollOnZoom.ts';
import { useShiftKeyHeld } from '../hooks/useShiftKeyHeld.ts';
import { useTimelineScrollTickStep } from '../hooks/useTimelineScrollTickStep.ts';
import { useTimelineWheelSeek } from '../hooks/useTimelineWheelSeek.ts';
import {
  parseTimelineThemeVariant,
  TIMELINE_THEME_VARIANT_OPTIONS,
} from '../timelineTheme/selection.ts';
import { buildTimelineSnapTicks, getDifficultyKey } from '../timelineUtils.ts';

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
    dnd,
  } = controller;

  const { viewMode } = useTimelineDisplay();

  const { tickStep, setTickStep } = useTimelineScrollTickStep();

  const snapTicks = useMemo(
    () =>
      buildTimelineSnapTicks(
        orderedDifficulties.filter(
          (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
        ),
        startTimeMs,
        endTimeMs
      ),
    [endTimeMs, orderedDifficulties, startTimeMs, visibilityByDifficulty]
  );

  useTimelineWheelSeek({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    snapTicks,
    tickStepCount: tickStep,
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

  const shiftHeld = useShiftKeyHeld();

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
              wrap="nowrap"
              align="center"
              data-stop-timeline-pan="true"
              data-timeline-wheel-ignore="true"
            >
              {showScrollModeControls && (
                <TimelineShiftSeekModeBadge
                  active={shiftHeld}
                  tickStep={tickStep}
                  onTickStepChange={setTickStep}
                />
              )}
              {scrollModeExtra}
            </Group>
          )}
          {hasRightHeaderControls && (
            <Group gap={0} align="center" wrap="nowrap" justify="flex-end" ml="auto">
              {headerExtra}
              {showModeSelector && (
                <Box ml="sm">
                  <ObjectsGameModeSelector
                    groupedDifficulties={groupedDifficulties}
                    selectedMode={selectedMode}
                    onModeChange={onModeChange}
                  />
                </Box>
              )}
              {showVisibilityControls && (
                <Group gap="xs" align="center" wrap="nowrap" ml="sm">
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
              {showZoomControls && (
                <Box ml="sm">
                  <TimelineZoomControls />
                </Box>
              )}
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
