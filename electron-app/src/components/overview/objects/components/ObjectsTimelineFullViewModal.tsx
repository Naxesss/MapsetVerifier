import {
  Box,
  Group,
  Modal,
  SegmentedControl,
  Switch,
  Tooltip,
} from '@mantine/core';
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
import type { ObjectsTimelineComparisonContentProps } from './ObjectsTimelineComparisonContent.tsx';
import type { Mode } from '../../../../Types';

type ObjectsTimelineFullViewModalProps = {
  opened: boolean;
  onClose: () => void;
  activeMode: Mode | undefined;
  contentProps: Omit<
    ObjectsTimelineComparisonContentProps,
    | 'rowHeight'
    | 'viewMode'
    | 'hitsoundLayers'
    | 'showModeSelector'
    | 'showVisibilityControls'
    | 'showThemeControls'
    | 'showZoomControls'
    | 'headerExtra'
    | 'playheadViewportX'
  >;
};

export default function ObjectsTimelineFullViewModal({
  opened,
  onClose,
  activeMode,
  contentProps,
}: ObjectsTimelineFullViewModalProps) {
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
      getFirstNoteTimeMs(
        contentProps.orderedDifficulties,
        contentProps.visibilityByDifficulty,
        getDifficultyKey
      ) ?? contentProps.startTimeMs;

    return getPlayheadViewportX(
      firstNoteTimeMs,
      contentProps.startTimeMs,
      contentProps.endTimeMs,
      contentProps.timelineWidth,
      LABEL_WIDTH
    );
  }, [
    contentProps.endTimeMs,
    contentProps.orderedDifficulties,
    contentProps.startTimeMs,
    contentProps.timelineWidth,
    contentProps.visibilityByDifficulty,
    viewMode,
  ]);

  const updateCrosshairFromScroll = useCallback(() => {
    if (playheadViewportX === null) {
      return;
    }

    const scrollElement = contentProps.scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const timestampMs = getTimestampAtPlayhead(
      scrollElement.scrollLeft,
      playheadViewportX,
      LABEL_WIDTH,
      contentProps.timelineWidth,
      contentProps.startTimeMs,
      contentProps.endTimeMs
    );

    setCrosshair({ timestampMs });
  }, [
    contentProps.endTimeMs,
    contentProps.scrollRef,
    contentProps.startTimeMs,
    contentProps.timelineWidth,
    playheadViewportX,
  ]);

  useEffect(() => {
    if (!opened) {
      setViewMode('structure');
      setHitsoundLayers(DEFAULT_HITSOUND_LAYERS);
      setCrosshair(null);
      contentProps.stopDragging();
    }
  }, [contentProps.stopDragging, opened]);

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

    const scrollElement = contentProps.scrollRef.current;
    if (scrollElement) {
      scrollElement.scrollLeft = 0;
    }

    updateCrosshairFromScroll();
  }, [
    contentProps.scrollRef,
    playheadViewportX,
    updateCrosshairFromScroll,
    viewMode,
  ]);

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      return;
    }

    const scrollElement = contentProps.scrollRef.current;
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
  }, [
    contentProps.scrollRef,
    playheadViewportX,
    updateCrosshairFromScroll,
    viewMode,
  ]);

  useEffect(() => {
    if (viewMode !== 'hitsounding' || playheadViewportX === null) {
      return;
    }

    updateCrosshairFromScroll();
  }, [contentProps.timelineWidth, playheadViewportX, updateCrosshairFromScroll, viewMode]);

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

  const panelResetKey = `${opened}:${viewMode}:${contentProps.timelineWidth}:${contentProps.orderedDifficulties.length}`;

  return (
    <Modal opened={opened} onClose={onClose} title="Timeline comparison" size="100%" centered>
      <Box ref={modalBodyRef} pos="relative">
        <ObjectsTimelineComparisonContent
          {...contentProps}
          rowHeight={rowHeight}
          viewMode={viewMode}
          hitsoundLayers={hitsoundLayers}
          playheadViewportX={playheadViewportX}
          showModeSelector={false}
          showVisibilityControls
          showThemeControls
          showZoomControls
          headerExtra={headerExtra}
          aboveTimelineExtra={viewMode === 'hitsounding' ? <HitsoundStripLegend /> : undefined}
        />

        {viewMode === 'hitsounding' && (
          <TimelineCrosshairFloatingPanel
            boundsRef={modalBodyRef}
            crosshair={crosshair}
            orderedDifficulties={contentProps.orderedDifficulties}
            visibilityByDifficulty={contentProps.visibilityByDifficulty}
            getDifficultyKey={getDifficultyKey}
            hitsoundLayers={hitsoundLayers}
            resetKey={panelResetKey}
          />
        )}
      </Box>
    </Modal>
  );
}
