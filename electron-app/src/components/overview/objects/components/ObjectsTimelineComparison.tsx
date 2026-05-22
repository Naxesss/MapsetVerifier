import { Button, Group, Paper, Stack, Text, Title } from '@mantine/core';
import { IconArrowsMaximize } from '@tabler/icons-react';
import { useLayoutEffect, useRef, useState } from 'react';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineFullViewModal from './ObjectsTimelineFullViewModal.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import { useSettings } from '../../../../context/SettingsContext.tsx';
import { ROW_HEIGHT } from '../constants.ts';
import { isHitsoundViewAvailable } from '../hitsoundUtils.ts';
import { useDifficultyRowDnd } from '../hooks/useDifficultyRowDnd.ts';
import { useDifficultyRowVisibility } from '../hooks/useDifficultyRowVisibility.ts';
import { useHorizontalScrollPan } from '../hooks/useHorizontalScrollPan.ts';
import { usePerModeDifficultyOrder } from '../hooks/usePerModeDifficultyOrder.ts';
import { useTimelineZoom } from '../hooks/useTimelineZoom.ts';
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
  const [modalOpened, setModalOpened] = useState(false);
  const savedScrollLeftRef = useRef(0);
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const { zoom, setZoom, tickIntervalMs, timelineWidth, adjustZoom, formatZoomLabel } =
    useTimelineZoom(durationMs);
  const inlinePan = useHorizontalScrollPan();
  const modalPan = useHorizontalScrollPan();
  const { visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible } =
    useDifficultyRowVisibility(groupedDifficulties);
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
  const { settings, setSettings } = useSettings();
  const timelineThemeVariant = settings.timelineThemeVariant;
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
    stopPanning: () => {
      inlinePan.stopDragging();
      modalPan.stopDragging();
    },
  });

  const handleOpenModal = () => {
    savedScrollLeftRef.current = inlinePan.scrollRef.current?.scrollLeft ?? 0;
    inlinePan.stopDragging();
    setModalOpened(true);
  };

  const handleCloseModal = () => {
    savedScrollLeftRef.current = modalPan.scrollRef.current?.scrollLeft ?? savedScrollLeftRef.current;
    modalPan.stopDragging();
    inlinePan.stopDragging();
    setModalOpened(false);
  };

  useLayoutEffect(() => {
    if (modalOpened) {
      const applyModalScroll = () => {
        const modalScroll = modalPan.scrollRef.current;
        if (modalScroll) {
          modalScroll.scrollLeft = savedScrollLeftRef.current;
        }
      };

      applyModalScroll();
      requestAnimationFrame(applyModalScroll);
      return;
    }

    const inlineScroll = inlinePan.scrollRef.current;
    if (inlineScroll) {
      inlineScroll.scrollLeft = savedScrollLeftRef.current;
    }
  }, [modalOpened, inlinePan.scrollRef, modalPan.scrollRef]);

  const sharedTimelineProps = {
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
    onTimelineThemeVariantChange: (variant: typeof timelineThemeVariant) =>
      setSettings((prev) => ({ ...prev, timelineThemeVariant: variant })),
    dndContextProps: {
      sensors,
      collisionDetection,
      autoScroll,
      measuring,
      onDragStart: handleDragStart,
      onDragEnd: handleDragEnd,
      onDragCancel: handleDragCancel,
    },
  };

  const inlineContentProps = {
    ...sharedTimelineProps,
    scrollRef: inlinePan.scrollRef,
    isPanningTimeline: inlinePan.isPanningTimeline,
    handleMouseDown: inlinePan.handleMouseDown,
    handleMouseMove: inlinePan.handleMouseMove,
    stopDragging: inlinePan.stopDragging,
  };

  const modalContentProps = {
    ...sharedTimelineProps,
    scrollRef: modalPan.scrollRef,
    isPanningTimeline: modalPan.isPanningTimeline,
    handleMouseDown: modalPan.handleMouseDown,
    handleMouseMove: modalPan.handleMouseMove,
    stopDragging: modalPan.stopDragging,
  };

  return (
    <>
      <Paper p="md" radius="md" withBorder>
        <Stack gap="md">
          <Group justify="space-between" align="flex-start">
            <Stack gap={2}>
              <Title order={4}>Timeline comparison</Title>
              <Text size="sm" c="dimmed">
                Drag the grip to reorder rows. Drag horizontally in the timeline to pan.
              </Text>
            </Stack>
            {orderedDifficulties.length > 0 && (
              <Group gap="xs" wrap="nowrap">
                <ObjectsTimelineHelpButton
                  showHitsoundSection={isHitsoundViewAvailable(activeMode)}
                />
                <Button
                  leftSection={<IconArrowsMaximize size={16} />}
                  variant="light"
                  size="sm"
                  onClick={handleOpenModal}
                >
                  Full view
                </Button>
              </Group>
            )}
          </Group>

          {modalOpened ? (
            <Text size="sm" c="dimmed" ta="center" py="md">
              Timeline open in full view
            </Text>
          ) : (
            <ObjectsTimelineComparisonContent
              {...inlineContentProps}
              rowHeight={ROW_HEIGHT}
              showModeSelector
              showVisibilityControls
              showThemeControls
              showZoomControls
            />
          )}
        </Stack>
      </Paper>

      <ObjectsTimelineFullViewModal
        opened={modalOpened}
        onClose={handleCloseModal}
        activeMode={activeMode}
        contentProps={modalContentProps}
      />
    </>
  );
}
