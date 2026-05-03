import {
  ActionIcon,
  Alert,
  Badge,
  Box,
  Button,
  Flex,
  Group,
  LoadingOverlay,
  Paper,
  SegmentedControl,
  SimpleGrid,
  Slider,
  Stack,
  Table,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core';
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconEye,
  IconEyeOff,
  IconGripVertical,
  IconMinus,
  IconPlus,
} from '@tabler/icons-react';
import {
  Fragment,
  useEffect,
  useMemo,
  useRef,
  useState,
  type MouseEvent as ReactMouseEvent,
} from 'react';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import {
  type Mode,
  type ObjectsBreakPeriod,
  type ObjectsOverviewDifficulty,
  type ObjectsSnappingBucket,
  type ObjectsTimingSegment,
  type ObjectsTimelineObject,
} from '../../../Types';
import { formatGameModeLabel, getModeAccentColor } from '../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../common/AppTable.tsx';
import AutoResizeCanvas from '../../common/AutoResizeCanvas.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';

const LABEL_WIDTH = 208;
const ROW_HEIGHT = 44;
const HIDDEN_ROW_HEIGHT = 36;
const HIDDEN_ROW_VERTICAL_PADDING = 6;
const AXIS_HEIGHT = 30;
const MIN_ZOOM = 1;
const MAX_ZOOM = 24;
const MAX_AXIS_PRECISION_ZOOM = 6;
const TIMING_SAMPLES_PER_BEAT = 48;
const MIN_TIMELINE_WIDTH = 1400;
const MAX_TIMELINE_WIDTH = 100000;
const MAX_TIMELINE_CANVAS_TILE_WIDTH = 4096;
const CIRCLE_OBJECT_RADIUS = 16.0;
const TAIKO_CIRCLE_RADIUS = 12.0;
const TAIKO_FINISHER_CIRCLE_RADIUS = 16.0;
const TAIKO_SPINNER_RADIUS = 10.0;
const REVERSE_ARROW_ICON_SIZE = 7.0;
const TAIKO_DRUMROLL_COLOR = 'rgb(252,191,31)';
const TAIKO_SPINNER_COLOR = 'rgb(125,135,150)';
const MODE_ORDER: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];
const TIMELINE_INTERVAL_STEPS_MS = [
  1000, 2000, 3000, 5000, 10000, 15000, 30000, 60000, 120000, 300000,
];

type ObjectsModeGroup = {
  mode: Mode;
  difficulties: ObjectsOverviewDifficulty[];
};

type DifficultyDropIndicator = {
  key: string;
  position: 'before' | 'after';
};

interface ObjectsOverviewProps {
  reloadFlag: number;
}

function ObjectsOverview({ reloadFlag }: ObjectsOverviewProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();
  const { data, isLoading, isError, error, refetch } = useObjectsAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    refetch();
  }, [reloadFlag]);

  const groupedDifficulties = useMemo<ObjectsModeGroup[]>(() => {
    if (!data?.success) return [];

    const grouped = new Map<Mode, ObjectsOverviewDifficulty[]>();
    for (const difficulty of data.difficulties) {
      const mode = normalizeMode(difficulty.mode);
      const modeDifficulties = grouped.get(mode);

      if (modeDifficulties) {
        modeDifficulties.push(difficulty);
      } else {
        grouped.set(mode, [difficulty]);
      }
    }

    return MODE_ORDER.filter((mode) => grouped.has(mode)).map((mode) => ({
      mode,
      difficulties: grouped.get(mode) ?? [],
    }));
  }, [data]);

  useEffect(() => {
    if (groupedDifficulties.length === 0) {
      setSelectedMode(undefined);
      return;
    }

    if (!selectedMode || !groupedDifficulties.some((group) => group.mode === selectedMode)) {
      setSelectedMode(groupedDifficulties[0].mode);
    }
  }, [groupedDifficulties, selectedMode]);

  const selectedGroup =
    groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0];

  const summary = useMemo(() => {
    if (!data?.success) return null;

    return data.difficulties.reduce(
      (accumulator, difficulty) => {
        accumulator.objectCount += difficulty.objectCount;
        accumulator.edgeCount += difficulty.edgeCount;
        accumulator.unsnappedCount += difficulty.unsnappedCount;
        return accumulator;
      },
      { objectCount: 0, edgeCount: 0, unsnappedCount: 0 }
    );
  }, [data]);

  if (!folder) {
    return (
      <Alert
        icon={<IconAlertTriangle />}
        color="yellow"
        title="No beatmapset selected"
        withCloseButton
      >
        <Text size="sm">Select a beatmapset from the sidebar to analyze objects.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />

      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing objects">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>
                {error.stackTrace}
              </Text>
            )}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert icon={<IconAlertTriangle />} color="yellow" title="Analysis failed">
            <Text size="sm">{data.errorMessage}</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && summary && (
        <Flex gap="md" p="md" direction="column">
          <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
            <SummaryCard label="Difficulties" value={String(data.difficulties.length)} />
            <SummaryCard label="Hit objects" value={summary.objectCount.toLocaleString()} />
            <SummaryCard
              label="Timeline range"
              value={`${formatTime(data.startTimeMs)} → ${formatTime(data.endTimeMs)}`}
              subValue={`${formatDuration(data.endTimeMs - data.startTimeMs)} total`}
            />
          </SimpleGrid>

          <ObjectsTimelineComparison
            startTimeMs={data.startTimeMs}
            endTimeMs={data.endTimeMs}
            groupedDifficulties={groupedDifficulties}
            difficulties={selectedGroup?.difficulties ?? []}
            selectedMode={selectedGroup?.mode}
            onModeChange={setSelectedMode}
          />
          <SnappingsOverview
            groupedDifficulties={groupedDifficulties}
            totalUnsnappedCount={summary.unsnappedCount}
            totalEdgeCount={summary.edgeCount}
          />
        </Flex>
      )}
    </Box>
  );
}

function SummaryCard({
  label,
  value,
  subValue,
}: {
  label: string;
  value: string;
  subValue?: string;
}) {
  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase">
          {label}
        </Text>
        <Text fw={700} size="lg">
          {value}
        </Text>
        {subValue && (
          <Text size="xs" c="dimmed">
            {subValue}
          </Text>
        )}
      </Stack>
    </Paper>
  );
}

