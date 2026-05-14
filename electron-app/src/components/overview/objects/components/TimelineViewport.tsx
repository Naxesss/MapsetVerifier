import { DndContext } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import type {
  CollisionDetection,
  DragEndEvent,
  DragStartEvent,
  MeasuringConfiguration,
  SensorDescriptor,
  SensorOptions,
} from '@dnd-kit/core';
import { Box, Paper, Stack, Text } from '@mantine/core';
import { memo, type MutableRefObject, type MouseEvent as ReactMouseEvent } from 'react';
import type { ObjectsOverviewDifficulty } from '../../../../Types';
import { TimelineTileVisibilityBoundary } from '../context/TimelineTileVisibilityContext.tsx';
import { getDifficultyKey } from '../timelineUtils.ts';
import SortableTimelineDifficultyRow from './SortableTimelineDifficultyRow.tsx';
import TimelineAxisRow from './TimelineAxisRow.tsx';

export interface TimelineViewportProps {
  showCenterPlayheadOverlay: boolean;
  centerPlayheadBackground: string;
  centerPlayheadOpacity: number;
  scrollRef: MutableRefObject<HTMLDivElement | null>;
  onScrollMouseDown: (event: ReactMouseEvent<HTMLDivElement>) => void;
  onScrollMouseMove: (event: ReactMouseEvent<HTMLDivElement>) => void;
  onScrollMouseUp: (event: ReactMouseEvent<HTMLDivElement>) => void;
  onScrollMouseLeave: (event: ReactMouseEvent<HTMLDivElement>) => void;
  isPanningTimeline: boolean;
  /** True while scrub-dragging a seek zone — same grabbing cursor as pan */
  isSeekDragging: boolean;
  contentWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  tickIntervalMs: number;
  orderedDifficulties: ObjectsOverviewDifficulty[];
  visibilityByDifficulty: Record<string, boolean>;
  sensors: SensorDescriptor<SensorOptions>[];
  collisionDetection: CollisionDetection;
  measuring: MeasuringConfiguration;
  onDragStart: (event: DragStartEvent) => void;
  onDragEnd: (event: DragEndEvent) => void;
  onDragCancel: () => void;
  onToggleDifficultyVisibility: (difficulty: ObjectsOverviewDifficulty) => void;
}

function TimelineViewportInner({
  showCenterPlayheadOverlay,
  centerPlayheadBackground,
  centerPlayheadOpacity,
  scrollRef,
  onScrollMouseDown,
  onScrollMouseMove,
  onScrollMouseUp,
  onScrollMouseLeave,
  isPanningTimeline,
  isSeekDragging,
  contentWidth,
  startTimeMs,
  endTimeMs,
  timelineWidth,
  tickIntervalMs,
  orderedDifficulties,
  visibilityByDifficulty,
  sensors,
  collisionDetection,
  measuring,
  onDragStart,
  onDragEnd,
  onDragCancel,
  onToggleDifficultyVisibility,
}: TimelineViewportProps) {
  return (
    <Box style={{ position: 'relative' }}>
      {showCenterPlayheadOverlay ? (
        <Box
          style={{
            position: 'absolute',
            left: '50%',
            top: 0,
            bottom: 0,
            width: 2,
            transform: 'translateX(-50%)',
            background: centerPlayheadBackground,
            opacity: centerPlayheadOpacity,
            pointerEvents: 'none',
            zIndex: 20,
          }}
          aria-hidden
        />
      ) : null}
      <Box
        ref={scrollRef}
        onMouseDown={onScrollMouseDown}
        onMouseMove={onScrollMouseMove}
        onMouseUp={onScrollMouseUp}
        onMouseLeave={onScrollMouseLeave}
        style={{
          overflowX: 'auto',
          overflowY: 'hidden',
          cursor:
            isPanningTimeline || isSeekDragging ? 'grabbing' : 'grab',
          userSelect: 'none',
        }}
      >
        <TimelineTileVisibilityBoundary scrollRef={scrollRef} timelineWidth={timelineWidth}>
          <Stack gap="xs" style={{ width: contentWidth, minWidth: contentWidth, contain: 'paint' }}>
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
              autoScroll={false}
              measuring={measuring}
              onDragStart={onDragStart}
              onDragEnd={onDragEnd}
              onDragCancel={onDragCancel}
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
                      onToggleVisibility={onToggleDifficultyVisibility}
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
        </TimelineTileVisibilityBoundary>
      </Box>
    </Box>
  );
}

export const TimelineViewport = memo(TimelineViewportInner);
