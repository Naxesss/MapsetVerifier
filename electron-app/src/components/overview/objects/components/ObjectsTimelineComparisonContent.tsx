import { DndContext, type DragEndEvent, type DragStartEvent , SensorDescriptor, SensorOptions } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { ActionIcon, Box, Group, Paper, Select, Slider, Stack, Text } from '@mantine/core';
import { IconEye, IconEyeOff, IconMinus, IconPlus } from '@tabler/icons-react';
import { LABEL_WIDTH } from '../constants.ts';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';
import {
  DEFAULT_HITSOUND_LAYERS,
  type HitsoundLayerVisibility,
  type TimelineViewMode,
} from '../hitsoundUtils.ts';
import {
  parseTimelineThemeVariant,
  TIMELINE_THEME_VARIANT_OPTIONS,
} from '../timelineTheme/selection.ts';
import { getDifficultyKey } from '../timelineUtils.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { TimelineThemeVariant } from '../timelineTheme/types.ts';
import type { ObjectsModeGroup } from '../types.ts';
import type { RefObject } from 'react';

export type ObjectsTimelineComparisonContentProps = {
  startTimeMs: number;
  endTimeMs: number;
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  orderedDifficulties: ObjectsOverviewDifficulty[];
  visibilityByDifficulty: Record<string, boolean | undefined>;
  toggleDifficultyVisibility: (difficulty: ObjectsOverviewDifficulty) => void;
  setManyVisible: (difficulties: ObjectsOverviewDifficulty[], visible: boolean) => void;
  timelineWidth: number;
  tickIntervalMs: number;
  zoom: number;
  setZoom: (value: number) => void;
  adjustZoom: (delta: -1 | 1) => void;
  formatZoomLabel: (value: number) => string;
  timelineThemeVariant: TimelineThemeVariant;
  onTimelineThemeVariantChange: (variant: TimelineThemeVariant) => void;
  scrollRef: RefObject<HTMLDivElement | null>;
  isPanningTimeline: boolean;
  handleMouseDown: (event: React.MouseEvent<HTMLDivElement>) => void;
  handleMouseMove: (event: React.MouseEvent<HTMLDivElement>) => void;
  stopDragging: () => void;
  dndContextProps: {
    sensors: SensorDescriptor<SensorOptions>[];
    collisionDetection: typeof import('@dnd-kit/core').closestCenter;
    autoScroll: boolean | { layoutShiftCompensation?: boolean };
    measuring: import('@dnd-kit/core').MeasuringConfiguration;
    onDragStart: (event: DragStartEvent) => void;
    onDragEnd: (event: DragEndEvent) => void;
    onDragCancel: () => void;
  };
  rowHeight: number;
  viewMode?: TimelineViewMode;
  hitsoundLayers?: HitsoundLayerVisibility;
  showModeSelector?: boolean;
  showVisibilityControls?: boolean;
  showThemeControls?: boolean;
  showZoomControls?: boolean;
  headerExtra?: React.ReactNode;
  aboveTimelineExtra?: React.ReactNode;
  playheadViewportX?: number | null;
};

export default function ObjectsTimelineComparisonContent({
  startTimeMs,
  endTimeMs,
  groupedDifficulties,
  selectedMode,
  onModeChange,
  orderedDifficulties,
  visibilityByDifficulty,
  toggleDifficultyVisibility,
  setManyVisible,
  timelineWidth,
  tickIntervalMs,
  zoom,
  setZoom,
  adjustZoom,
  formatZoomLabel,
  timelineThemeVariant,
  onTimelineThemeVariantChange,
  scrollRef,
  isPanningTimeline,
  handleMouseDown,
  handleMouseMove,
  stopDragging,
  dndContextProps,
  rowHeight,
  viewMode = 'structure',
  hitsoundLayers = DEFAULT_HITSOUND_LAYERS,
  showModeSelector = true,
  showVisibilityControls = true,
  showThemeControls = true,
  showZoomControls = true,
  headerExtra,
  aboveTimelineExtra,
  playheadViewportX = null,
}: ObjectsTimelineComparisonContentProps) {
  const contentWidth = timelineWidth + LABEL_WIDTH;

  const visibleCount = orderedDifficulties.filter(
    (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
  ).length;
  const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
  const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

  const setSelectedDifficultyVisibility = (visible: boolean) => {
    setManyVisible(orderedDifficulties, visible);
  };

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
              onChange={(value) =>
                onTimelineThemeVariantChange(parseTimelineThemeVariant(value))
              }
            />
          )}
          {showZoomControls && (
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
          )}
        </Group>
      )}

      {aboveTimelineExtra}

      <Box pos="relative">
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
            sensors={dndContextProps.sensors}
            collisionDetection={dndContextProps.collisionDetection}
            autoScroll={dndContextProps.autoScroll}
            measuring={dndContextProps.measuring}
            onDragStart={dndContextProps.onDragStart}
            onDragEnd={dndContextProps.onDragEnd}
            onDragCancel={dndContextProps.onDragCancel}
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
                    visualThemeVariant={timelineThemeVariant}
                    rowHeight={rowHeight}
                    viewMode={viewMode}
                    hitsoundLayers={hitsoundLayers}
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
      </Box>
    </Stack>
  );
}