function ObjectsTimelineComparison({
  startTimeMs,
  endTimeMs,
  groupedDifficulties,
  difficulties,
  selectedMode,
  onModeChange,
}: {
  startTimeMs: number;
  endTimeMs: number;
  groupedDifficulties: ObjectsModeGroup[];
  difficulties: ObjectsOverviewDifficulty[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}) {
  const theme = useMantineTheme();
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const rowElementsRef = useRef<Partial<Record<string, HTMLDivElement | null>>>({});
  const dragState = useRef({ isDragging: false, startX: 0, scrollLeft: 0 });
  const dropIndicatorRef = useRef<DifficultyDropIndicator | null>(null);
  const [zoom, setZoom] = useState(8.0);
  const [isDragging, setIsDragging] = useState(false);
  const [visibilityByDifficulty, setVisibilityByDifficulty] = useState<Record<string, boolean>>({});
  const [difficultyOrderByMode, setDifficultyOrderByMode] = useState<
    Partial<Record<Mode, string[]>>
  >({});
  const [draggedDifficultyKey, setDraggedDifficultyKey] = useState<string | null>(null);
  const [dropIndicator, setDropIndicator] = useState<DifficultyDropIndicator | null>(null);

  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const tickIntervalMs = getTimelineIntervalMs(durationMs, zoom);
  const timelineWidth = Math.max(
    MIN_TIMELINE_WIDTH,
    Math.min(MAX_TIMELINE_WIDTH, Math.round(durationMs * 0.02 * zoom))
  );
  const contentWidth = timelineWidth + LABEL_WIDTH;
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;

  useEffect(() => {
    const allDifficultyKeys = groupedDifficulties.flatMap((group) =>
      group.difficulties.map(getDifficultyKey)
    );

    setVisibilityByDifficulty((current) => {
      let changed = false;
      const next = { ...current };

      for (const key of allDifficultyKeys) {
        if (!(key in next)) {
          next[key] = true;
          changed = true;
        }
      }

      return changed ? next : current;
    });
  }, [groupedDifficulties]);

  useEffect(() => {
    setDifficultyOrderByMode((current) => {
      let changed = false;
      const next: Partial<Record<Mode, string[]>> = { ...current };
      const availableModes = new Set(groupedDifficulties.map((group) => group.mode));

      for (const mode of Object.keys(next) as Mode[]) {
        if (!availableModes.has(mode)) {
          delete next[mode];
          changed = true;
        }
      }

      for (const group of groupedDifficulties) {
        const currentKeys = group.difficulties.map(getDifficultyKey);
        const existingOrder = next[group.mode] ?? [];
        const preservedOrder = existingOrder.filter((key) => currentKeys.includes(key));
        const missingKeys = currentKeys.filter((key) => !preservedOrder.includes(key));
        const mergedOrder = [...preservedOrder, ...missingKeys];

        if (!areStringArraysEqual(existingOrder, mergedOrder)) {
          next[group.mode] = mergedOrder;
          changed = true;
        }
      }

      return changed ? next : current;
    });
  }, [groupedDifficulties]);

  const orderedDifficulties = useMemo(() => {
    const difficultyMap = new Map(
      difficulties.map((difficulty) => [getDifficultyKey(difficulty), difficulty])
    );
    const currentOrder = activeMode ? difficultyOrderByMode[activeMode] : undefined;
    const fallbackOrder = difficulties.map(getDifficultyKey);
    const resolvedOrder = currentOrder && currentOrder.length > 0 ? currentOrder : fallbackOrder;
    const ordered = resolvedOrder
      .map((key) => difficultyMap.get(key))
      .filter((difficulty): difficulty is ObjectsOverviewDifficulty => difficulty !== undefined);
    const orderedKeys = new Set(ordered.map(getDifficultyKey));
    const missing = difficulties.filter(
      (difficulty) => !orderedKeys.has(getDifficultyKey(difficulty))
    );

    return [...ordered, ...missing];
  }, [activeMode, difficulties, difficultyOrderByMode]);

  useEffect(() => {
    dropIndicatorRef.current = dropIndicator;
  }, [dropIndicator]);

  const visibleCount = orderedDifficulties.filter(
    (difficulty) => visibilityByDifficulty[getDifficultyKey(difficulty)] !== false
  ).length;
  const allVisible = orderedDifficulties.length > 0 && visibleCount === orderedDifficulties.length;
  const allHidden = orderedDifficulties.length > 0 && visibleCount === 0;

  const handleMouseDown = (event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container) return;

    const target = event.target as HTMLElement | null;
    if (target?.closest('[data-stop-timeline-pan="true"]')) {
      return;
    }

    dragState.current = {
      isDragging: true,
      startX: event.clientX,
      scrollLeft: container.scrollLeft,
    };
    setIsDragging(true);
  };

  const handleMouseMove = (event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container || !dragState.current.isDragging) return;

    const deltaX = event.clientX - dragState.current.startX;
    container.scrollLeft = dragState.current.scrollLeft - deltaX;
  };

  const stopDragging = () => {
    dragState.current.isDragging = false;
    setIsDragging(false);
  };

  const setSelectedDifficultyVisibility = (visible: boolean) => {
    setVisibilityByDifficulty((current) => {
      const next = { ...current };

      for (const difficulty of orderedDifficulties) {
        next[getDifficultyKey(difficulty)] = visible;
      }

      return next;
    });
  };

  const toggleDifficultyVisibility = (difficulty: ObjectsOverviewDifficulty) => {
    const difficultyKey = getDifficultyKey(difficulty);

    setVisibilityByDifficulty((current) => ({
      ...current,
      [difficultyKey]: current[difficultyKey] === false,
    }));
  };

  const adjustZoom = (direction: -1 | 1) => {
    setZoom((value) => clampZoom(value + getZoomStep(value) * direction));
  };

  const reorderDifficulties = (
    sourceKey: string,
    targetKey: string,
    position: 'before' | 'after'
  ) => {
    if (!activeMode) return;

    setDifficultyOrderByMode((current) => {
      const currentOrder = current[activeMode] ?? orderedDifficulties.map(getDifficultyKey);
      const nextOrder = reorderDifficultyKeys(currentOrder, sourceKey, targetKey, position);

      if (areStringArraysEqual(currentOrder, nextOrder)) {
        return current;
      }

      return {
        ...current,
        [activeMode]: nextOrder,
      };
    });
  };

  useEffect(() => {
    if (!draggedDifficultyKey) {
      return;
    }

    const orderedKeys = orderedDifficulties.map(getDifficultyKey);

    const updateDropIndicator = (clientY: number) => {
      const nextIndicator = getDifficultyDropIndicator(
        clientY,
        orderedKeys,
        rowElementsRef.current
      );
      setDropIndicator((current) => {
        if (current?.key === nextIndicator?.key && current?.position === nextIndicator?.position) {
          return current;
        }

        return nextIndicator;
      });
    };

    const handleWindowMouseMove = (event: MouseEvent) => {
      updateDropIndicator(event.clientY);
    };

    const handleWindowMouseUp = () => {
      if (dropIndicatorRef.current) {
        reorderDifficulties(
          draggedDifficultyKey,
          dropIndicatorRef.current.key,
          dropIndicatorRef.current.position
        );
      }

      setDraggedDifficultyKey(null);
      setDropIndicator(null);
    };

    window.addEventListener('mousemove', handleWindowMouseMove);
    window.addEventListener('mouseup', handleWindowMouseUp);
    document.body.style.userSelect = 'none';

    return () => {
      window.removeEventListener('mousemove', handleWindowMouseMove);
      window.removeEventListener('mouseup', handleWindowMouseUp);
      document.body.style.userSelect = '';
    };
  }, [draggedDifficultyKey, orderedDifficulties]);

  const handleDifficultyReorderMouseDown = (
    event: ReactMouseEvent<HTMLDivElement>,
    difficultyKey: string
  ) => {
    event.preventDefault();
    event.stopPropagation();
    stopDragging();
    setDraggedDifficultyKey(difficultyKey);

    const initialIndicator = getDifficultyDropIndicator(
      event.clientY,
      orderedDifficulties.map(getDifficultyKey),
      rowElementsRef.current
    );
    setDropIndicator(initialIndicator);
  };

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Timeline comparison</Title>
            <Text size="sm" c="dimmed">
              Drag the grip to reorder rows. Drag horizontally in the timeline to pan.
            </Text>
          </Stack>
          <Group gap="sm" align="center" wrap="wrap" justify="flex-end">
            <ObjectsGameModeSelector
              groupedDifficulties={groupedDifficulties}
              selectedMode={selectedMode}
              onModeChange={onModeChange}
            />
            <Group gap="xs" align="center" wrap="nowrap">
              <Button
                variant="default"
                size="xs"
                leftSection={<IconEye size={14} />}
                disabled={orderedDifficulties.length === 0 || allVisible}
                onClick={() => setSelectedDifficultyVisibility(true)}
              >
                Show all
              </Button>
              <Button
                variant="default"
                size="xs"
                leftSection={<IconEyeOff size={14} />}
                disabled={orderedDifficulties.length === 0 || allHidden}
                onClick={() => setSelectedDifficultyVisibility(false)}
              >
                Hide all
              </Button>
            </Group>
            <Group gap="xs" align="center" wrap="nowrap">
              <ActionIcon variant="default" onClick={() => adjustZoom(-1)}>
                <IconMinus size={16} />
              </ActionIcon>
              <Box miw={220}>
                <Slider
                  value={zoom}
                  min={MIN_ZOOM}
                  max={MAX_ZOOM}
                  step={1.0}
                  onChange={setZoom}
                  label={(value) => `${formatZoom(value)}x`}
                />
              </Box>
              <ActionIcon variant="default" onClick={() => adjustZoom(1)}>
                <IconPlus size={16} />
              </ActionIcon>
            </Group>
          </Group>
        </Group>
        <Box
          ref={scrollRef}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={stopDragging}
          onMouseLeave={stopDragging}
          style={{
            overflowX: 'auto',
            overflowY: 'hidden',
            cursor: isDragging ? 'grabbing' : 'grab',
            userSelect: 'none',
          }}
        >
          <Stack gap="xs" style={{ width: contentWidth, minWidth: contentWidth }}>
            <TimelineAxisRow
              startTimeMs={startTimeMs}
              endTimeMs={endTimeMs}
              timelineWidth={timelineWidth}
              tickIntervalMs={tickIntervalMs}
              linePosition="bottom"
            />

            {orderedDifficulties.length === 0 && (
              <Paper p="md" radius="md" withBorder>
                <Text size="sm" c="dimmed">
                  No difficulties available for the selected mode.
                </Text>
              </Paper>
            )}

            {orderedDifficulties.map((difficulty) => {
              const difficultyKey = getDifficultyKey(difficulty);
              const isVisible = visibilityByDifficulty[difficultyKey] !== false;
              const rowHeight = isVisible ? ROW_HEIGHT : HIDDEN_ROW_HEIGHT;
              const showDropIndicator = dropIndicator?.key === difficultyKey;
              const dropIndicatorColor = theme.colors.blue[4];
              const dropLineShadow = `0 0 0 1px ${withAlpha(dropIndicatorColor, 0.8)}, 0 0 12px ${withAlpha(dropIndicatorColor, 0.45)}`;

              return (
                <Box
                  key={difficultyKey}
                  ref={(element) => {
                    rowElementsRef.current[difficultyKey] = element;
                  }}
                  style={{
                    position: 'relative',
                    display: 'flex',
                    alignItems: 'stretch',
                    width: contentWidth,
                    minWidth: contentWidth,
                    height: rowHeight,
                    borderRadius: theme.radius.sm,
                    background: showDropIndicator ? withAlpha(dropIndicatorColor, 0.08) : undefined,
                    opacity: draggedDifficultyKey === difficultyKey ? 0.72 : 1,
                  }}
                >
                  {showDropIndicator && (
                    <>
                      <Box
                        style={{
                          pointerEvents: 'none',
                          position: 'absolute',
                          left: 0,
                          right: 0,
                          height: 4,
                          zIndex: 6,
                          borderRadius: 999,
                          background: dropIndicatorColor,
                          boxShadow: dropLineShadow,
                          top: dropIndicator.position === 'before' ? -3 : undefined,
                          bottom: dropIndicator.position === 'after' ? -3 : undefined,
                        }}
                      />
                      <Box
                        style={{
                          pointerEvents: 'none',
                          position: 'absolute',
                          left: 8,
                          width: 10,
                          height: 10,
                          zIndex: 7,
                          borderRadius: '50%',
                          background: dropIndicatorColor,
                          boxShadow: dropLineShadow,
                          top: dropIndicator.position === 'before' ? -6 : undefined,
                          bottom: dropIndicator.position === 'after' ? -6 : undefined,
                        }}
                      />
                    </>
                  )}
                  <Box
                    style={{
                      position: 'sticky',
                      left: 0,
                      zIndex: 2,
                      display: 'flex',
                      alignItems: 'center',
                      flex: `0 0 ${LABEL_WIDTH}px`,
                      height: rowHeight,
                      paddingInline: theme.spacing.xs,
                      background: isVisible ? theme.colors.dark[8] : theme.colors.dark[7],
                      borderRight: `1px solid ${theme.colors.dark[4]}`,
                      boxShadow: '8px 0 16px rgba(0, 0, 0, 0.18)',
                      boxSizing: 'border-box',
                      overflow: 'hidden',
                      outline: showDropIndicator
                        ? `1px solid ${withAlpha(dropIndicatorColor, 0.55)}`
                        : undefined,
                    }}
                  >
                    <Flex
                      align="center"
                      gap={8}
                      style={{ width: '100%', minWidth: 0, overflow: 'hidden' }}
                    >
                      <Box
                        aria-label={`Reorder ${difficulty.version}`}
                        data-stop-timeline-pan="true"
                        onMouseDown={(event) =>
                          handleDifficultyReorderMouseDown(event, difficultyKey)
                        }
                        style={{
                          flex: '0 0 auto',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          width: 22,
                          height: 22,
                          borderRadius: theme.radius.sm,
                          color: theme.colors.gray[4],
                          cursor: draggedDifficultyKey === difficultyKey ? 'grabbing' : 'grab',
                        }}
                      >
                        <IconGripVertical size={16} />
                      </Box>
                      <Group
                        gap={8}
                        wrap="nowrap"
                        style={{
                          flex: 1,
                          minWidth: 0,
                          maxWidth: '100%',
                          overflow: 'hidden',
                          opacity: isVisible ? 1 : 0.7,
                        }}
                      >
                        <GameModeIcon
                          mode={normalizeMode(difficulty.mode)}
                          size={18}
                          color={getModeAccentColor(normalizeMode(difficulty.mode))}
                        />
                        <Stack gap={0} style={{ flex: 1, minWidth: 0, overflow: 'hidden' }}>
                          <Text fw={600} size="sm" truncate style={{ width: '100%', minWidth: 0 }}>
                            {difficulty.version}
                          </Text>
                          <Text
                            size="xs"
                            c="dimmed"
                            truncate
                            style={{ width: '100%', minWidth: 0 }}
                          >
                            {isVisible ? formatGameModeLabel(difficulty.mode) : 'Hidden'}
                          </Text>
                        </Stack>
                      </Group>
                      <ActionIcon
                        variant="subtle"
                        color={isVisible ? 'blue' : 'gray'}
                        aria-label={
                          isVisible ? `Hide ${difficulty.version}` : `Show ${difficulty.version}`
                        }
                        data-stop-timeline-pan="true"
                        onMouseDown={(event) => event.stopPropagation()}
                        onClick={() => toggleDifficultyVisibility(difficulty)}
                        style={{ flex: '0 0 auto' }}
                      >
                        {isVisible ? <IconEye size={16} /> : <IconEyeOff size={16} />}
                      </ActionIcon>
                    </Flex>
                  </Box>
                  <Box
                    h={rowHeight}
                    style={{
                      flex: `0 0 ${timelineWidth}px`,
                      minWidth: timelineWidth,
                      width: timelineWidth,
                      borderRadius: theme.radius.sm,
                      overflow: 'hidden',
                      border: `1px solid ${showDropIndicator ? withAlpha(dropIndicatorColor, 0.75) : theme.colors.dark[4]}`,
                      boxShadow: showDropIndicator
                        ? `0 0 0 1px ${withAlpha(dropIndicatorColor, 0.35)} inset`
                        : undefined,
                      boxSizing: 'border-box',
                    }}
                  >
                    {isVisible ? (
                      <TimelineRow
                        difficulty={difficulty}
                        startTimeMs={startTimeMs}
                        endTimeMs={endTimeMs}
                        width={timelineWidth}
                        height={ROW_HEIGHT}
                      />
                    ) : (
                      <Flex
                        h="100%"
                        px="sm"
                        py={HIDDEN_ROW_VERTICAL_PADDING}
                        align="center"
                        style={{ boxSizing: 'border-box' }}
                      >
                        <Text size="xs" c="dimmed" lh={1.2}>
                          Timeline hidden for this difficulty.
                        </Text>
                      </Flex>
                    )}
                  </Box>
                </Box>
              );
            })}

            {orderedDifficulties.length > 0 && (
              <TimelineAxisRow
                startTimeMs={startTimeMs}
                endTimeMs={endTimeMs}
                timelineWidth={timelineWidth}
                tickIntervalMs={tickIntervalMs}
                linePosition="top"
              />
            )}
          </Stack>
        </Box>
      </Stack>
    </Paper>
  );
}

