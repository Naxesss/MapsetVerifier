import { Button, Group, Paper, Stack, Text, Title } from '@mantine/core';
import { IconArrowsMaximize } from '@tabler/icons-react';
import { useLayoutEffect, useRef, useState } from 'react';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineFullViewModal from './ObjectsTimelineFullViewModal.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import {
  TimelineControllerProvider,
  TimelinePanProvider,
} from '../context/ObjectsTimelineContext.tsx';
import { isHitsoundViewAvailable } from '../hitsoundUtils.ts';
import { useHorizontalScrollPan } from '../hooks/useHorizontalScrollPan.ts';
import { useObjectsTimelineController } from '../hooks/useObjectsTimelineController.ts';
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
  const inlinePan = useHorizontalScrollPan();
  const modalPan = useHorizontalScrollPan();

  const controller = useObjectsTimelineController({
    startTimeMs,
    endTimeMs,
    groupedDifficulties,
    difficulties,
    selectedMode,
    onModeChange,
    stopPanning: () => {
      inlinePan.stopDragging();
      modalPan.stopDragging();
    },
  });

  const { orderedDifficulties } = controller.rows;
  const { activeMode } = controller.mode;

  const handleOpenModal = () => {
    savedScrollLeftRef.current = inlinePan.scrollRef.current?.scrollLeft ?? 0;
    inlinePan.stopDragging();
    setModalOpened(true);
  };

  const handleCloseModal = () => {
    savedScrollLeftRef.current =
      modalPan.scrollRef.current?.scrollLeft ?? savedScrollLeftRef.current;
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

  return (
    <>
      <Paper p="md" radius="md" withBorder>
        <Stack gap="md">
          <Group justify="space-between" align="flex-start">
            <Stack gap={2}>
              <Title order={4}>Timeline comparison</Title>
              <Text size="sm" c="dimmed">
                Drag the grip to reorder rows. Drag horizontally or shift + scroll to pan. Hover or
                right click on objects for more info.
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
            <TimelineControllerProvider value={controller}>
              <TimelinePanProvider value={inlinePan}>
                <ObjectsTimelineComparisonContent
                  showModeSelector
                  showVisibilityControls
                  showThemeControls
                  showZoomControls
                />
              </TimelinePanProvider>
            </TimelineControllerProvider>
          )}
        </Stack>
      </Paper>

      <ObjectsTimelineFullViewModal
        opened={modalOpened}
        onClose={handleCloseModal}
        pan={modalPan}
        controller={controller}
      />
    </>
  );
}
