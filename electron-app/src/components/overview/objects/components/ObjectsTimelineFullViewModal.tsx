import { Box, Group, Modal, SegmentedControl, Switch, Tooltip } from '@mantine/core';
import { useEffect, useMemo, useState } from 'react';
import HitsoundStripLegend from './HitsoundStripLegend.tsx';
import ObjectsTimelineComparisonContent from './ObjectsTimelineComparisonContent.tsx';
import ObjectsTimelineHelpButton from './ObjectsTimelineHelpButton.tsx';
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
    <Modal.Root opened={opened} onClose={onClose} size="100%" centered>
      <Modal.Overlay />
      <Modal.Content
        styles={{
          content: {
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
          },
        }}
      >
        <Modal.Header>
          <Modal.Title>Timeline comparison</Modal.Title>
          <Modal.CloseButton />
        </Modal.Header>
        <Modal.Body
          styles={{
            body: {
              flex: 1,
              minHeight: 0,
              overflow: 'auto',
            },
          }}
        >
          {opened && (
            <TimelineControllerProvider value={controller}>
              <ObjectsTimelineFullViewModalBody pan={pan} />
            </TimelineControllerProvider>
          )}
        </Modal.Body>
      </Modal.Content>
    </Modal.Root>
  );
}

function ObjectsTimelineFullViewModalBody({ pan }: { pan: TimelinePanValue }) {
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
            headerExtra={headerExtra}
            aboveTimelineExtra={viewMode === 'hitsounding' ? <HitsoundStripLegend /> : undefined}
          />
        </TimelinePanProvider>
      </TimelineFullViewProvider>
    </Box>
  );
}
