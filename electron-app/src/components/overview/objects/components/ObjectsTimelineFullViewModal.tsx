import { Box, Group, Modal, SegmentedControl, Switch, Tooltip } from '@mantine/core';
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
import {
  TimelineCrosshairFloatingPanel,
  type TimelineCrosshairState,
} from './TimelineCrosshairPanel.tsx';
import { HITSOUND_ROW_HEIGHT, LABEL_WIDTH, PLAYHEAD_VIEWPORT_OFFSET, ROW_HEIGHT } from '../constants.ts';
import {
  TimelineControllerProvider,
  TimelineFullViewProvider,
  TimelinePanProvider,
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
  getPlayheadScrollPadding,
  getScrollLeftForTimestamp,
  getTimestampAtPlayhead,
} from '../timelineUtils.ts';
import type { TimelineControllerValue, TimelinePanValue } from '../context/types.ts';

type ObjectsTimelineFullViewModalProps = {
  opened: boolean;
  onClose: () => void;
  pan: TimelinePanValue;
  controller: TimelineControllerValue;
};

export default function ObjectsTimelineFullViewModal({
  opened,
  onClose,
  pan,
  controller,
}: ObjectsTimelineFullViewModalProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Timeline comparison" size="100%" centered>
      {opened && (
        <TimelineControllerProvider value={controller}>
          <ObjectsTimelineFullViewModalBody pan={pan} controller={controller} />
        </TimelineControllerProvider>
      )}
    </Modal>
  );
}

function ObjectsTimelineFullViewModalBody({
  pan,
  controller,
}: {
  pan: TimelinePanValue;
  controller: TimelineControllerValue;
}) {
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
  const prevViewModeRef = useRef<TimelineViewMode>('structure');
  const pendingFirstNoteScrollRef = useRef(false);

  const playheadViewportX = useMemo(() => {
    if (viewMode !== 'hitsounding') {
      return null;
    }

    return LABEL_WIDTH + PLAYHEAD_VIEWPORT_OFFSET;
  }, [viewMode]);

  const firstNoteTimeMs = useMemo(
    () =>
      getFirstNoteTimeMs(orderedDifficulties, visibilityByDifficulty, getDifficultyKey) ??
      startTimeMs,
    [orderedDifficulties, startTimeMs, visibilityByDifficulty]
  );

  const updateCrosshairFromScroll = useCallback(() => {
    if (playheadViewportX === null) {
      return;
    }

    const scrollElement = pan.scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const padding =
      scrollElement.clientWidth > 0
        ? getPlayheadScrollPadding(playheadViewportX, LABEL_WIDTH, scrollElement.clientWidth)
        : undefined;

    const timestampMs = getTimestampAtPlayhead(
      scrollElement.scrollLeft,
      playheadViewportX,
      LABEL_WIDTH,
      timelineWidth,
      startTimeMs,
      endTimeMs,
      padding
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

  useLayoutEffect(() => {
    const enteredHitsounding =
      viewMode === 'hitsounding' && prevViewModeRef.current !== 'hitsounding';
    prevViewModeRef.current = viewMode;

    if (viewMode !== 'hitsounding') {
      pendingFirstNoteScrollRef.current = false;
      return;
    }

    if (enteredHitsounding) {
      pendingFirstNoteScrollRef.current = true;
    }

    if (!pendingFirstNoteScrollRef.current || playheadViewportX === null) {
      return;
    }

    const scrollElement = pan.scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const scrollToFirstNote = () => {
      if (!pendingFirstNoteScrollRef.current || scrollElement.clientWidth <= 0) {
        return;
      }

      const padding = getPlayheadScrollPadding(
        playheadViewportX,
        LABEL_WIDTH,
        scrollElement.clientWidth
      );
      const scrollLeft = getScrollLeftForTimestamp(
        firstNoteTimeMs,
        playheadViewportX,
        LABEL_WIDTH,
        timelineWidth,
        startTimeMs,
        endTimeMs,
        padding
      );
      const maxScrollLeft = Math.max(0, scrollElement.scrollWidth - scrollElement.clientWidth);
      scrollElement.scrollLeft = Math.max(0, Math.min(maxScrollLeft, scrollLeft));
      pendingFirstNoteScrollRef.current = false;
      updateCrosshairFromScroll();
    };

    scrollToFirstNote();

    const observer = new ResizeObserver(scrollToFirstNote);
    observer.observe(scrollElement);

    return () => {
      observer.disconnect();
    };
  }, [
    endTimeMs,
    firstNoteTimeMs,
    pan.scrollRef,
    playheadViewportX,
    startTimeMs,
    timelineWidth,
    updateCrosshairFromScroll,
    viewMode,
  ]);

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      setCrosshair(null);
      return;
    }

    updateCrosshairFromScroll();
  }, [playheadViewportX, timelineWidth, updateCrosshairFromScroll, viewMode]);

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
            showModeSelector
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
