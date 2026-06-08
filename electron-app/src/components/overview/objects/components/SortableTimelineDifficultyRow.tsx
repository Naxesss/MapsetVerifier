import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { ActionIcon, Box, Flex, Group, Stack, Text, useMantineTheme } from '@mantine/core';
import { IconEye, IconEyeOff, IconGripVertical } from '@tabler/icons-react';
import { memo } from 'react';
import TimelineRow from './TimelineRow.tsx';
import { withAlpha } from '../../../../utils/color.ts';
import { formatGameModeLabel, getModeAccentColor, normalizeMode } from '../../../../utils/gameMode';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import {
  HIDDEN_ROW_HEIGHT,
  HIDDEN_ROW_VERTICAL_PADDING,
  LABEL_WIDTH,
  TIMELINE_VIEW_MODE_TRANSITION_EASING,
  TIMELINE_VIEW_MODE_TRANSITION_MS,
} from '../constants.ts';
import {
  useTimelineDisplay,
  useTimelineScale,
  useTimelineVisibility,
} from '../context/ObjectsTimelineContext.tsx';
import type { ObjectsOverviewDifficulty } from '../../../../Types';

interface SortableTimelineDifficultyRowProps {
  difficulty: ObjectsOverviewDifficulty;
  difficultyKey: string;
  isVisible: boolean;
  contentWidth: number;
}

function SortableTimelineDifficultyRow({
  difficulty,
  difficultyKey,
  isVisible,
  contentWidth,
}: SortableTimelineDifficultyRowProps) {
  const theme = useMantineTheme();
  const { timelineWidth } = useTimelineScale();
  const { rowHeight } = useTimelineDisplay();
  const { toggleDifficultyVisibility } = useTimelineVisibility();
  const visibleRowHeight = isVisible ? rowHeight : HIDDEN_ROW_HEIGHT;
  const rowHeightTransition = `height ${TIMELINE_VIEW_MODE_TRANSITION_MS}ms ${TIMELINE_VIEW_MODE_TRANSITION_EASING}`;
  const dropIndicatorColor = theme.colors.blue[4];
  const {
    attributes,
    listeners,
    setNodeRef,
    setActivatorNodeRef,
    transform,
    transition,
    isDragging,
    isOver,
  } = useSortable({
    id: difficultyKey,
    transition: {
      duration: 120,
      easing: 'cubic-bezier(0.2, 0, 0, 1)',
    },
  });

  return (
    <Box
      ref={setNodeRef}
      style={{
        position: 'relative',
        display: 'flex',
        alignItems: 'stretch',
        width: contentWidth,
        minWidth: contentWidth,
        height: visibleRowHeight,
        borderRadius: theme.radius.sm,
        background: isOver ? withAlpha(dropIndicatorColor, 0.08) : undefined,
        opacity: isDragging ? 0.78 : 1,
        zIndex: isDragging ? 10 : undefined,
        transform: CSS.Transform.toString(transform),
        transition: [transition, rowHeightTransition].filter(Boolean).join(', '),
      }}
    >
      <Box
        style={{
          position: 'sticky',
          left: 0,
          zIndex: 2,
          display: 'flex',
          alignItems: 'center',
          flex: `0 0 ${LABEL_WIDTH}px`,
          height: visibleRowHeight,
          paddingInline: theme.spacing.xs,
          background: isVisible ? theme.colors.dark[8] : theme.colors.dark[7],
          borderRight: `1px solid ${theme.colors.dark[4]}`,
          boxShadow: '8px 0 16px rgba(0, 0, 0, 0.18)',
          boxSizing: 'border-box',
          overflow: 'hidden',
          outline: isOver ? `1px solid ${withAlpha(dropIndicatorColor, 0.55)}` : undefined,
          transition: rowHeightTransition,
        }}
      >
        <Flex align="center" gap={8} style={{ width: '100%', minWidth: 0, overflow: 'hidden' }}>
          <Box
            ref={setActivatorNodeRef}
            aria-label={`Reorder ${difficulty.version}`}
            data-stop-timeline-pan="true"
            {...attributes}
            {...listeners}
            style={{
              flex: '0 0 auto',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 22,
              height: 22,
              borderRadius: theme.radius.sm,
              color: theme.colors.gray[4],
              cursor: isDragging ? 'grabbing' : 'grab',
            }}
          >
            <IconGripVertical size={16} />
          </Box>
          <Group
            gap={8}
            wrap="nowrap"
            style={{
              flex: 1,
              minWidth: 0,
              maxWidth: '100%',
              overflow: 'hidden',
              opacity: isVisible ? 1 : 0.7,
            }}
          >
            <GameModeIcon
              mode={normalizeMode(difficulty.mode)}
              size={18}
              starRating={difficulty.starRating}
              color={getModeAccentColor(normalizeMode(difficulty.mode))}
            />
            <Stack gap={0} style={{ flex: 1, minWidth: 0, overflow: 'hidden' }}>
              <Text fw={600} size="sm" truncate style={{ width: '100%', minWidth: 0 }}>
                {difficulty.version}
              </Text>
              <Text size="xs" c="dimmed" truncate style={{ width: '100%', minWidth: 0 }}>
                {isVisible ? formatGameModeLabel(difficulty.mode) : 'Hidden'}
              </Text>
            </Stack>
          </Group>
          <ActionIcon
            variant="subtle"
            color={isVisible ? 'blue' : 'gray'}
            aria-label={isVisible ? `Hide ${difficulty.version}` : `Show ${difficulty.version}`}
            data-stop-timeline-pan="true"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={() => toggleDifficultyVisibility(difficulty)}
            style={{ flex: '0 0 auto' }}
          >
            {isVisible ? <IconEye size={16} /> : <IconEyeOff size={16} />}
          </ActionIcon>
        </Flex>
      </Box>
      <Box
        h={visibleRowHeight}
        style={{
          flex: `0 0 ${timelineWidth}px`,
          minWidth: timelineWidth,
          width: timelineWidth,
          borderRadius: theme.radius.sm,
          overflow: 'hidden',
          border: `1px solid ${isOver ? withAlpha(dropIndicatorColor, 0.75) : theme.colors.dark[4]}`,
          boxShadow: isOver ? `0 0 0 1px ${withAlpha(dropIndicatorColor, 0.35)} inset` : undefined,
          boxSizing: 'border-box',
          transition: rowHeightTransition,
        }}
      >
        {isVisible ? (
          <TimelineRow difficulty={difficulty} height={visibleRowHeight} />
        ) : (
          <Flex
            h="100%"
            px="sm"
            py={HIDDEN_ROW_VERTICAL_PADDING}
            align="center"
            style={{ boxSizing: 'border-box' }}
          >
            <Text size="xs" c="dimmed" lh={1.2}>
              Timeline hidden for this difficulty.
            </Text>
          </Flex>
        )}
      </Box>
    </Box>
  );
}

export default memo(SortableTimelineDifficultyRow);