function ObjectsGameModeSelector({
  groupedDifficulties,
  selectedMode,
  onModeChange,
}: {
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
}) {
  if (groupedDifficulties.length <= 1) {
    return null;
  }

  return (
    <Group ml="auto" w="unset" gap="md" align="center">
      <SegmentedControl
        radius="md"
        p="xs"
        data={groupedDifficulties.map((group) => ({
          label: (
            <Flex gap="xs" align="center">
              <GameModeIcon mode={group.mode} size={22} color="currentColor" />
              <Text size="xs" fw={600}>
                {group.difficulties.length}
              </Text>
            </Flex>
          ),
          value: group.mode,
        }))}
        value={selectedMode}
        onChange={(value) => onModeChange(value as Mode)}
        fullWidth={false}
      />
    </Group>
  );
}

function TimelineAxisRow({
  startTimeMs,
  endTimeMs,
  timelineWidth,
  tickIntervalMs,
  linePosition,
}: {
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  tickIntervalMs: number;
  linePosition: 'top' | 'bottom';
}) {
  const theme = useMantineTheme();

  return (
    <Box
      style={{
        display: 'flex',
        alignItems: 'stretch',
        width: LABEL_WIDTH + timelineWidth,
        minWidth: LABEL_WIDTH + timelineWidth,
        height: AXIS_HEIGHT,
      }}
    >
      <Box
        style={{
          position: 'sticky',
          left: 0,
          zIndex: 3,
          flex: `0 0 ${LABEL_WIDTH}px`,
          height: AXIS_HEIGHT,
          background: theme.colors.dark[8],
          borderRight: `1px solid ${theme.colors.dark[4]}`,
          boxSizing: 'border-box',
        }}
      />
      <Box
        style={{ flex: `0 0 ${timelineWidth}px`, minWidth: timelineWidth, width: timelineWidth }}
      >
        <TimelineAxis
          startTimeMs={startTimeMs}
          endTimeMs={endTimeMs}
          width={timelineWidth}
          tickIntervalMs={tickIntervalMs}
          linePosition={linePosition}
        />
      </Box>
    </Box>
  );
}

