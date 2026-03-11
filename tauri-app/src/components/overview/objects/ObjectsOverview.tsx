import {
  ActionIcon,
  Alert,
  Badge,
  Box,
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
import { IconMinus, IconPlus } from '@tabler/icons-react';
import { Fragment, useEffect, useMemo, useRef, useState, type MouseEvent as ReactMouseEvent } from 'react';
import AutoResizeCanvas from '../../common/AutoResizeCanvas.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import {
  type Mode,
  type ObjectsOverviewDifficulty,
  type ObjectsSnappingBucket,
  type ObjectsTimelineObject,
} from '../../../Types';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';

const LABEL_WIDTH = 176;
const ROW_HEIGHT = 44;
const AXIS_HEIGHT = 30;
const MIN_ZOOM = 1;
const MAX_ZOOM = 6;
const MODE_ORDER: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];
const TIMELINE_INTERVAL_STEPS_MS = [1000, 2000, 3000, 5000, 10000, 15000, 30000, 60000, 120000, 300000];

type ObjectsModeGroup = {
  mode: Mode;
  difficulties: ObjectsOverviewDifficulty[];
};

function ObjectsOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();
  const { data, isLoading, isError, error } = useObjectsAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

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

  const selectedGroup = groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0];

  const summary = useMemo(() => {
    if (!data?.success) return null;

    return data.difficulties.reduce(
      (accumulator, difficulty) => {
        accumulator.objectCount += difficulty.objectCount;
        accumulator.edgeCount += difficulty.edgeCount;
        accumulator.unsnappedCount += difficulty.unsnappedCount;
        return accumulator;
      },
      { objectCount: 0, edgeCount: 0, unsnappedCount: 0 },
    );
  }, [data]);

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to analyze objects.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />

      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error analyzing objects" withCloseButton>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{error?.message}</Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>{error.stackTrace}</Text>
            )}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert color="yellow" title="Analysis failed">
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

