import { Box, Group, Paper, ScrollArea, Stack, Text, Tooltip } from "@mantine/core";
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
import {
  formatSampleBankLine,
  getHitsoundTypesFromFlags,
  HITSOUND_FLAG_NORMAL,
  type HitsoundTypeDisplay,
} from "../hitsoundUtils.ts";
import { formatEditorTimestamp, getDifficultyKey } from "../timelineUtils.ts";
import type { ObjectsTimelineSample } from "../../../../Types";

const DEFAULT_VERTICAL_OFFSET = 50;
const PANEL_MAX_WIDTH = 360;
const PANEL_BODY_MAX_HEIGHT = 320;

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

function HitsoundTypeSwatch({ type }: { type: HitsoundTypeDisplay }) {
  const isSecondary = type.role === "ring";
  const tooltipLabel = isSecondary ? "Stacked addition" : "Primary addition";

  return (
    <Tooltip label={tooltipLabel}>
      <Group gap={4} wrap="nowrap" style={{ cursor: "help" }}>
        <Box
          style={{
            width: 10,
            height: 10,
            borderRadius: isSecondary ? "50%" : 2,
            background: type.color,
            boxShadow: isSecondary
              ? `0 0 0 1px ${type.color}, 0 0 0 2px rgba(255, 255, 255, 0.25)`
              : undefined,
            flexShrink: 0,
            opacity: isSecondary ? 0.65 : 1,
          }}
        />
        <Text size="xs" c={isSecondary ? "dimmed" : undefined}>
          {type.label}
        </Text>
      </Group>
    </Tooltip>
  );
}

function formatHitsoundPartLabel(partName: string, sampleSource: string | null): string {
  if (sampleSource === "Body") {
    return "Slider body";
  }
  if (sampleSource === "Tick") {
    return "Slider tick";
  }
  return partName;
}

function HitsoundSampleDetail({
  partName,
  hitSoundFlags,
  sample,
  sampleSource,
}: {
  partName: string;
  hitSoundFlags: number;
  sample: ObjectsTimelineSample | null;
  sampleSource: string | null;
}) {
  const hitsoundTypes = getHitsoundTypesFromFlags(hitSoundFlags || HITSOUND_FLAG_NORMAL);
  const showAdditions = sampleSource === "Edge";

  return (
    <Stack gap={4}>
      <Text size="sm" fw={500}>
        {formatHitsoundPartLabel(partName, sampleSource)}
      </Text>
      {showAdditions && (
        <Group gap={6} wrap="wrap" align="center">
          <Text size="xs" c="dimmed">
            Hitsound additions
          </Text>
          {hitsoundTypes.map((type) => (
            <HitsoundTypeSwatch key={`${type.label}-${type.role}`} type={type} />
          ))}
        </Group>
      )}

      {sample ? (
        <Text size="xs" c="dimmed">
          Sample bank: {formatSampleBankLine(sample.sampleset, sample.customIndex)}
        </Text>
      ) : (
        <Text size="xs" c="dimmed">
          No sample bank at this edge
        </Text>
      )}
    </Stack>
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
        buildCrosshairRowLookupCache(difficulty, hitsoundLayers)
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
        const samples = cache?.sortedSamples ?? [];
        const resolved = resolveCrosshairRow(
          difficulty,
          crosshair.timestampMs,
          samples,
          cache
        );

        return {
          key: difficultyKey,
          version: difficulty.version,
          starRating: difficulty.starRating,
          partName: resolved.partName,
          hitSoundFlags: resolved.hitSoundFlags,
          sample: resolved.sample,
          sampleSource: resolved.sampleSource,
          hasEdge: resolved.hasMatch,
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

          {row.hasEdge ? (
            <HitsoundSampleDetail
              partName={row.partName}
              hitSoundFlags={row.hitSoundFlags}
              sample={row.sample}
              sampleSource={row.sampleSource}
            />
          ) : (
            <Text size="xs" c="dimmed">
              No sample at playhead
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

  const headerTitle = crosshair ? formatEditorTimestamp(crosshair.timestampMs) : "Drag timeline to seek";

  return (
    <Box pos="absolute" inset={0} style={{ zIndex: 30, pointerEvents: "none" }}>
      <Box
        pos="absolute"
        left={position.x}
        top={position.y}
        style={{ pointerEvents: "auto" }}
        onPointerDown={(event) => event.stopPropagation()}>
        <Paper
          ref={panelRef}
          shadow="lg"
          radius="md"
          withBorder
          style={{
            width: "max-content",
            maxWidth: PANEL_MAX_WIDTH,
            background: "var(--mantine-color-dark-6)",
          }}>
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
            }}>
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
