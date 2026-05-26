import { ActionIcon, Box, Group, Slider, Tooltip } from '@mantine/core';
import { IconMinus, IconPlus } from '@tabler/icons-react';
import { useState } from 'react';
import { MAX_ZOOM, MIN_ZOOM } from '../constants.ts';
import { useTimelineController } from '../context/ObjectsTimelineContext.tsx';
import { clampZoom } from '../timelineUtils.ts';

export default function TimelineZoomControls() {
  const {
    zoom: { zoom, setZoom, adjustZoom, formatZoomLabel },
  } = useTimelineController();
  const [hovered, setHovered] = useState(false);
  const [dragging, setDragging] = useState(false);

  return (
    <Group gap="xs" align="center" wrap="nowrap">
      <ActionIcon variant="default" aria-label="Zoom out" onClick={() => adjustZoom(-1)}>
        <IconMinus size={16} />
      </ActionIcon>
      <Tooltip
        label={`${formatZoomLabel(zoom)}x`}
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
          onPointerLeave={() => setHovered(false)}
          onPointerDown={() => setDragging(true)}
          onPointerUp={() => setDragging(false)}
          onPointerCancel={() => setDragging(false)}
        >
          <Slider
            value={zoom}
            min={MIN_ZOOM}
            max={MAX_ZOOM}
            step={0.01}
            onChange={(value) => setZoom(clampZoom(value))}
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