function SummaryCard({ label, value, subValue }: { label: string; value: string; subValue?: string }) {
  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase">{label}</Text>
        <Text fw={700} size="lg">{value}</Text>
        {subValue && <Text size="xs" c="dimmed">{subValue}</Text>}
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
  const dragState = useRef({ isDragging: false, startX: 0, scrollLeft: 0 });
  const [zoom, setZoom] = useState(1.5);
  const [isDragging, setIsDragging] = useState(false);

  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const tickIntervalMs = getTimelineIntervalMs(durationMs, zoom);
  const timelineWidth = Math.max(1400, Math.min(22000, Math.round(durationMs * 0.02 * zoom)));
  const contentWidth = timelineWidth + LABEL_WIDTH;

  const handleMouseDown = (event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container) return;

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

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Timeline comparison</Title>
            <Text size="sm" c="dimmed">Drag horizontally to pan. Use zoom to inspect dense patterns across difficulties.</Text>
          </Stack>
          <Group gap="sm" align="center" wrap="wrap" justify="flex-end">
            <ObjectsGameModeSelector
              groupedDifficulties={groupedDifficulties}
              selectedMode={selectedMode}
              onModeChange={onModeChange}
            />
            <Group gap="xs" align="center" wrap="nowrap">
              <ActionIcon variant="default" onClick={() => setZoom((value) => Math.max(MIN_ZOOM, value - 0.5))}>
                <IconMinus size={16} />
              </ActionIcon>
              <Box miw={180}>
                <Slider value={zoom} min={MIN_ZOOM} max={MAX_ZOOM} step={0.5} onChange={setZoom} label={(value) => `${value.toFixed(1)}x`} />
              </Box>
              <ActionIcon variant="default" onClick={() => setZoom((value) => Math.min(MAX_ZOOM, value + 0.5))}>
                <IconPlus size={16} />
              </ActionIcon>
            </Group>
          </Group>
        </Group>

        <Group gap="xs">
          {['Circle', 'Slider', 'Spinner', 'Hold note'].map((objectType) => (
            <Badge key={objectType} variant="light" color={getBadgeColor(objectType)}>
              {objectType}
            </Badge>
          ))}
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

            {difficulties.length === 0 && (
              <Paper p="md" radius="md" withBorder>
                <Text size="sm" c="dimmed">No difficulties available for the selected mode.</Text>
              </Paper>
            )}

            {difficulties.map((difficulty) => (
              <Box
                key={`${difficulty.mode}-${difficulty.version}`}
                style={{
                  display: 'flex',
                  alignItems: 'stretch',
                  width: contentWidth,
                  minWidth: contentWidth,
                  height: ROW_HEIGHT,
                }}
              >
                <Box
                  style={{
                    position: 'sticky',
                    left: 0,
                    zIndex: 2,
                    display: 'flex',
                    alignItems: 'center',
                    flex: `0 0 ${LABEL_WIDTH}px`,
                    height: ROW_HEIGHT,
                    paddingInline: theme.spacing.xs,
                    background: theme.colors.dark[8],
                    borderRight: `1px solid ${theme.colors.dark[4]}`,
                    boxShadow: '8px 0 16px rgba(0, 0, 0, 0.18)',
                    boxSizing: 'border-box',
                  }}
                >
                  <Group gap={8} wrap="nowrap">
                    <GameModeIcon
                      mode={normalizeMode(difficulty.mode)}
                      size={18}
                      color={getModeAccentColor(normalizeMode(difficulty.mode), theme)}
                    />
                    <Stack gap={0} maw={LABEL_WIDTH - 44} miw={0}>
                      <Text fw={600} size="sm" truncate>{difficulty.version}</Text>
                      <Text size="xs" c="dimmed">{difficulty.mode}</Text>
                    </Stack>
                  </Group>
                </Box>
                <Box
                  h={ROW_HEIGHT}
                  style={{
                    flex: `0 0 ${timelineWidth}px`,
                    minWidth: timelineWidth,
                    width: timelineWidth,
                    borderRadius: theme.radius.sm,
                    overflow: 'hidden',
                    border: `1px solid ${theme.colors.dark[4]}`,
                    boxSizing: 'border-box',
                  }}
                >
                  <TimelineRow
                    difficulty={difficulty}
                    startTimeMs={startTimeMs}
                    endTimeMs={endTimeMs}
                    width={timelineWidth}
                    height={ROW_HEIGHT}
                    tickIntervalMs={tickIntervalMs}
                  />
                </Box>
              </Box>
            ))}

            {difficulties.length > 0 && (
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
              <Text size="xs" fw={600}>{group.difficulties.length}</Text>
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
      <Box style={{ flex: `0 0 ${timelineWidth}px`, minWidth: timelineWidth, width: timelineWidth }}>
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
        <line x1={0} y1={lineY} x2={width} y2={lineY} stroke={theme.colors.dark[4]} strokeWidth={1} />
        {ticks.map((tick) => {
          const x = getAlignedTimelineLineX(tick, startTimeMs, durationMs, width);
          return (
            <g key={tick}>
              <line x1={x} y1={lineY} x2={x} y2={tickEndY} stroke={theme.colors.dark[3]} strokeWidth={1} />
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
  tickIntervalMs,
}: {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  height: number;
  tickIntervalMs: number;
}) {
  const theme = useMantineTheme();

  return (
    <AutoResizeCanvas
      fixedWidth={width}
      fixedHeight={height}
      draw={(ctx) => {
        drawTimelineRow(ctx, {
          difficulty,
          startTimeMs,
          endTimeMs,
          width,
          height,
          tickIntervalMs,
          theme,
        });
      }}
    />
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
  const totalUnsnappedPercentage = totalEdgeCount > 0 ? (totalUnsnappedCount * 100) / totalEdgeCount : 0;
  const difficulties = groupedDifficulties.flatMap((group) => group.difficulties);
  const snappingColumns = getSnappingColumns(difficulties);
  const totalColumns = 5 + snappingColumns.length;

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Snapping overview</Title>
            <Text size="sm" c="dimmed">Counts are based on object edge times, including slider reverses and tails.</Text>
          </Stack>
          <Badge color={totalUnsnappedCount > 0 ? 'yellow' : 'green'} variant="light">
            Unsnapped: {totalUnsnappedCount.toLocaleString()} ({totalUnsnappedPercentage.toFixed(1)}%)
          </Badge>
        </Group>

        <Box style={{ overflowX: 'auto' }}>
          <Table
            striped
            highlightOnHover
            withTableBorder
            withColumnBorders
            horizontalSpacing="sm"
            verticalSpacing="xs"
            miw={Math.max(960, 520 + snappingColumns.length * 88)}
          >
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Difficulty</Table.Th>
                <Table.Th>Mode</Table.Th>
                <Table.Th>Objects</Table.Th>
                <Table.Th>Edges</Table.Th>
                {snappingColumns.map((column) => (
                  <Table.Th key={column.label}>{column.label}</Table.Th>
                ))}
                <Table.Th>Unsnapped</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {groupedDifficulties.map((group) => (
                <Fragment key={group.mode}>
                  {group.difficulties.map((difficulty) => (
                    <Table.Tr key={`${group.mode}-${difficulty.version}`}>
                      <Table.Td>
                        <Group gap="xs" wrap="nowrap">
                          <GameModeIcon
                            mode={group.mode}
                            size={16}
                            color={getModeAccentColor(group.mode)}
                          />
                          <Text size="sm" fw={600}>{difficulty.version}</Text>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Group gap={6} wrap="nowrap">
                          <GameModeIcon
                            mode={group.mode}
                            size={16}
                            color={getModeAccentColor(group.mode)}
                          />
                          <Text size="sm">{group.mode}</Text>
                        </Group>
                      </Table.Td>
                      <Table.Td><Text size="sm">{difficulty.objectCount.toLocaleString()}</Text></Table.Td>
                      <Table.Td><Text size="sm">{difficulty.edgeCount.toLocaleString()}</Text></Table.Td>
                      {snappingColumns.map((column) => {
                        const bucket = difficulty.snappings.find((candidate) => candidate.label === column.label);
                        return (
                          <Table.Td key={`${difficulty.mode}-${difficulty.version}-${column.label}`}>
                            <SnappingTableValue count={bucket?.count ?? 0} percentage={bucket?.percentage ?? 0} />
                          </Table.Td>
                        );
                      })}
                      <Table.Td>
                        <SnappingStatusBadge
                          count={difficulty.unsnappedCount}
                          percentage={difficulty.unsnappedPercentage}
                        />
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Fragment>
              ))}
            </Table.Tbody>
          </Table>
        </Box>
      </Stack>
    </Paper>
  );
}

function SnappingTableValue({
  count,
  percentage,
}: {
  count: number;
  percentage: number;
}) {
  return (
    <Stack gap={0} align="flex-end">
      <Text size="sm" fw={600}>{count.toLocaleString()}</Text>
      <Text size="xs" c="dimmed">{percentage.toFixed(1)}%</Text>
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
    width,
    height,
    tickIntervalMs,
    theme,
  }: {
    difficulty: ObjectsOverviewDifficulty;
    startTimeMs: number;
    endTimeMs: number;
    width: number;
    height: number;
    tickIntervalMs: number;
    theme: ReturnType<typeof useMantineTheme>;
  },
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const centerY = height / 2;
  const firstTickMs = Math.ceil(startTimeMs / tickIntervalMs) * tickIntervalMs;

  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = theme.colors.dark[7];
  ctx.fillRect(0, 0, width, height);

  for (let tick = firstTickMs; tick <= endTimeMs; tick += tickIntervalMs) {
    const x = getAlignedTimelineLineX(tick, startTimeMs, durationMs, width);
    ctx.strokeStyle = theme.colors.dark[5];
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
    ctx.stroke();
  }

  ctx.strokeStyle = theme.colors.dark[4];
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(0, centerY);
  ctx.lineTo(width, centerY);
  ctx.stroke();

  for (const timelineObject of difficulty.timelineObjects) {
    drawObjectBody(ctx, timelineObject, startTimeMs, durationMs, width, centerY);
  }

  const drawnMarkers = new Set<string>();
  for (const timelineObject of difficulty.timelineObjects) {
    for (const edge of timelineObject.edges) {
      const rawX = getTimelineX(edge.timeMs, startTimeMs, durationMs, width);
      if (rawX < 0 || rawX > width) continue;

      const x = getAlignedTimelineLineX(edge.timeMs, startTimeMs, durationMs, width);
      const markerKey = `${timelineObject.objectType}-${edge.partName}-${x}`;
      if (drawnMarkers.has(markerKey)) continue;

      drawnMarkers.add(markerKey);
      drawObjectMarker(ctx, timelineObject.objectType, edge.partName, x, centerY);
    }
  }
}

function drawObjectBody(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
) {
  if (timelineObject.endTimeMs <= timelineObject.startTimeMs) return;

  const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
  const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, width);
  if (endX <= 0 || startX >= width || endX - startX < 1.5) return;

  ctx.strokeStyle = `${getCanvasColor(timelineObject.objectType)}66`;
  ctx.lineWidth = timelineObject.objectType === 'Spinner' || timelineObject.objectType === 'Hold note' ? 10 : 6;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(startX, centerY);
  ctx.lineTo(endX, centerY);
  ctx.stroke();
}

function drawObjectMarker(
  ctx: CanvasRenderingContext2D,
  objectType: string,
  partName: string,
  x: number,
  centerY: number,
) {
  const lowerPart = partName.toLowerCase();
  const color = getCanvasColor(objectType);
  const top = lowerPart.includes('reverse') ? centerY - 8 : centerY - 12;
  const bottom = lowerPart.includes('reverse') ? centerY + 8 : centerY + 12;

  ctx.strokeStyle = color;
  ctx.fillStyle = color;
  ctx.lineWidth = lowerPart.includes('reverse') ? 2 : 3;
  ctx.beginPath();
  ctx.moveTo(x, top);
  ctx.lineTo(x, bottom);
  ctx.stroke();

  if (lowerPart.includes('head') || objectType === 'Circle') {
    ctx.beginPath();
    ctx.arc(x, centerY, objectType === 'Circle' ? 3.5 : 2.5, 0, Math.PI * 2);
    ctx.fill();
  }

  if (lowerPart.includes('tail')) {
    ctx.fillRect(x - 2, centerY - 2, 4, 4);
  }
}

function getTimelineIntervalMs(durationMs: number, zoom: number) {
  const baseIntervalMs = getAdaptiveBaseIntervalMs(durationMs);
  const zoomProgress = Math.max(0, Math.min(1, (zoom - MIN_ZOOM) / (MAX_ZOOM - MIN_ZOOM)));

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
  return TIMELINE_INTERVAL_STEPS_MS.find((step) => step >= targetIntervalMs) ?? TIMELINE_INTERVAL_STEPS_MS[TIMELINE_INTERVAL_STEPS_MS.length - 1];
}

function getTimelineX(timeMs: number, startTimeMs: number, durationMs: number, width: number) {
  return ((timeMs - startTimeMs) / durationMs) * width;
}

function getAlignedTimelineLineX(timeMs: number, startTimeMs: number, durationMs: number, width: number) {
  return Math.round(getTimelineX(timeMs, startTimeMs, durationMs, width)) + 0.5;
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

function getModeAccentColor(mode: Mode, theme?: ReturnType<typeof useMantineTheme>) {
  if (!theme) {
    switch (mode) {
      case 'Standard':
        return 'var(--mantine-color-pink-4)';
      case 'Taiko':
        return 'var(--mantine-color-red-4)';
      case 'Catch':
        return 'var(--mantine-color-lime-4)';
      case 'Mania':
        return 'var(--mantine-color-violet-4)';
      default:
        return 'var(--mantine-color-gray-4)';
    }
  }

  switch (mode) {
    case 'Standard':
      return theme.colors.pink[4];
    case 'Taiko':
      return theme.colors.red[4];
    case 'Catch':
      return theme.colors.lime[4];
    case 'Mania':
      return theme.colors.violet[4];
    default:
      return theme.colors.gray[4];
  }
}

function getBadgeColor(objectType: string) {
  switch (objectType) {
    case 'Circle':
      return 'blue';
    case 'Slider':
      return 'teal';
    case 'Spinner':
      return 'grape';
    case 'Hold note':
      return 'orange';
    default:
      return 'gray';
  }
}

function getCanvasColor(objectType: string) {
  switch (objectType) {
    case 'Circle':
      return '#4dabf7';
    case 'Slider':
      return '#38d9a9';
    case 'Spinner':
      return '#b197fc';
    case 'Hold note':
      return '#ffa94d';
    default:
      return '#ced4da';
  }
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
