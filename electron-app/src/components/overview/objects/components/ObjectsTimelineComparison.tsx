import {
  Box,
  Group,
  Paper,
  SegmentedControl,
  Stack,
  Switch,
  Text,
  Title,
  Tooltip,
} from '@mantine/core';
import { useEffect, useMemo, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import TimelineHorizontalReveal from './TimelineHorizontalReveal.tsx';
import { HITSOUND_ROW_HEIGHT, ROW_HEIGHT } from '../constants.ts';
import {
  TimelineControllerProvider,
  TimelineFullViewProvider,
  TimelinePanProvider,
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

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Stack gap={2}>
          <Title order={4}>Timeline comparison</Title>
          <Text size="sm" c="dimmed">
            Drag the grip to reorder rows. Drag horizontally or shift + scroll to pan. Hover or
            right click on objects for more info.
          </Text>
        </Stack>

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

  const hitsoundAvailable = isHitsoundViewAvailable(activeMode);
  const [viewMode, setViewMode] = useState<TimelineViewMode>('structure');
  const [hitsoundLayers, setHitsoundLayers] =
    useState<HitsoundLayerVisibility>(DEFAULT_HITSOUND_LAYERS);

  useEffect(() => {
    return () => {
      pan.stopDragging();
    };
  }, [pan.stopDragging]);

  useEffect(() => {
    if (!hitsoundAvailable && viewMode === 'hitsounding') {
      setViewMode('structure');
    }
  }, [hitsoundAvailable, viewMode]);

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
        <SegmentedControl
          size="xs"
          value={viewMode}
          onChange={(value) => setViewMode(value as TimelineViewMode)}
          data={[
            { label: 'Structure', value: 'structure' },
            {
              label: 'Hitsounding',
              value: 'hitsounding',
              disabled: !hitsoundAvailable,
            },
          ]}
        />
      </Tooltip>
    ),
    [hitsoundAvailable, viewMode]
  );

  const headerExtra = useMemo(
    () => (
      <Group gap={0} wrap="nowrap" align="center">
        <ObjectsTimelineHelpButton showHitsoundSection={hitsoundAvailable} size="xs" />
        <TimelineHorizontalReveal
          visible={viewMode === 'hitsounding'}
          spacing="var(--mantine-spacing-sm)"
        >
          <Group gap="md" wrap="nowrap">
            {layerToggle('body', 'Body sounds')}
            {layerToggle('ticks', 'Ticks')}
            {layerToggle('sampleset', 'Sample bank')}
            {layerToggle('gaps', 'Gap overlay')}
          </Group>
        </TimelineHorizontalReveal>
      </Group>
    ),
    [hitsoundAvailable, hitsoundLayers, viewMode]
  );

  const fullViewValue = useMemo(
    () => ({
      viewMode,
      setViewMode,
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
          <ObjectsTimelineComparisonContent
            showModeSelector
            showVisibilityControls
            showThemeControls
            showZoomControls
            scrollModeExtra={scrollModeExtra}
            headerExtra={headerExtra}
            aboveTimelineExtra={hitsoundAvailable ? <HitsoundStripLegend /> : undefined}
          />
        </TimelinePanProvider>
      </TimelineFullViewProvider>
    </Box>
  );
}
