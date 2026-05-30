import { Box, Group, Paper, ScrollArea, Stack, Text } from "@mantine/core";
import { IconGripVertical } from "@tabler/icons-react";
import {
  memo,
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
  type RefObject,
} from "react";
import { HitsoundContextDetail } from "./HitsoundContextDetail.tsx";
import { getDifficultyColor } from "../../../common/DifficultyColor.ts";
import DifficultyColorPill from "../../../common/DifficultyColorPill.tsx";
import {
  useTimelineController,
  useTimelineCrosshairState,
  useTimelineFullView,
  useTimelineVisibility,
} from "../context/ObjectsTimelineContext.tsx";
import {
  buildCrosshairRowLookupCache,
  resolveCrosshairRow,
} from "../crosshairUtils.ts";
import { formatEditorTimestamp, getDifficultyKey } from "../timelineUtils.ts";

const DEFAULT_VERTICAL_OFFSET = 50;
const PANEL_MAX_WIDTH = 400;
const PANEL_BODY_MAX_HEIGHT = 420;

type Point = { x: number; y: number };

type DragState = {
  pointerId: number;
  originClient: Point;
  originPanel: Point;
};

type TimelineCrosshairFloatingPanelProps = {
  boundsRef: RefObject<HTMLElement | null>;
  resetKey?: string;
};

function clampPosition(
  point: Point,
  containerWidth: number,
  containerHeight: number,
  panelWidth: number,
  panelHeight: number,
): Point {
  const maxX = Math.max(0, containerWidth - panelWidth);
  const maxY = Math.max(0, containerHeight - panelHeight);

  return {
    x: Math.min(Math.max(0, point.x), maxX),
    y: Math.min(Math.max(0, point.y), maxY),
  };
}

function getDefaultPosition(
  containerWidth: number,
  containerHeight: number,
  panelWidth: number,
  panelHeight: number,
): Point {
  return clampPosition(
    { x: containerWidth - panelWidth, y: DEFAULT_VERTICAL_OFFSET },
    containerWidth,
    containerHeight,
    panelWidth,
    panelHeight,
  );
}

function DifficultyName({ version, starRating }: { version: string; starRating: number | null }) {
  const color = getDifficultyColor(starRating ?? 0);

  return (
    <Group gap={6} wrap="nowrap">
      <DifficultyColorPill color={color} height={14} />
      <Text size="sm" fw={600} style={{ color }}>
        {version}
      </Text>
    </Group>
  );
}

