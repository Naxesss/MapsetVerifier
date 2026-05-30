import { DndContext } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { ActionIcon, Box, Group, Paper, Select, Stack, Text } from '@mantine/core';
import { IconEye, IconEyeOff } from '@tabler/icons-react';
import { useMemo } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';
import TimelineShiftSeekModeBadge from './TimelineShiftSeekModeBadge.tsx';
import TimelineZoomControls from './TimelineZoomControls.tsx';
import {
  useTimelineController,
  useTimelineDisplay,
  useTimelinePan,
  useTimelineScale,
} from '../context/ObjectsTimelineContext.tsx';
import { usePlayheadScrollPadding } from '../hooks/usePlayheadScrollPadding.ts';
import { usePreserveTimelineScrollOnZoom } from '../hooks/usePreserveTimelineScrollOnZoom.ts';
import { useShiftKeyHeld } from '../hooks/useShiftKeyHeld.ts';
import { useTimelineWheelSeek } from '../hooks/useTimelineWheelSeek.ts';
import {
  parseTimelineThemeVariant,
  TIMELINE_THEME_VARIANT_OPTIONS,
} from '../timelineTheme/selection.ts';
import { buildTimelineSnapTicks, getDifficultyKey } from '../timelineUtils.ts';

export type ObjectsTimelineComparisonContentProps = {
  showModeSelector?: boolean;
  showVisibilityControls?: boolean;
  showThemeControls?: boolean;
  showZoomControls?: boolean;
  headerExtra?: React.ReactNode;
  aboveTimelineExtra?: React.ReactNode;
};

export default function ObjectsTimelineComparisonContent({
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
  const { viewMode, playheadViewportX } = useTimelineDisplay();

  const {
    mode: { groupedDifficulties, selectedMode, onModeChange },
    rows: { orderedDifficulties },
    visibility: { visibilityByDifficulty, setManyVisible },
    display: { timelineThemeVariant, setTimelineThemeVariant },
    dnd,
  } = controller;

  const playheadScrollPadding = usePlayheadScrollPadding(scrollRef, playheadViewportX, {
    timelineWidth,
    startTimeMs,
    endTimeMs,
  });

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
    playheadViewportX,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    snapTicks,
  });

  usePreserveTimelineScrollOnZoom({
    scrollRef,
    timelineWidth,
    startTimeMs,
    endTimeMs,
    anchorViewportX: playheadViewportX,
  });

  const contentWidth = timelineWidth + LABEL_WIDTH;
  const scrollContentWidth =
    contentWidth + playheadScrollPadding.padLeft + playheadScrollPadding.padRight;

  const visibleCount = orderedDifficulties.filter(
    (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
  ).length;
  const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
  const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

  const setSelectedDifficultyVisibility = (visible: boolean) => {
    setManyVisible(orderedDifficulties, visible);
  };

  const shiftHeld = useShiftKeyHeld();

  return (
    <Stack gap="md">
      {(showModeSelector ||
        showVisibilityControls ||
        showThemeControls ||
        showZoomControls ||
        headerExtra) && (
        <Group gap="sm" align="center" wrap="wrap" justify="flex-end">
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
              <ActionIcon
                variant="default"
                aria-label="Show all difficulties"
                disabled={orderedDifficulties.length === 0 || allVisible}
                onClick={() => setSelectedDifficultyVisibility(true)}
              >
                <IconEye size={16} />
              </ActionIcon>
              <ActionIcon
                variant="default"
                aria-label="Hide all difficulties"
                disabled={orderedDifficulties.length === 0 || allHidden}
                onClick={() => setSelectedDifficultyVisibility(false)}
              >
                <IconEyeOff size={16} />
              </ActionIcon>
            </Group>
          )}
          {showThemeControls && viewMode === 'structure' && (
            <Select
              aria-label="Timeline object style"
              title="Timeline object style"
              size="xs"
              w={108}
              data={TIMELINE_THEME_VARIANT_OPTIONS}
              value={timelineThemeVariant}
              allowDeselect={false}
              comboboxProps={{ withinPortal: true }}
              onChange={(value) => setTimelineThemeVariant(parseTimelineThemeVariant(value))}
            />
          )}
          {showZoomControls && <TimelineZoomControls />}
        </Group>
      )}

      {aboveTimelineExtra}

      <Box pos="relative">
        <TimelineShiftSeekModeBadge visible={shiftHeld} />
        {playheadViewportX !== null && (
          <Box
            style={{
              pointerEvents: 'none',
              position: 'absolute',
              left: playheadViewportX,
              top: 0,
              bottom: 0,
              width: 0,
              borderLeft: '2px solid var(--mantine-color-blue-4)',
              boxShadow: '0 0 12px rgba(56, 189, 248, 0.35)',
              zIndex: 20,
            }}
          />
        )}
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
              width: scrollContentWidth,
              minWidth: scrollContentWidth,
            }}
          >
            {playheadScrollPadding.padLeft > 0 && (
              <Box
                style={{
                  flex: `0 0 ${playheadScrollPadding.padLeft}px`,
                  width: playheadScrollPadding.padLeft,
                }}
              />
            )}
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
            {playheadScrollPadding.padRight > 0 && (
              <Box
                style={{
                  flex: `0 0 ${playheadScrollPadding.padRight}px`,
                  width: playheadScrollPadding.padRight,
                }}
              />
            )}
          </Box>
        </Box>
      </Box>
    </Stack>
  );
}
