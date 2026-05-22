import { Box, Group, Paper, ScrollArea, Stack, Text, Tooltip } from "@mantine/core";
import { IconGripVertical } from "@tabler/icons-react";
import {
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
  findNearestSample,
  formatSampleBankLine,
  getHitsoundTypesFromFlags,
  HITSOUND_FLAG_NORMAL,
  type HitsoundLayerVisibility,
  type HitsoundTypeDisplay,
} from "../hitsoundUtils.ts";
import { filterSamplesForHover } from "../timelineHitsoundDrawing.ts";
import {
  findEdgeSampleAtTime,
  findNearestTimelineEdge,
  formatEditorTimestamp,
  getEdgeHitSoundFlags,
} from "../timelineUtils.ts";
import type { ObjectsOverviewDifficulty, ObjectsTimelineSample } from "../../../../Types";

export type TimelineCrosshairState = {
  timestampMs: number;
};

const DEFAULT_POSITION = { x: 16, y: 16 };
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
  crosshair: TimelineCrosshairState | null;
  orderedDifficulties: ObjectsOverviewDifficulty[];
  visibilityByDifficulty: Record<string, boolean | undefined>;
  getDifficultyKey: (difficulty: ObjectsOverviewDifficulty) => string;
  hitsoundLayers: HitsoundLayerVisibility;
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
  const tooltipLabel = isSecondary ? "Secondary" : "Dominant";

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

function HitsoundSampleDetail({
  partName,
  hitSoundFlags,
  sample,
}: {
  partName: string;
  hitSoundFlags: number;
  sample: ObjectsTimelineSample | null;
}) {
  const hitsoundTypes = getHitsoundTypesFromFlags(hitSoundFlags || HITSOUND_FLAG_NORMAL);

  return (
    <Stack gap={4}>
      <Text size="sm" fw={500}>
        {partName}
      </Text>
      <Group gap={6} wrap="wrap" align="center">
        <Text size="xs" c="dimmed">
          Object circle
        </Text>
        {hitsoundTypes.map((type) => (
          <HitsoundTypeSwatch key={`${type.label}-${type.role}`} type={type} />
        ))}
      </Group>

      {sample ? (
        <Text size="xs" c="dimmed">
          Sample file: {formatSampleBankLine(sample.sampleset, sample.customIndex)}
        </Text>
      ) : (
        <Text size="xs" c="dimmed">
          No sample metadata at this edge
        </Text>
      )}
    </Stack>
  );
}

function TimelineCrosshairPanelBody({
  crosshair,
  orderedDifficulties,
  visibilityByDifficulty,
  getDifficultyKey,
  hitsoundLayers,
}: Omit<TimelineCrosshairFloatingPanelProps, "boundsRef" | "resetKey">) {
  const rows = useMemo(() => {
    if (!crosshair) return [];

    return orderedDifficulties
      .filter((difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false)
      .map((difficulty) => {
        const samples = filterSamplesForHover(difficulty.timelineSamples ?? [], hitsoundLayers);
        const edgeMatch = findNearestTimelineEdge(difficulty.timelineObjects, crosshair.timestampMs);
        const edgeSample = edgeMatch ? findEdgeSampleAtTime(samples, edgeMatch.edge.timeMs) : null;
        const fallbackSample = findNearestSample(
          samples.filter((sample) => sample.source === "Edge"),
          crosshair.timestampMs,
        );

        return {
          key: getDifficultyKey(difficulty),
          version: difficulty.version,
          starRating: difficulty.starRating,
          partName: edgeMatch?.edge.partName ?? fallbackSample?.partName ?? "Edge",
          hitSoundFlags: getEdgeHitSoundFlags(edgeMatch),
          sample: edgeSample ?? fallbackSample,
          hasEdge: edgeMatch != null || fallbackSample != null,
        };
      });
  }, [crosshair, getDifficultyKey, hitsoundLayers, orderedDifficulties, visibilityByDifficulty]);

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
            />
          ) : (
            <Text size="xs" c="dimmed">
              No edge hitsound nearby
            </Text>
          )}
        </Stack>
      ))}
    </Stack>
  );
}

export function TimelineCrosshairFloatingPanel({
  boundsRef,
  crosshair,
  orderedDifficulties,
  visibilityByDifficulty,
  getDifficultyKey,
  hitsoundLayers,
  resetKey,
}: TimelineCrosshairFloatingPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const posRef = useRef<Point>(DEFAULT_POSITION);
  const dragRef = useRef<DragState | null>(null);
  const [position, setPosition] = useState(DEFAULT_POSITION);
  const [isDragging, setIsDragging] = useState(false);

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

  useEffect(() => {
    applyPosition(DEFAULT_POSITION);
  }, [applyPosition, resetKey]);

  useLayoutEffect(() => {
    applyPosition(posRef.current);
  }, [applyPosition, orderedDifficulties.length, resetKey]);

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

          <ScrollArea.Autosize mah={PANEL_BODY_MAX_HEIGHT} offsetScrollbars type="scroll">
            <Box p="xs" mx="sm" pt={8}>
              <TimelineCrosshairPanelBody
                crosshair={crosshair}
                orderedDifficulties={orderedDifficulties}
                visibilityByDifficulty={visibilityByDifficulty}
                getDifficultyKey={getDifficultyKey}
                hitsoundLayers={hitsoundLayers}
              />
            </Box>
          </ScrollArea.Autosize>
        </Paper>
      </Box>
    </Box>
  );
}
