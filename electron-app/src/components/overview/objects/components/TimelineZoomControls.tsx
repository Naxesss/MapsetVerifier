import { ActionIcon, Box, Group, Slider, Tooltip } from '@mantine/core';
import { IconMinus, IconPlus } from '@tabler/icons-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { MAX_ZOOM, MIN_ZOOM } from '../constants.ts';
import { useTimelineController } from '../context/ObjectsTimelineContext.tsx';
import { clampZoom } from '../timelineUtils.ts';

export default function TimelineZoomControls() {
  const {
    zoom: { zoom, setZoom, adjustZoom, formatZoomLabel },
  } = useTimelineController();
  const [hovered, setHovered] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [sliderZoom, setSliderZoom] = useState(zoom);
  const rafRef = useRef<number | null>(null);
  const pendingZoomRef = useRef<number | null>(null);

  useEffect(() => {
    if (!dragging) {
      setSliderZoom(zoom);
    }
  }, [dragging, zoom]);

  useEffect(() => {
    return () => {
      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
      }
    };
  }, []);

  const flushPendingZoom = useCallback(() => {
    if (rafRef.current !== null) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }

    if (pendingZoomRef.current === null) {
      return;
    }

    setZoom(pendingZoomRef.current);
    pendingZoomRef.current = null;
  }, [setZoom]);

  const scheduleZoomCommit = useCallback(
    (value: number) => {
      pendingZoomRef.current = clampZoom(value);

      if (rafRef.current !== null) {
        return;
      }

      rafRef.current = requestAnimationFrame(() => {
        rafRef.current = null;
        if (pendingZoomRef.current !== null) {
          setZoom(pendingZoomRef.current);
          pendingZoomRef.current = null;
        }
      });
    },
    [setZoom]
  );

  const handleSliderChange = useCallback(
    (value: number) => {
      const clamped = clampZoom(value);
      setSliderZoom(clamped);
      scheduleZoomCommit(clamped);
    },
    [scheduleZoomCommit]
  );

  const endSliderDrag = useCallback(() => {
    setDragging(false);
    flushPendingZoom();
    setZoom(clampZoom(sliderZoom));
  }, [flushPendingZoom, setZoom, sliderZoom]);

  const displayZoom = dragging ? sliderZoom : zoom;

  return (
    <Group gap="xs" align="center" wrap="nowrap">
      <ActionIcon variant="default" aria-label="Zoom out" onClick={() => adjustZoom(-1)}>
        <IconMinus size={16} />
      </ActionIcon>
      <Tooltip
        label={`${formatZoomLabel(displayZoom)}x`}
        opened={hovered || dragging}
        withinPortal
        position="top"
        withArrow
        transitionProps={{ duration: 0 }}
        events={{ hover: false, focus: false, touch: false }}
      >
        <Box
          miw={220}
          onPointerEnter={() => setHovered(true)}
          onPointerLeave={() => {
            setHovered(false);
            if (dragging) {
              endSliderDrag();
            }
          }}
          onPointerDown={() => setDragging(true)}
          onPointerUp={endSliderDrag}
          onPointerCancel={endSliderDrag}
        >
          <Slider
            value={sliderZoom}
            min={MIN_ZOOM}
            max={MAX_ZOOM}
            step={0.01}
            onChange={handleSliderChange}
            label={null}
            styles={{
              label: {
                display: 'none',
              },
            }}
          />
        </Box>
      </Tooltip>
      <ActionIcon variant="default" aria-label="Zoom in" onClick={() => adjustZoom(1)}>
        <IconPlus size={16} />
      </ActionIcon>
    </Group>
  );
}
