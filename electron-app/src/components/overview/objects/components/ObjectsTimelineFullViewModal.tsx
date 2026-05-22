import { Box, Group, Modal, SegmentedControl, Switch, Tooltip } from '@mantine/core';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import {
  TimelineCrosshairFloatingPanel,
  type TimelineCrosshairState,
} from './TimelineCrosshairPanel.tsx';
import { HITSOUND_ROW_HEIGHT, LABEL_WIDTH, ROW_HEIGHT } from '../constants.ts';
import {
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
import {
  getDifficultyKey,
  getFirstNoteTimeMs,
  getPlayheadViewportX,
  getTimestampAtPlayhead,
} from '../timelineUtils.ts';
import type { TimelinePanValue } from '../context/types.ts';

type ObjectsTimelineFullViewModalProps = {
  opened: boolean;
  onClose: () => void;
  pan: TimelinePanValue;
};

export default function ObjectsTimelineFullViewModal({
  opened,
  onClose,
  pan,
}: ObjectsTimelineFullViewModalProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Timeline comparison" size="100%" centered>
      {opened && <ObjectsTimelineFullViewModalBody onClose={onClose} pan={pan} />}
    </Modal>
  );
}

function ObjectsTimelineFullViewModalBody({
  pan,
}: {
  onClose: () => void;
  pan: TimelinePanValue;
}) {
  const controller = useTimelineController();
  const {
    scale: { startTimeMs, endTimeMs },
    zoom: { timelineWidth },
    mode: { activeMode },
    rows: { orderedDifficulties },
    visibility: { visibilityByDifficulty },
  } = controller;

  const hitsoundAvailable = isHitsoundViewAvailable(activeMode);
  const [viewMode, setViewMode] = useState<TimelineViewMode>('structure');
  const [hitsoundLayers, setHitsoundLayers] =
    useState<HitsoundLayerVisibility>(DEFAULT_HITSOUND_LAYERS);
  const [crosshair, setCrosshair] = useState<TimelineCrosshairState | null>(null);
  const modalBodyRef = useRef<HTMLDivElement>(null);

  const playheadViewportX = useMemo(() => {
    if (viewMode !== 'hitsounding') {
      return null;
    }

    const firstNoteTimeMs =
      getFirstNoteTimeMs(orderedDifficulties, visibilityByDifficulty, getDifficultyKey) ??
      startTimeMs;

    return getPlayheadViewportX(
      firstNoteTimeMs,
      startTimeMs,
      endTimeMs,
      timelineWidth,
      LABEL_WIDTH
    );
  }, [
    endTimeMs,
    orderedDifficulties,
    startTimeMs,
    timelineWidth,
    visibilityByDifficulty,
    viewMode,
  ]);

  const updateCrosshairFromScroll = useCallback(() => {
    if (playheadViewportX === null) {
      return;
    }

    const scrollElement = pan.scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const timestampMs = getTimestampAtPlayhead(
      scrollElement.scrollLeft,
      playheadViewportX,
      LABEL_WIDTH,
      timelineWidth,
      startTimeMs,
      endTimeMs
    );

    setCrosshair({ timestampMs });
  }, [endTimeMs, pan.scrollRef, playheadViewportX, startTimeMs, timelineWidth]);

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

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      setCrosshair(null);
      return;
    }

    const scrollElement = pan.scrollRef.current;
    if (scrollElement) {
      scrollElement.scrollLeft = 0;
    }

    updateCrosshairFromScroll();
  }, [pan.scrollRef, playheadViewportX, updateCrosshairFromScroll, viewMode]);

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      return;
    }

    const scrollElement = pan.scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const handleScroll = () => {
      updateCrosshairFromScroll();
    };

    scrollElement.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      scrollElement.removeEventListener('scroll', handleScroll);
    };
  }, [pan.scrollRef, playheadViewportX, updateCrosshairFromScroll, viewMode]);

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      return;
    }

    updateCrosshairFromScroll();
  }, [playheadViewportX, timelineWidth, updateCrosshairFromScroll, viewMode]);

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
    />
  );

  const headerExtra = useMemo(
    () => (
      <Group gap="md" wrap="wrap" align="center">
        <ObjectsTimelineHelpButton showHitsoundSection={hitsoundAvailable} size="xs" />
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
        {viewMode === 'hitsounding' && (
          <Group gap="md" wrap="wrap">
            {layerToggle('body', 'Body sounds')}
            {layerToggle('ticks', 'Ticks')}
            {layerToggle('sampleset', 'Sample bank')}
            {layerToggle('gaps', 'Gap overlay')}
          </Group>
        )}
      </Group>
    ),
    [hitsoundAvailable, hitsoundLayers, viewMode]
  );

  const panelResetKey = `${viewMode}:${timelineWidth}:${orderedDifficulties.length}`;

  const fullViewValue = useMemo(
    () => ({
      viewMode,
      setViewMode,
      hitsoundLayers,
      setHitsoundLayers,
      playheadViewportX,
      crosshair,
      setCrosshair,
      rowHeight,
    }),
    [viewMode, hitsoundLayers, playheadViewportX, crosshair, rowHeight]
  );

  return (
    <Box ref={modalBodyRef} pos="relative">
      <TimelineFullViewProvider value={fullViewValue}>
        <TimelinePanProvider value={pan}>
          <ObjectsTimelineComparisonContent
            showModeSelector={false}
            showVisibilityControls
            showThemeControls
            showZoomControls
            headerExtra={headerExtra}
            aboveTimelineExtra={viewMode === 'hitsounding' ? <HitsoundStripLegend /> : undefined}
          />
        </TimelinePanProvider>

        {viewMode === 'hitsounding' && (
          <TimelineCrosshairFloatingPanel boundsRef={modalBodyRef} resetKey={panelResetKey} />
        )}
      </TimelineFullViewProvider>
    </Box>
  );
}