function TimelineAxis({
  startTimeMs,
  endTimeMs,
  width,
  tickIntervalMs,
  linePosition,
}: {
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  tickIntervalMs: number;
  linePosition: 'top' | 'bottom';
}) {
  const theme = useMantineTheme();
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const firstTickMs = Math.ceil(startTimeMs / tickIntervalMs) * tickIntervalMs;
  const ticks: number[] = [];
  const lineY = linePosition === 'bottom' ? AXIS_HEIGHT - 1.5 : 1.5;
  const tickEndY = linePosition === 'bottom' ? lineY - 8 : lineY + 8;
  const labelY = linePosition === 'bottom' ? 11 : AXIS_HEIGHT - 9;

  for (let tick = firstTickMs; tick <= endTimeMs; tick += tickIntervalMs) {
    ticks.push(tick);
  }

  return (
    <Box style={{ position: 'relative', width, height: AXIS_HEIGHT }}>
      <svg width={width} height={AXIS_HEIGHT} style={{ display: 'block' }}>
        <line
          x1={0}
          y1={lineY}
          x2={width}
          y2={lineY}
          stroke={theme.colors.dark[4]}
          strokeWidth={1}
        />
        {ticks.map((tick) => {
          const x = getAlignedTimelineLineX(tick, startTimeMs, durationMs, width);
          return (
            <g key={tick}>
              <line
                x1={x}
                y1={lineY}
                x2={x}
                y2={tickEndY}
                stroke={theme.colors.dark[3]}
                strokeWidth={1}
              />
              <text
                x={x}
                y={labelY}
                fill={theme.colors.dark[2]}
                fontSize="10"
                textAnchor="middle"
                dominantBaseline="middle"
                fontFamily={theme.fontFamily}
              >
                {formatTime(tick)}
              </text>
            </g>
          );
        })}
      </svg>
    </Box>
  );
}

