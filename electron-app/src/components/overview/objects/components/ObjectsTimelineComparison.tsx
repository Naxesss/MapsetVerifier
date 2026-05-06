import { DndContext } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { ActionIcon, Box, Button, Group, Paper, Slider, Stack, Text, Title } from '@mantine/core';
import { IconEye, IconEyeOff, IconMinus, IconPlus } from '@tabler/icons-react';
import { LABEL_WIDTH } from '../constants.ts';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';
import { useDifficultyRowDnd } from '../hooks/useDifficultyRowDnd.ts';
import { useDifficultyRowVisibility } from '../hooks/useDifficultyRowVisibility.ts';
import { useHorizontalScrollPan } from '../hooks/useHorizontalScrollPan.ts';
import { usePerModeDifficultyOrder } from '../hooks/usePerModeDifficultyOrder.ts';
import { useTimelineZoom } from '../hooks/useTimelineZoom.ts';
import { getDifficultyKey } from '../timelineUtils.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

interface ObjectsTimelineComparisonProps {
  startTimeMs: number;
  endTimeMs: number;
  groupedDifficulties: ObjectsModeGroup[];
  difficulties: ObjectsOverviewDifficulty[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}

export default function ObjectsTimelineComparison({
  startTimeMs,
  endTimeMs,
  groupedDifficulties,
  difficulties,
  selectedMode,
  onModeChange,
}: ObjectsTimelineComparisonProps) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const { zoom, setZoom, tickIntervalMs, timelineWidth, adjustZoom, formatZoomLabel } =
    useTimelineZoom(durationMs);
  const { scrollRef, isPanningTimeline, handleMouseDown, handleMouseMove, stopDragging } =
    useHorizontalScrollPan();
  const { visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible } =
    useDifficultyRowVisibility(groupedDifficulties);
  const contentWidth = timelineWidth + LABEL_WIDTH;
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
  const { orderedDifficulties, moveDifficulty } = usePerModeDifficultyOrder({
    groupedDifficulties,
    activeMode,
    difficulties,
  });
  const {
    sensors,
    collisionDetection,
    autoScroll,
    measuring,
    handleDragStart,
    handleDragEnd,
    handleDragCancel,
  } = useDifficultyRowDnd({
    moveDifficulty,
    stopPanning: stopDragging,
  });

  const visibleCount = orderedDifficulties.filter(
    (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
  ).length;
  const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
  const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

  const setSelectedDifficultyVisibility = (visible: boolean) => {
    setManyVisible(orderedDifficulties, visible);
  };

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Timeline comparison</Title>
            <Text size="sm" c="dimmed">
              Drag the grip to reorder rows. Drag horizontally in the timeline to pan.
            </Text>
          </Stack>
          <Group gap="sm" align="center" wrap="wrap" justify="flex-end">
            <ObjectsGameModeSelector
              groupedDifficulties={groupedDifficulties}
              selectedMode={selectedMode}
              onModeChange={onModeChange}
            />
            <Group gap="xs" align="center" wrap="nowrap">
              <Button
                variant="default"
                size="xs"
                leftSection={<IconEye size={14} />}
                disabled={orderedDifficulties.length === 0 || allVisible}
                onClick={() => setSelectedDifficultyVisibility(true)}
              >
                Show all
              </Button>
              <Button
                variant="default"
                size="xs"
                leftSection={<IconEyeOff size={14} />}
                disabled={orderedDifficulties.length === 0 || allHidden}
                onClick={() => setSelectedDifficultyVisibility(false)}
              >
                Hide all
              </Button>
            </Group>
            <Group gap="xs" align="center" wrap="nowrap">
              <ActionIcon variant="default" onClick={() => adjustZoom(-1)}>
                <IconMinus size={16} />
              </ActionIcon>
              <Box miw={220}>
                <Slider
                  value={zoom}
                  min={1}
                  max={24}
                  step={1.0}
                  onChange={setZoom}
                  label={(value) => `${formatZoomLabel(value)}x`}
                />
              </Box>
              <ActionIcon variant="default" onClick={() => adjustZoom(1)}>
                <IconPlus size={16} />
              </ActionIcon>
            </Group>
          </Group>
        </Group>
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
          <Stack gap="xs" style={{ width: contentWidth, minWidth: contentWidth }}>
            <TimelineAxisRow
              startTimeMs={startTimeMs}
              endTimeMs={endTimeMs}
              timelineWidth={timelineWidth}
              tickIntervalMs={tickIntervalMs}
              linePosition="bottom"
            />

            {orderedDifficulties.length === 0 && (
              <Paper p="md" radius="md" withBorder>
                <Text size="sm" c="dimmed">
                  No difficulties available for the selected mode.
                </Text>
              </Paper>
            )}

            <DndContext
              sensors={sensors}
              collisionDetection={collisionDetection}
              autoScroll={autoScroll}
              measuring={measuring}
              onDragStart={handleDragStart}
              onDragEnd={handleDragEnd}
              onDragCancel={handleDragCancel}
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
                      timelineWidth={timelineWidth}
                      contentWidth={contentWidth}
                      startTimeMs={startTimeMs}
                      endTimeMs={endTimeMs}
                      onToggleVisibility={toggleDifficultyVisibility}
                    />
                  );
                })}
              </SortableContext>
            </DndContext>

            {orderedDifficulties.length > 0 && (
              <TimelineAxisRow
                startTimeMs={startTimeMs}
                endTimeMs={endTimeMs}
                timelineWidth={timelineWidth}
                tickIntervalMs={tickIntervalMs}
                linePosition="top"
              />
            )}
          </Stack>
        </Box>
      </Stack>
    </Paper>
  );
}
