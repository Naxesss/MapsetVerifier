import {
  Box,
  Group,
  Loader,
  Paper,
  SegmentedControl,
  Stack,
  Switch,
  Text,
  Title,
  Tooltip,
} from '@mantine/core';
import { useDeferredValue, useEffect, useMemo, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import TimelineHorizontalReveal from './TimelineHorizontalReveal.tsx';
import { HITSOUND_ROW_HEIGHT, ROW_HEIGHT } from '../constants.ts';
import {
  TimelineControllerProvider,
  TimelineFullViewProvider,
  TimelinePanProvider,
  TimelineViewportProvider,
  useTimelineController,
} from '../context/ObjectsTimelineContext.tsx';
import {
  DEFAULT_HITSOUND_LAYERS,
  isHitsoundViewAvailable,
  type HitsoundLayerVisibility,
  type TimelineViewMode,
} from '../hitsoundUtils.ts';
import { useHorizontalScrollPan } from '../hooks/useHorizontalScrollPan.ts';
import { useObjectsTimelineController } from '../hooks/useObjectsTimelineController.ts';
import { useTimelineViewportRange } from '../hooks/useTimelineViewportRange.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { TimelinePanValue } from '../context/types.ts';
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
  const pan = useHorizontalScrollPan();

  const controller = useObjectsTimelineController({
    startTimeMs,
    endTimeMs,
    groupedDifficulties,
    difficulties,
    selectedMode,
    onModeChange,
    stopPanning: () => pan.stopDragging(),
  });

  const hitsoundAvailable = isHitsoundViewAvailable(controller.mode.activeMode);

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start" wrap="nowrap">
          <Stack gap={2}>
            <Title order={4}>Timeline comparison</Title>
            <Text size="sm" c="dimmed">
              Drag the grip to reorder rows. Drag horizontally or shift + scroll to pan. Hover or
              right click on objects for more info.
            </Text>
          </Stack>
          <ObjectsTimelineHelpButton showHitsoundSection={hitsoundAvailable} />
        </Group>

        <TimelineControllerProvider value={controller}>
          <ObjectsTimelineComparisonBody pan={pan} />
        </TimelineControllerProvider>
      </Stack>
    </Paper>
  );
}

function ObjectsTimelineComparisonBody({ pan }: { pan: TimelinePanValue }) {
  const controller = useTimelineController();
  const {
    mode: { activeMode },
  } = controller;

  const viewport = useTimelineViewportRange(pan.scrollRef, controller.zoom.timelineWidth);

  const hitsoundAvailable = isHitsoundViewAvailable(activeMode);
  const [selectedViewMode, setSelectedViewMode] = useState<TimelineViewMode>('structure');
  const effectiveSelectedViewMode =
    !hitsoundAvailable && selectedViewMode === 'hitsounding' ? 'structure' : selectedViewMode;
  const viewMode = useDeferredValue(effectiveSelectedViewMode);
  const isViewModePending = effectiveSelectedViewMode !== viewMode;
  const [hitsoundLayers, setHitsoundLayers] =
    useState<HitsoundLayerVisibility>(DEFAULT_HITSOUND_LAYERS);

  useEffect(() => {
    return () => {
      pan.stopDragging();
    };
  }, [pan.stopDragging]);

  const rowHeight = viewMode === 'hitsounding' ? HITSOUND_ROW_HEIGHT : ROW_HEIGHT;

  const layerToggle = (key: keyof HitsoundLayerVisibility, label: string) => (
    <Switch
      key={key}
      size="xs"
      checked={hitsoundLayers[key]}
      onChange={(event) => {
        const checked = event.currentTarget.checked;
        setHitsoundLayers((prev) => ({ ...prev, [key]: checked }));
      }}
      label={label}
      styles={{ root: { flexShrink: 0 }, label: { whiteSpace: 'nowrap' } }}
    />
  );

  const scrollModeExtra = useMemo(
    () => (
      <Tooltip
        label="Hitsounding view is available for osu! and osu!catch only"
        disabled={hitsoundAvailable}
        withArrow
      >
        <Group gap="xs" wrap="nowrap">
          <SegmentedControl
            size="xs"
            value={effectiveSelectedViewMode}
            onChange={(value) => setSelectedViewMode(value as TimelineViewMode)}
            data={[
              { label: 'Structure', value: 'structure' },
              {
                label: 'Hitsounding',
                value: 'hitsounding',
                disabled: !hitsoundAvailable,
              },
            ]}
          />
          {isViewModePending && <Loader size="xs" />}
        </Group>
      </Tooltip>
    ),
    [hitsoundAvailable, isViewModePending, effectiveSelectedViewMode]
  );

  const headerExtra = useMemo(
    () => (
      <TimelineHorizontalReveal visible={viewMode === 'hitsounding'}>
        <Group gap="md">
          {layerToggle('body', 'Body sounds')}
          {layerToggle('ticks', 'Ticks')}
          {layerToggle('sampleset', 'Sample bank')}
          {layerToggle('gaps', 'Gap overlay')}
        </Group>
      </TimelineHorizontalReveal>
    ),
    [hitsoundLayers, viewMode]
  );

  const fullViewValue = useMemo(
    () => ({
      viewMode,
      setViewMode: setSelectedViewMode,
      hitsoundLayers,
      setHitsoundLayers,
      rowHeight,
    }),
    [viewMode, hitsoundLayers, rowHeight]
  );

  return (
    <Box pos="relative">
      <TimelineFullViewProvider value={fullViewValue}>
        <TimelinePanProvider value={pan}>
          <TimelineViewportProvider value={viewport}>
            <ObjectsTimelineComparisonContent
              showModeSelector
              showVisibilityControls
              showThemeControls
              showZoomControls
              scrollModeExtra={scrollModeExtra}
              headerExtra={headerExtra}
              aboveTimelineExtra={hitsoundAvailable ? <HitsoundStripLegend /> : undefined}
            />
          </TimelineViewportProvider>
        </TimelinePanProvider>
      </TimelineFullViewProvider>
    </Box>
  );
}
