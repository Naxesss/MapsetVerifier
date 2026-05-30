import { Box, Group, Paper, Text } from '@mantine/core';
import { useMove, type UseMovePosition } from '@mantine/hooks';
import { IconGripVertical } from '@tabler/icons-react';
import { useCallback, useEffect, useLayoutEffect, useRef, useState, type RefObject } from 'react';
import TimeSeriesHoverTooltip from './TimeSeriesHoverTooltip.tsx';
import { formatEditorTimestamp } from '../../overview/objects/timelineUtils.ts';
import type { PeakHoverState } from './types.ts';

const DEFAULT_POSITION = { x: 16, y: 16 };
/** Invisible padding around the panel so chart hover is not cleared while crossing the gap. */
const HOVER_SAFE_ZONE_PADDING = 24;

type Point = { x: number; y: number };

type ScrubState = {
  panel: Point;
  move: UseMovePosition | null;
  containerWidth: number;
  containerHeight: number;
};

type ChartHoverFloatingPanelProps = {
  boundsRef: RefObject<HTMLElement | null>;
  hover: PeakHoverState | null;
  valueSuffix?: string;
  resetKey?: string;
  onSafeZoneChange?: (inside: boolean) => void;
};

function clampPosition(
  point: Point,
  containerWidth: number,
  containerHeight: number,
  panelWidth: number,
  panelHeight: number
): Point {
  const maxX = Math.max(0, containerWidth - panelWidth);
  const maxY = Math.max(0, containerHeight - panelHeight);
  return {
    x: Math.min(Math.max(0, point.x), maxX),
    y: Math.min(Math.max(0, point.y), maxY),
  };
}

export function ChartHoverFloatingPanel({
  boundsRef,
  hover,
  valueSuffix,
  resetKey,
  onSafeZoneChange,
}: ChartHoverFloatingPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const posRef = useRef<Point>(DEFAULT_POSITION);
  const scrubRef = useRef<ScrubState | null>(null);
  const [position, setPosition] = useState(DEFAULT_POSITION);

  const clampToContainer = useCallback(
    (point: Point) => {
      const container = boundsRef.current;
      const panel = panelRef.current;
      if (!container || !panel) {
        return point;
      }

      const { width, height } = container.getBoundingClientRect();
      return clampPosition(point, width, height, panel.offsetWidth, panel.offsetHeight);
    },
    [boundsRef]
  );

  const applyPosition = useCallback(
    (point: Point) => {
      const clamped = clampToContainer(point);
      posRef.current = clamped;
      setPosition(clamped);
    },
    [clampToContainer]
  );

  const { ref: setMoveNode, active } = useMove(
    ({ x, y }) => {
      const scrub = scrubRef.current;
      if (!scrub) {
        return;
      }

      if (scrub.move === null) {
        scrub.move = { x, y };
        return;
      }

      const dx = (x - scrub.move.x) * scrub.containerWidth;
      const dy = (y - scrub.move.y) * scrub.containerHeight;

      applyPosition({
        x: scrub.panel.x + dx,
        y: scrub.panel.y + dy,
      });
    },
    {
      onScrubStart: () => {
        const container = boundsRef.current;
        if (!container) {
          scrubRef.current = null;
          return;
        }

        const { width, height } = container.getBoundingClientRect();
        scrubRef.current = {
          panel: { ...posRef.current },
          move: null,
          containerWidth: width,
          containerHeight: height,
        };
      },
      onScrubEnd: () => {
        scrubRef.current = null;
      },
    }
  );

  useEffect(() => {
    const node = boundsRef.current;
    setMoveNode(node);
    return () => {
      setMoveNode(null);
    };
  }, [boundsRef, setMoveNode, resetKey]);

  useEffect(() => {
    applyPosition(DEFAULT_POSITION);
  }, [applyPosition, resetKey]);

  useLayoutEffect(() => {
    applyPosition(posRef.current);
  }, [applyPosition, hover?.values.length, valueSuffix]);

  const headerTitle = hover ? formatEditorTimestamp(hover.timeMs) : 'Hover over chart';

  return (
    <Box
      pos="absolute"
      left={position.x - HOVER_SAFE_ZONE_PADDING}
      top={position.y - HOVER_SAFE_ZONE_PADDING}
      p={HOVER_SAFE_ZONE_PADDING}
      style={{ zIndex: 30, pointerEvents: 'auto' }}
      onPointerEnter={() => onSafeZoneChange?.(true)}
      onPointerLeave={() => onSafeZoneChange?.(false)}
    >
      <Paper
        ref={panelRef}
        shadow="lg"
        radius="md"
        withBorder
        style={{
          width: 'max-content',
          maxWidth: 360,
          background: 'var(--mantine-color-dark-6)',
        }}
      >
        <Group
          gap={6}
          wrap="nowrap"
          px="xs"
          py={6}
          style={{
            cursor: active ? 'grabbing' : 'grab',
            borderBottom: '1px solid var(--mantine-color-dark-4)',
            userSelect: 'none',
            touchAction: 'none',
          }}
        >
          <IconGripVertical size={14} color="var(--mantine-color-primary-2)" />
          <Text size="xs" mr="xs" fw={600} style={{ lineHeight: 1.35 }}>
            {headerTitle}
          </Text>
        </Group>
        <Box p="xs" mx="sm" pt={8} onMouseDown={(event) => event.stopPropagation()}>
          {hover ? (
            <TimeSeriesHoverTooltip
              hover={hover}
              valueSuffix={valueSuffix}
              showTimestamp={false}
              embedded
            />
          ) : (
            <Text size="xs" c="dimmed">
              to inspect values.
            </Text>
          )}
        </Box>
      </Paper>
    </Box>
  );
}