function TimelineRow({
  difficulty,
  startTimeMs,
  endTimeMs,
  width,
  height,
}: {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  height: number;
}) {
  const theme = useMantineTheme();
  const canvasTiles = useMemo(() => getTimelineCanvasTiles(width), [width]);

  return (
    <Box style={{ position: 'relative', width, height, overflow: 'hidden' }}>
      {canvasTiles.map((tile) => (
        <Box
          key={tile.startX}
          style={{
            position: 'absolute',
            left: tile.startX,
            top: 0,
            width: tile.width,
            height,
          }}
        >
          <AutoResizeCanvas
            fixedWidth={tile.width}
            fixedHeight={height}
            draw={(ctx) => {
              drawTimelineRow(ctx, {
                difficulty,
                startTimeMs,
                endTimeMs,
                timelineWidth: width,
                viewportStartX: tile.startX,
                viewportWidth: tile.width,
                height,
                theme,
              });
            }}
          />
        </Box>
      ))}
    </Box>
  );
}

function SnappingsOverview({
  groupedDifficulties,
  totalUnsnappedCount,
  totalEdgeCount,
}: {
  groupedDifficulties: ObjectsModeGroup[];
  totalUnsnappedCount: number;
  totalEdgeCount: number;
}) {
  const theme = useMantineTheme();
  const totalUnsnappedPercentage =
    totalEdgeCount > 0 ? (totalUnsnappedCount * 100) / totalEdgeCount : 0;
  const difficulties = groupedDifficulties.flatMap((group) => group.difficulties);
  const snappingColumns = getSnappingColumns(difficulties);

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Snapping overview</Title>
            <Text size="sm" c="dimmed">
              Counts are based on object edge times, including slider reverses and tails.
            </Text>
          </Stack>
          <Badge color={totalUnsnappedCount > 0 ? 'yellow' : 'green'} variant="light">
            Unsnapped: {totalUnsnappedCount.toLocaleString()} ({totalUnsnappedPercentage.toFixed(1)}
            %)
          </Badge>
        </Group>
        <AppTable>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              <Table.Th>Mode</Table.Th>
              <Table.Th style={{ textAlign: 'center' }}>Objects</Table.Th>
              <Table.Th style={{ textAlign: 'center' }}>Edges</Table.Th>
              {snappingColumns.map((column) => (
                <Table.Th key={column.label} style={{ textAlign: 'center' }}>
                  {column.label}
                </Table.Th>
              ))}
              <Table.Th style={{ textAlign: 'center' }}>Unsnapped</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {groupedDifficulties.map((group) => (
              <Fragment key={group.mode}>
                {group.difficulties.map((difficulty) => (
                  <Table.Tr key={`${group.mode}-${difficulty.version}`}>
                    <DifficultyTableCell>
                      <Group gap="xs" wrap="nowrap">
                        <GameModeIcon
                          mode={group.mode}
                          size={16}
                          color={getModeAccentColor(group.mode)}
                        />
                        <Text size="sm" fw={600}>
                          {difficulty.version}
                        </Text>
                      </Group>
                    </DifficultyTableCell>
                    <Table.Td>
                      <Group gap={6} wrap="nowrap" justify="center">
                        <GameModeIcon
                          mode={group.mode}
                          size={16}
                          color={getModeAccentColor(group.mode)}
                        />
                        <Text size="sm">{formatGameModeLabel(group.mode)}</Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{difficulty.objectCount.toLocaleString()}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{difficulty.edgeCount.toLocaleString()}</Text>
                    </Table.Td>
                    {snappingColumns.map((column) => {
                      const bucket = difficulty.snappings.find(
                        (candidate) => candidate.label === column.label
                      );
                      return (
                        <Table.Td key={`${difficulty.mode}-${difficulty.version}-${column.label}`}>
                          <SnappingTableValue
                            count={bucket?.count ?? 0}
                            percentage={bucket?.percentage ?? 0}
                          />
                        </Table.Td>
                      );
                    })}
                    <Table.Td>
                      <Group justify="center" wrap="nowrap">
                        <SnappingStatusBadge
                          count={difficulty.unsnappedCount}
                          percentage={difficulty.unsnappedPercentage}
                        />
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Fragment>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}

function SnappingTableValue({ count, percentage }: { count: number; percentage: number }) {
  return (
    <Stack gap={0}>
      <Text size="sm" fw={600} c={count === 0 ? 'dimmed' : undefined}>
        {count.toLocaleString()}
      </Text>
      <Text size="xs" c="dimmed">
        {percentage.toFixed(1)}%
      </Text>
    </Stack>
  );
}

function SnappingStatusBadge({ count, percentage }: { count: number; percentage: number }) {
  return (
    <Badge color={count > 0 ? 'yellow' : 'green'} variant="light">
      {count.toLocaleString()} ({percentage.toFixed(1)}%)
    </Badge>
  );
}

function drawTimelineRow(
  ctx: CanvasRenderingContext2D,
  {
    difficulty,
    startTimeMs,
    endTimeMs,
    timelineWidth,
    viewportStartX,
    viewportWidth,
    height,
    theme,
  }: {
    difficulty: ObjectsOverviewDifficulty;
    startTimeMs: number;
    endTimeMs: number;
    timelineWidth: number;
    viewportStartX: number;
    viewportWidth: number;
    height: number;
    theme: ReturnType<typeof useMantineTheme>;
  }
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const centerY = height / 2;
  const viewportEndX = viewportStartX + viewportWidth;

  ctx.clearRect(0, 0, viewportWidth, height);
  ctx.save();
  ctx.beginPath();
  ctx.rect(0, 0, viewportWidth, height);
  ctx.clip();
  ctx.translate(-viewportStartX, 0);

  ctx.fillStyle = theme.colors.dark[7];
  ctx.fillRect(viewportStartX, 0, viewportWidth, height);

  drawBreakPeriods(ctx, {
    breakPeriods: difficulty.breakPeriods,
    startTimeMs,
    durationMs,
    width: timelineWidth,
    visibleStartX: viewportStartX,
    visibleEndX: viewportEndX,
    height,
    theme,
  });

  drawTimingGrid(ctx, {
    timingSegments: difficulty.timingSegments,
    timelineObjects: difficulty.timelineObjects,
    startTimeMs,
    endTimeMs,
    durationMs,
    width: timelineWidth,
    visibleStartX: viewportStartX,
    visibleEndX: viewportEndX,
    height,
  });

  ctx.strokeStyle = withAlpha(theme.colors.dark[2], 0.35);
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(viewportStartX, centerY);
  ctx.lineTo(viewportEndX, centerY);
  ctx.stroke();

  const difficultyMode = normalizeMode(difficulty.mode);

  for (const timelineObject of difficulty.timelineObjects) {
    drawTimelineObject(
      ctx,
      timelineObject,
      difficultyMode,
      startTimeMs,
      durationMs,
      timelineWidth,
      centerY,
      viewportStartX,
      viewportEndX
    );
  }

  const drawnMarkers = new Set<string>();
  for (const timelineObject of difficulty.timelineObjects) {
    if (timelineObject.objectType === 'Circle') {
      continue;
    }

    for (const edge of timelineObject.edges) {
      const rawX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      if (rawX < viewportStartX || rawX > viewportEndX) continue;

      const x = getAlignedTimelineLineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      const markerKey = `${timelineObject.objectType}-${edge.partName}-${x}`;
      if (drawnMarkers.has(markerKey)) continue;

      drawnMarkers.add(markerKey);
      drawObjectMarker(ctx, timelineObject, difficultyMode, edge.partName, x, centerY);
    }
  }

  ctx.restore();
}

function drawBreakPeriods(
  ctx: CanvasRenderingContext2D,
  {
    breakPeriods,
    startTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
    theme,
  }: {
    breakPeriods: ObjectsBreakPeriod[];
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
    theme: ReturnType<typeof useMantineTheme>;
  }
) {
  if (breakPeriods.length === 0) {
    return;
  }

  const blockY = 4;
  const blockHeight = Math.max(0, height - 8);

  for (const breakPeriod of breakPeriods) {
    if (breakPeriod.endTimeMs <= breakPeriod.startTimeMs) {
      continue;
    }

    const startX = getTimelineX(breakPeriod.startTimeMs, startTimeMs, durationMs, width);
    const endX = getTimelineX(breakPeriod.endTimeMs, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 12);
    if (!bounds) {
      continue;
    }

    const blockWidth = bounds.endX - bounds.startX;

    ctx.save();
    ctx.fillStyle = withAlpha(theme.colors.yellow[7], 0.08);
    ctx.fillRect(bounds.startX, blockY, blockWidth, blockHeight);

    ctx.setLineDash([5, 4]);
    ctx.strokeStyle = withAlpha(theme.colors.yellow[4], 0.45);
    ctx.lineWidth = 1;
    ctx.strokeRect(
      bounds.startX + 0.5,
      blockY + 0.5,
      Math.max(0, blockWidth - 1),
      Math.max(0, blockHeight - 1)
    );
    ctx.setLineDash([]);

    if (blockWidth >= 20) {
      drawBreakPauseIcon(ctx, bounds.startX + blockWidth / 2, height / 2, theme.colors.yellow[2]);
    }

    ctx.restore();
  }
}

function drawTimingGrid(
  ctx: CanvasRenderingContext2D,
  {
    timingSegments,
    timelineObjects,
    startTimeMs,
    endTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
  }: {
    timingSegments: ObjectsTimingSegment[];
    timelineObjects: ObjectsTimelineObject[];
    startTimeMs: number;
    endTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
  }
) {
  if (timingSegments.length === 0) {
    return;
  }

  const roundedEdgeTimes = new Set(
    timelineObjects.flatMap((timelineObject) =>
      timelineObject.edges.map((edge) => Math.round(edge.timeMs))
    )
  );
  const tickLines = new Map<
    string,
    { x: number; color: string; height: number; alpha: number; priority: number }
  >();

  for (const segment of timingSegments) {
    const visibleStartMs = Math.max(startTimeMs, segment.startTimeMs);
    const visibleEndMs = Math.min(endTimeMs, segment.endTimeMs);
    const sampleStepMs = segment.msPerBeat / TIMING_SAMPLES_PER_BEAT;

    if (visibleEndMs <= visibleStartMs || sampleStepMs <= 0) {
      continue;
    }

    const startSampleIndex = Math.max(
      0,
      Math.ceil((visibleStartMs - segment.offsetMs) / sampleStepMs)
    );
    const endSampleIndex = Math.floor((visibleEndMs - segment.offsetMs) / sampleStepMs);

    for (let sampleIndex = startSampleIndex; sampleIndex <= endSampleIndex; sampleIndex += 1) {
      const sampleTimeMs = segment.offsetMs + sampleIndex * sampleStepMs;
      const rawX = getTimelineX(sampleTimeMs, startTimeMs, durationMs, width);
      if (rawX < visibleStartX || rawX > visibleEndX) {
        continue;
      }

      const hasNearbyEdge = hasNearbyRoundedEdge(roundedEdgeTimes, sampleTimeMs);
      const tickStyle = getTimingTickStyle(sampleIndex, segment.meter, hasNearbyEdge);
      if (!tickStyle) {
        continue;
      }

      const x = getAlignedTimelineLineX(sampleTimeMs, startTimeMs, durationMs, width);
      const key = x.toFixed(1);
      const existing = tickLines.get(key);

      if (!existing || tickStyle.priority >= existing.priority) {
        tickLines.set(key, { x, ...tickStyle });
      }
    }
  }

  const tickBottomY = height - 6;
  for (const tickLine of Array.from(tickLines.values()).sort((left, right) => left.x - right.x)) {
    ctx.strokeStyle = withAlpha(tickLine.color, tickLine.alpha);
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(tickLine.x, tickBottomY - tickLine.height);
    ctx.lineTo(tickLine.x, tickBottomY);
    ctx.stroke();
  }
}

function drawTimelineObject(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number
) {
  const color = getTimelineObjectColor(difficultyMode, timelineObject);
  const circleRadius = getTimelineObjectCircleRadius(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Circle') {
    const x = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - (circleRadius + 2) || x > visibleEndX + circleRadius + 2) {
      return;
    }

    drawCircleObject(ctx, x, centerY, color, circleRadius);
    return;
  }

  drawObjectBody(
    ctx,
    timelineObject,
    difficultyMode,
    startTimeMs,
    durationMs,
    width,
    centerY,
    visibleStartX,
    visibleEndX
  );
}

function drawObjectBody(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number
) {
  if (timelineObject.endTimeMs <= timelineObject.startTimeMs) return;

  const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
  const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, width);
  const bodyBounds = getObjectBodyWidth(
    startX,
    endX,
    visibleStartX,
    visibleEndX,
    timelineObject.objectType === 'Spinner' ? 12 : 8
  );
  if (!bodyBounds) return;

  const color = getTimelineObjectColor(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    ctx.strokeStyle = withAlpha(color, 0.18);
    ctx.lineWidth = 32;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(bodyBounds.startX, centerY);
    ctx.lineTo(bodyBounds.endX, centerY);
    ctx.stroke();

    ctx.strokeStyle = withAlpha(color, 0.45);
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(bodyBounds.startX, centerY);
    ctx.lineTo(bodyBounds.endX, centerY);
    ctx.stroke();
    return;
  }

  if (timelineObject.objectType === 'Hold note') {
    ctx.fillStyle = withAlpha(color, 0.42);
    ctx.fillRect(bodyBounds.startX, centerY - 5, bodyBounds.endX - bodyBounds.startX, 10);
    ctx.strokeStyle = withAlpha(color, 0.85);
    ctx.lineWidth = 1.5;
    ctx.strokeRect(bodyBounds.startX, centerY - 5, bodyBounds.endX - bodyBounds.startX, 10);
    return;
  }

  ctx.strokeStyle = withAlpha(color, 0.4);
  ctx.lineWidth = 32;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(bodyBounds.startX, centerY);
  ctx.lineTo(bodyBounds.endX, centerY);
  ctx.stroke();

  ctx.strokeStyle = withAlpha(color, 0.95);
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(bodyBounds.startX, centerY);
  ctx.lineTo(bodyBounds.endX, centerY);
  ctx.stroke();
}

function drawObjectMarker(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  partName: string,
  x: number,
  centerY: number
) {
  const lowerPart = partName.toLowerCase();
  const color = getTimelineObjectColor(difficultyMode, timelineObject);
  const isSlider = timelineObject.objectType === 'Slider';
  const circleRadius = getTimelineObjectCircleRadius(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    drawSpinnerIcon(ctx, x, centerY, color, getSpinnerMarkerRadius(difficultyMode));
    return;
  }

  ctx.strokeStyle = withAlpha(color, 0.95);
  ctx.fillStyle = withAlpha(color, 0.95);
  ctx.lineWidth = 1.5;

  if (lowerPart.includes('reverse')) {
    drawCircleObject(ctx, x, centerY, color, circleRadius);
    drawReverseArrowIcon(ctx, x, centerY);
    return;
  }

  if (lowerPart.includes('tail') || lowerPart.includes('end')) {
    if (isSlider) {
      drawCircleObject(ctx, x, centerY, color, circleRadius);
      return;
    }

    ctx.fillRect(x - 3, centerY - 3, 6, 6);
    return;
  }

  drawCircleObject(ctx, x, centerY, color, circleRadius);
}

function getTimelineIntervalMs(durationMs: number, zoom: number) {
  const baseIntervalMs = getAdaptiveBaseIntervalMs(durationMs);
  const normalizedZoom = Math.min(zoom, MAX_AXIS_PRECISION_ZOOM);
  const zoomProgress = Math.max(
    0,
    Math.min(1, (normalizedZoom - MIN_ZOOM) / (MAX_AXIS_PRECISION_ZOOM - MIN_ZOOM))
  );

  if (zoomProgress >= 1) {
    return 1000;
  }

  const scaledIntervalMs = baseIntervalMs * Math.pow(1000 / baseIntervalMs, zoomProgress);
  return getNextTimelineIntervalStep(Math.max(1000, scaledIntervalMs));
}

function getAdaptiveBaseIntervalMs(durationMs: number) {
  const durationSeconds = durationMs / 1000;
  if (durationSeconds <= 30) return 5000;
  if (durationSeconds <= 60) return 10000;
  if (durationSeconds <= 120) return 15000;
  if (durationSeconds <= 300) return 30000;
  if (durationSeconds <= 600) return 60000;
  if (durationSeconds <= 1800) return 120000;
  return 300000;
}

function getNextTimelineIntervalStep(targetIntervalMs: number) {
  return (
    TIMELINE_INTERVAL_STEPS_MS.find((step) => step >= targetIntervalMs) ??
    TIMELINE_INTERVAL_STEPS_MS[TIMELINE_INTERVAL_STEPS_MS.length - 1]
  );
}

function getTimelineX(timeMs: number, startTimeMs: number, durationMs: number, width: number) {
  return ((timeMs - startTimeMs) / durationMs) * width;
}

function getAlignedTimelineLineX(
  timeMs: number,
  startTimeMs: number,
  durationMs: number,
  width: number
) {
  return Math.round(getTimelineX(timeMs, startTimeMs, durationMs, width)) + 0.5;
}

function getDifficultyKey(difficulty: ObjectsOverviewDifficulty) {
  return `${normalizeMode(difficulty.mode)}::${difficulty.version}`;
}

function areStringArraysEqual(left: string[], right: string[]) {
  if (left.length !== right.length) {
    return false;
  }

  for (let index = 0; index < left.length; index += 1) {
    if (left[index] !== right[index]) {
      return false;
    }
  }

  return true;
}

function reorderDifficultyKeys(
  keys: string[],
  sourceKey: string,
  targetKey: string,
  position: 'before' | 'after'
) {
  if (sourceKey === targetKey) {
    return keys;
  }

  if (!keys.includes(sourceKey) || !keys.includes(targetKey)) {
    return keys;
  }

  const next = keys.filter((key) => key !== sourceKey);
  let targetIndex = next.indexOf(targetKey);

  if (targetIndex === -1) {
    return keys;
  }

  if (position === 'after') {
    targetIndex += 1;
  }

  next.splice(targetIndex, 0, sourceKey);
  return next;
}

function getTimelineObjectCircleRadius(
  difficultyMode: Mode,
  timelineObject: ObjectsTimelineObject
) {
  if (difficultyMode === 'Taiko' && timelineObject.hasFinishHitSound) {
    return TAIKO_FINISHER_CIRCLE_RADIUS;
  }

  return TAIKO_CIRCLE_RADIUS;
}

function getSpinnerMarkerRadius(difficultyMode: Mode) {
  if (difficultyMode === 'Taiko') {
    return TAIKO_SPINNER_RADIUS;
  }

  return CIRCLE_OBJECT_RADIUS - 1.5;
}

function getDifficultyDropIndicator(
  clientY: number,
  orderedKeys: string[],
  rowElements: Partial<Record<string, HTMLDivElement | null>>
): DifficultyDropIndicator | null {
  for (const key of orderedKeys) {
    const element = rowElements[key];
    if (!element) {
      continue;
    }

    const bounds = element.getBoundingClientRect();
    const midpointY = bounds.top + bounds.height / 2;

    if (clientY < midpointY) {
      return { key, position: 'before' };
    }
  }

  const lastKey = orderedKeys[orderedKeys.length - 1];
  return lastKey ? { key: lastKey, position: 'after' } : null;
}

function getObjectBodyWidth(
  startX: number,
  endX: number,
  visibleStartX: number,
  visibleEndX: number,
  minimumWidth: number
) {
  const centerX = (startX + endX) / 2;
  let adjustedStartX = startX;
  let adjustedEndX = endX;

  if (adjustedEndX - adjustedStartX < minimumWidth) {
    adjustedStartX = centerX - minimumWidth / 2;
    adjustedEndX = centerX + minimumWidth / 2;
  }

  if (adjustedEndX <= visibleStartX || adjustedStartX >= visibleEndX) {
    return null;
  }

  return {
    startX: Math.max(visibleStartX, adjustedStartX),
    endX: Math.min(visibleEndX, adjustedEndX),
  };
}

function getTimelineCanvasTiles(width: number) {
  const tiles: Array<{ startX: number; width: number }> = [];

  for (let startX = 0; startX < width; startX += MAX_TIMELINE_CANVAS_TILE_WIDTH) {
    tiles.push({
      startX,
      width: Math.min(MAX_TIMELINE_CANVAS_TILE_WIDTH, width - startX),
    });
  }

  return tiles;
}

function getTimingTickStyle(sampleIndex: number, meter: number, hasNearbyEdge: boolean) {
  const shouldShowTick =
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 4) === 0 ||
    (hasNearbyEdge &&
      (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 12) === 0 ||
        sampleIndex % (TIMING_SAMPLES_PER_BEAT / 16) === 0));

  if (!shouldShowTick) {
    return null;
  }

  const safeMeter = Math.max(1, meter || 1);
  const baseHeight = hasNearbyEdge ? 6 : 3;

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT * safeMeter) === 0) {
    return { color: 'rgb(255,255,255)', height: 12, alpha: 0.5, priority: 8 };
  }

  if (sampleIndex % TIMING_SAMPLES_PER_BEAT === 0) {
    return { color: 'rgb(255,255,255)', height: 6, alpha: 0.5, priority: 7 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 2) === 0) {
    return { color: 'rgb(255,120,100)', height: baseHeight, alpha: 0.5, priority: 6 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 3) === 0) {
    return { color: 'rgb(255,100,225)', height: baseHeight, alpha: 0.5, priority: 5 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 4) === 0) {
    return { color: 'rgb(120,175,255)', height: baseHeight, alpha: 0.5, priority: 4 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 6) === 0) {
    return { color: 'rgb(200,150,255)', height: baseHeight, alpha: 0.5, priority: 3 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 8) === 0) {
    return { color: 'rgb(255,225,100)', height: baseHeight, alpha: 0.5, priority: 2 };
  }

  if (
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 12) === 0 ||
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 16) === 0
  ) {
    return { color: 'rgb(125,135,150)', height: baseHeight, alpha: 0.5, priority: 1 };
  }

  return { color: 'rgb(255,0,0)', height: 12, alpha: 0.5, priority: 0 };
}