const TimelineCrosshairPanelBody = memo(function TimelineCrosshairPanelBody() {
  const crosshair = useTimelineCrosshairState();
  const { hitsoundLayers } = useTimelineFullView();
  const {
    rows: { orderedDifficulties },
  } = useTimelineController();
  const { visibilityByDifficulty } = useTimelineVisibility();

  const lookupCaches = useMemo(() => {
    const caches = new Map<string, ReturnType<typeof buildCrosshairRowLookupCache>>();

    for (const difficulty of orderedDifficulties) {
      caches.set(
        getDifficultyKey(difficulty),
        buildCrosshairRowLookupCache(difficulty, hitsoundLayers),
      );
    }

    return caches;
  }, [hitsoundLayers, orderedDifficulties]);

  const rows = useMemo(() => {
    if (!crosshair) return [];

    return orderedDifficulties
      .filter((difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false)
      .map((difficulty) => {
        const difficultyKey = getDifficultyKey(difficulty);
        const cache = lookupCaches.get(difficultyKey);
        const samples = cache?.enrichedSamples ?? difficulty.timelineSamples ?? [];
        const resolved = resolveCrosshairRow(
          difficulty,
          crosshair.timestampMs,
          samples,
          cache,
        );

        return {
          key: difficultyKey,
          version: difficulty.version,
          starRating: difficulty.starRating,
          resolved,
        };
      });
  }, [crosshair, lookupCaches, orderedDifficulties, visibilityByDifficulty]);

  if (!crosshair) {
    return (
      <Text size="xs" c="dimmed">
        Drag the timeline horizontally to seek.
      </Text>
    );
  }

  return (
    <Stack gap="sm">
      {rows.map((row) => (
        <Stack key={row.key} gap={4}>
          <DifficultyName version={row.version} starRating={row.starRating} />

          {row.resolved.hasMatch ? (
            <HitsoundContextDetail
              resolved={row.resolved}
              timestampMs={crosshair.timestampMs}
            />
          ) : (
            <Text size="xs" c="dimmed">
              No hitsound context at playhead
            </Text>
          )}
        </Stack>
      ))}
    </Stack>
  );
});

export function TimelineCrosshairFloatingPanel({
  boundsRef,
  resetKey,
}: TimelineCrosshairFloatingPanelProps) {
  const crosshair = useTimelineCrosshairState();
  const {
    rows: { orderedDifficulties },
  } = useTimelineController();
  const panelRef = useRef<HTMLDivElement>(null);
  const posRef = useRef<Point>({ x: 0, y: DEFAULT_VERTICAL_OFFSET });
  const dragRef = useRef<DragState | null>(null);
  const [position, setPosition] = useState<Point>({ x: 0, y: DEFAULT_VERTICAL_OFFSET });
  const [isDragging, setIsDragging] = useState(false);

  const getDefaultPositionFromRefs = useCallback(() => {
    const container = boundsRef.current;
    const panel = panelRef.current;

    if (!container || !panel) {
      return { x: 0, y: DEFAULT_VERTICAL_OFFSET };
    }

    const { width, height } = container.getBoundingClientRect();
    return getDefaultPosition(width, height, panel.offsetWidth, panel.offsetHeight);
  }, [boundsRef]);

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
    [boundsRef],
  );

  const applyPosition = useCallback(
    (point: Point) => {
      const clamped = clampToContainer(point);
      posRef.current = clamped;
      setPosition(clamped);
    },
    [clampToContainer],
  );

  const handleHeaderPointerDown = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
    event.preventDefault();
    event.stopPropagation();

    dragRef.current = {
      pointerId: event.pointerId,
      originClient: { x: event.clientX, y: event.clientY },
      originPanel: { ...posRef.current },
    };

    setIsDragging(true);
    event.currentTarget.setPointerCapture(event.pointerId);
  }, []);

  const handleHeaderPointerMove = useCallback(
    (event: ReactPointerEvent<HTMLDivElement>) => {
      const drag = dragRef.current;

      if (!drag || event.pointerId !== drag.pointerId) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      applyPosition({
        x: drag.originPanel.x + (event.clientX - drag.originClient.x),
        y: drag.originPanel.y + (event.clientY - drag.originClient.y),
      });
    },
    [applyPosition],
  );

  const endHeaderDrag = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
    const drag = dragRef.current;

    if (!drag || event.pointerId !== drag.pointerId) {
      return;
    }

    dragRef.current = null;
    setIsDragging(false);

    if (event.currentTarget.hasPointerCapture(event.pointerId)) {
      event.currentTarget.releasePointerCapture(event.pointerId);
    }
  }, []);

  useLayoutEffect(() => {
    applyPosition(getDefaultPositionFromRefs());
  }, [applyPosition, getDefaultPositionFromRefs, resetKey]);

  useLayoutEffect(() => {
    applyPosition(posRef.current);
  }, [applyPosition, orderedDifficulties.length]);

  useEffect(() => {
    const container = boundsRef.current;

    if (!container) {
      return;
    }

    const observer = new ResizeObserver(() => {
      applyPosition(posRef.current);
    });

    observer.observe(container);

    if (panelRef.current) {
      observer.observe(panelRef.current);
    }

    return () => {
      observer.disconnect();
    };
  }, [applyPosition, boundsRef, resetKey]);

  const headerTitle = crosshair
    ? formatEditorTimestamp(crosshair.timestampMs)
    : "Drag timeline to seek";

  return (
    <Box pos="absolute" inset={0} style={{ zIndex: 30, pointerEvents: "none" }}>
      <Box
        pos="absolute"
        left={position.x}
        top={position.y}
        style={{ pointerEvents: "auto" }}
        onPointerDown={(event) => event.stopPropagation()}
      >
        <Paper
          ref={panelRef}
          shadow="lg"
          radius="md"
          withBorder
          style={{
            width: "max-content",
            maxWidth: PANEL_MAX_WIDTH,
            background: "var(--mantine-color-dark-6)",
          }}
        >
          <Group
            gap={6}
            wrap="nowrap"
            px="xs"
            py={6}
            onPointerDown={handleHeaderPointerDown}
            onPointerMove={handleHeaderPointerMove}
            onPointerUp={endHeaderDrag}
            onPointerCancel={endHeaderDrag}
            style={{
              cursor: isDragging ? "grabbing" : "grab",
              borderBottom: "1px solid var(--mantine-color-dark-4)",
              userSelect: "none",
              touchAction: "none",
            }}
          >
            <IconGripVertical size={14} color="var(--mantine-color-primary-2)" />
            <Text size="xs" mr="xs" fw={600} style={{ lineHeight: 1.35 }}>
              {headerTitle}
            </Text>
          </Group>

          <ScrollArea.Autosize
            mah={PANEL_BODY_MAX_HEIGHT}
            offsetScrollbars
            type="scroll"
            data-timeline-wheel-ignore="true"
          >
            <Box p="xs" mx="sm" pt={8}>
              <TimelineCrosshairPanelBody />
            </Box>
          </ScrollArea.Autosize>
        </Paper>
      </Box>
    </Box>
  );
}
