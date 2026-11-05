import { Box, Group, Loader, SegmentedControl, Stack, Switch, Tooltip } from '@mantine/core';
import { useDeferredValue, useEffect, useMemo, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import TimelineHorizontalReveal from './TimelineHorizontalReveal.tsx';
import SectionCard from '../../../common/SectionCard.tsx';
import { HITSOUND_ROW_HEIGHT, ROW_HEIGHT } from '../constants.ts';
import {
  TimelineControllerProvider,
  TimelineEdgesProvider,
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
import { useMeasuredTimelineEdges } from '../hooks/useMeasuredTimelineEdges.ts';
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
  audioUrl?: string;
}

export default function ObjectsTimelineComparison({
  startTimeMs,
  endTimeMs,
  groupedDifficulties,
  difficulties,
  selectedMode,
  audioUrl,
}: ObjectsTimelineComparisonProps) {
  const pan = useHorizontalScrollPan();

  const controller = useObjectsTimelineController({
    startTimeMs,
    endTimeMs,
    groupedDifficulties,
    difficulties,
    selectedMode,
    stopPanning: () => pan.stopDragging(),
  });

  const hitsoundAvailable = isHitsoundViewAvailable(controller.mode.activeMode);

  return (
    <SectionCard
      title="Timeline comparison"
      actions={<ObjectsTimelineHelpButton showHitsoundSection={hitsoundAvailable} />}
    >
      <Stack gap="md">
        <TimelineControllerProvider value={controller}>
          <ObjectsTimelineComparisonBody pan={pan} audioUrl={audioUrl} />
        </TimelineControllerProvider>
      </Stack>
    </SectionCard>
  );
}

function ObjectsTimelineComparisonBody({
  pan,
  audioUrl,
}: {
  pan: TimelinePanValue;
  audioUrl?: string;
}) {
  const controller = useTimelineController();
  const {
    mode: { activeMode },
  } = controller;

  const edges = useMeasuredTimelineEdges(pan.scrollRef);
  const viewport = useTimelineViewportRange(
    pan.scrollRef,
    controller.zoom.timelineWidth,
    edges.originX
  );

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
          <TimelineEdgesProvider value={edges}>
            <TimelineViewportProvider value={viewport}>
              <ObjectsTimelineComparisonContent
                showVisibilityControls
                showThemeControls
                showZoomControls
                audioUrl={audioUrl}
                scrollModeExtra={scrollModeExtra}
                headerExtra={headerExtra}
                aboveTimelineExtra={hitsoundAvailable ? <HitsoundStripLegend /> : undefined}
              />
            </TimelineViewportProvider>
          </TimelineEdgesProvider>
        </TimelinePanProvider>
      </TimelineFullViewProvider>
    </Box>
  );
}