function getZoomStep(zoom: number) {
  if (zoom >= 12) return 2;
  if (zoom >= 6) return 1;
  return 0.5;
}

function clampZoom(zoom: number) {
  return Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, Math.round(zoom * 100) / 100));
}

function formatZoom(zoom: number) {
  return Number.isInteger(zoom)
    ? zoom.toFixed(0)
    : zoom.toFixed(2).replace(/0+$/, '').replace(/\.$/, '');
}

function hasNearbyRoundedEdge(roundedEdgeTimes: Set<number>, timeMs: number) {
  const roundedTimeMs = Math.round(timeMs);

  for (let delta = -2; delta <= 2; delta += 1) {
    if (roundedEdgeTimes.has(roundedTimeMs + delta)) {
      return true;
    }
  }

  return false;
}

function drawCircleObject(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string,
  radius: number
) {
  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.55);
  ctx.strokeStyle = withAlpha(color, 0.98);
  ctx.lineWidth = 1.75;
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
}

function drawReverseArrowIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  size = REVERSE_ARROW_ICON_SIZE
) {
  const leftX = x - size;
  const middleX = x - size * 0.17;
  const rightX = x + size * 0.67;
  const yOffset = size * 0.89;

  ctx.save();
  ctx.strokeStyle = 'rgba(255,255,255,0.95)';
  ctx.lineWidth = Math.max(1.4, size * 0.36);
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';

  ctx.beginPath();
  ctx.moveTo(leftX, centerY - yOffset);
  ctx.lineTo(middleX, centerY);
  ctx.lineTo(leftX, centerY + yOffset);
  ctx.stroke();

  ctx.beginPath();
  ctx.moveTo(middleX, centerY - yOffset);
  ctx.lineTo(rightX, centerY);
  ctx.lineTo(middleX, centerY + yOffset);
  ctx.stroke();

  ctx.restore();
}

function drawSpinnerIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string,
  radius: number
) {
  const ringRadius = Math.max(radius * 0.72, radius - 4);
  const accentX = x + ringRadius * 0.55;
  const accentY = centerY - ringRadius * 0.6;

  ctx.save();
  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.22);
  ctx.strokeStyle = withAlpha(color, 0.9);
  ctx.lineWidth = Math.max(1.25, radius * 0.1);
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();

  ctx.beginPath();
  ctx.strokeStyle = 'rgba(255,255,255,0.95)';
  ctx.lineWidth = Math.max(1.75, radius * 0.16);
  ctx.lineCap = 'round';
  ctx.arc(x, centerY, ringRadius, -Math.PI * 0.35, Math.PI * 1.15);
  ctx.stroke();

  ctx.beginPath();
  ctx.fillStyle = 'rgba(255,255,255,0.95)';
  ctx.arc(accentX, accentY, Math.max(1.4, radius * 0.12), 0, Math.PI * 2);
  ctx.fill();

  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.92);
  ctx.arc(x, centerY, Math.max(1.6, radius * 0.15), 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function drawBreakPauseIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string
) {
  ctx.save();
  ctx.fillStyle = withAlpha(color, 0.9);
  ctx.fillRect(x - 4, centerY - 5, 2.5, 10);
  ctx.fillRect(x + 1.5, centerY - 5, 2.5, 10);
  ctx.restore();
}

function getTimelineObjectColor(difficultyMode: Mode, timelineObject: ObjectsTimelineObject) {
  if (difficultyMode === 'Taiko') {
    if (timelineObject.objectType === 'Spinner') {
      return TAIKO_SPINNER_COLOR;
    }

    if (timelineObject.objectType === 'Slider') {
      return TAIKO_DRUMROLL_COLOR;
    }
  }

  return timelineObject.comboColourHex ?? '#ced4da';
}

function withAlpha(color: string, alpha: number) {
  const safeAlpha = Math.max(0, Math.min(1, alpha));

  if (color.startsWith('#')) {
    const normalized =
      color.length === 4
        ? `#${color[1]}${color[1]}${color[2]}${color[2]}${color[3]}${color[3]}`
        : color;
    const red = Number.parseInt(normalized.slice(1, 3), 16);
    const green = Number.parseInt(normalized.slice(3, 5), 16);
    const blue = Number.parseInt(normalized.slice(5, 7), 16);
    return `rgba(${red}, ${green}, ${blue}, ${safeAlpha})`;
  }

  if (color.startsWith('rgba(')) {
    return color.replace(/rgba\(([^)]+),\s*[^,]+\)$/, `rgba($1, ${safeAlpha})`);
  }

  if (color.startsWith('rgb(')) {
    return color.replace('rgb(', 'rgba(').replace(')', `, ${safeAlpha})`);
  }

  return color;
}

function getSnappingColumns(difficulties: ObjectsOverviewDifficulty[]) {
  const columnMap = new Map<string, ObjectsSnappingBucket>();

  for (const difficulty of difficulties) {
    for (const bucket of difficulty.snappings) {
      const existing = columnMap.get(bucket.label);
      if (!existing || bucket.divisor < existing.divisor) {
        columnMap.set(bucket.label, bucket);
      }
    }
  }

  return Array.from(columnMap.values()).sort((left, right) => left.divisor - right.divisor);
}

function normalizeMode(mode: string): Mode {
  return MODE_ORDER.includes(mode as Mode) ? (mode as Mode) : 'Standard';
}

function formatDuration(durationMs: number) {
  const safeDuration = Math.max(0, durationMs);
  const totalSeconds = Math.floor(safeDuration / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

function formatTime(timeMs: number) {
  const absoluteMs = Math.abs(timeMs);
  const totalSeconds = Math.floor(absoluteMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  const sign = timeMs < 0 ? '-' : '';
  return `${sign}${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export default ObjectsOverview;
