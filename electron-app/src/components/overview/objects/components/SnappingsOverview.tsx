import {
  Badge,
  Box,
  Group,
  Paper,
  Popover,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
  UnstyledButton,
  useMantineTheme,
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { CSSProperties, Fragment, type ReactNode, useMemo, useState } from 'react';
import { formatGameModeLabel, getModeAccentColor } from '../../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import OsuLink from '../../../common/OsuLink.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import {
  buildRoundedEdgePartNameMap,
  formatEditorTimestamp,
  getSnappingColumns,
  lookupEdgePartName,
} from '../timelineUtils.ts';
import type {
  Mode,
  ObjectsOverviewDifficulty,
  ObjectsSnappingBucket,
} from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

const POPOVER_EDGE_LIST_MAX_HEIGHT_PX = 280;
const POPOVER_EDGE_LIST_APPROX_LINE_HEIGHT_PX = 22;
const POPOVER_DROPDOWN_MAX_WIDTH_PX = 380;

const BADGE_FILTER_TOOLTIP_LABEL = 'Click badges below to filter edge times by type';

/**
 * Builds a line `MM:SS:mmm - - edgeLabel` where the first ` -` is consumed by {@link OsuLink}
 * so the rendered line shows the timestamp link followed by ` - edgeLabel`.
 */
function buildOsuTimestampLinkTextWithEdgeTypes(
  entries: Array<{ timeMs: number; partName: string }>
) {
  if (entries.length === 0) return '';
  return [...entries]
    .sort((a, b) => a.timeMs - b.timeMs)
    .map(({ timeMs, partName }) => `${formatEditorTimestamp(timeMs)} - - ${partName}`)
    .join('\n');
}

function EdgeTimesPopover({
  headingLabel,
  difficultyVersion,
  timesMs,
  roundedEdgePartNameMap,
  children,
  fullWidth = true,
  hoverHighlightColor,
}: {
  /** Shown before the difficulty name, e.g. `1/4 snaps` or `Unsnapped objects`. */
  headingLabel: string;
  difficultyVersion: string;
  timesMs: number[];
  /** When omitted, timestamps are shown without edge types (legacy / empty timeline). */
  roundedEdgePartNameMap?: Map<number, string>;
  children: ReactNode;
  fullWidth?: boolean;
  hoverHighlightColor?: string;
}) {
  const [edgeTypeFilter, setEdgeTypeFilter] = useState<string | null>(null);

  const sortedEdges = useMemo(() => {
    if (timesMs.length === 0) {
      return [];
    }
    const sortedTimes = [...timesMs].sort((a, b) => a - b);
    if (!roundedEdgePartNameMap || roundedEdgePartNameMap.size === 0) {
      return sortedTimes.map((timeMs) => ({ timeMs, partName: 'Unknown' }));
    }
    return sortedTimes.map((timeMs) => ({
      timeMs,
      partName: lookupEdgePartName(roundedEdgePartNameMap, timeMs),
    }));
  }, [timesMs, roundedEdgePartNameMap]);

  const countsByType = useMemo(() => {
    const counts = new Map<string, number>();
    for (const { partName } of sortedEdges) {
      counts.set(partName, (counts.get(partName) ?? 0) + 1);
    }
    return counts;
  }, [sortedEdges]);

  const typeBadges = useMemo(() => {
    return Array.from(countsByType.entries()).sort((left, right) => {
      if (right[1] !== left[1]) {
        return right[1] - left[1];
      }
      return left[0].localeCompare(right[0]);
    });
  }, [countsByType]);

  const filteredEdges = useMemo(() => {
    if (!edgeTypeFilter) {
      return sortedEdges;
    }
    return sortedEdges.filter((entry) => entry.partName === edgeTypeFilter);
  }, [sortedEdges, edgeTypeFilter]);

  const linkText = useMemo(
    () => buildOsuTimestampLinkTextWithEdgeTypes(filteredEdges),
    [filteredEdges]
  );

  const scrollViewportMinHeight = useMemo(() => {
    const lines = sortedEdges.length;
    if (lines === 0) return undefined;
    return Math.min(
      POPOVER_EDGE_LIST_MAX_HEIGHT_PX,
      Math.max(
        POPOVER_EDGE_LIST_APPROX_LINE_HEIGHT_PX,
        lines * POPOVER_EDGE_LIST_APPROX_LINE_HEIGHT_PX
      )
    );
  }, [sortedEdges.length]);

  if (timesMs.length === 0) {
    return <>{children}</>;
  }

  return (
    <Popover
      position="top"
      withArrow
      shadow="md"
      trapFocus={false}
      styles={{
        dropdown: {
          maxWidth: POPOVER_DROPDOWN_MAX_WIDTH_PX,
          width: 'max-content',
        },
      }}
      onDismiss={() => setEdgeTypeFilter(null)}
    >
      <Popover.Target>
        <UnstyledButton
          type="button"
          p={0}
          style={{
            width: fullWidth ? '100%' : 'auto',
            display: fullWidth ? 'block' : 'inline-flex',
            textAlign: 'inherit',
            verticalAlign: 'inherit',
            borderRadius: fullWidth ? undefined : 12,
            transition: hoverHighlightColor ? 'background-color 120ms' : undefined,
          }}
          onMouseEnter={
            hoverHighlightColor
              ? (event) => {
                event.currentTarget.style.backgroundColor = hoverHighlightColor;
              }
              : undefined
          }
          onMouseLeave={
            hoverHighlightColor
              ? (event) => {
                event.currentTarget.style.backgroundColor = '';
              }
              : undefined
          }
        >
          {children}
        </UnstyledButton>
      </Popover.Target>
      <Popover.Dropdown>
        <Group gap={6} mb="xs" wrap="wrap" align="center">
          <Text size="xs" c="dimmed" fw={600} component="span">
            {headingLabel} ·{' '}
          </Text>
          <Group gap={4} wrap="nowrap" align="center">
            <Text size="xs" c="dimmed" fw={700} component="span">
              {difficultyVersion}
            </Text>
            <Tooltip label={BADGE_FILTER_TOOLTIP_LABEL}>
              <Box
                component="span"
                style={{ display: 'inline-flex', alignItems: 'center', cursor: 'help' }}
              >
                <IconInfoCircle size={14} color="var(--mantine-color-gray-6)" />
              </Box>
            </Tooltip>
          </Group>
        </Group>
        {typeBadges.length > 0 ? (
          <Group gap={6} mb="sm" wrap="wrap" align="flex-start">
            {typeBadges.map(([partName, count]) => {
              const isActive = edgeTypeFilter === partName;
              const canFilterByType = typeBadges.length > 1;
              return (
                <Badge
                  key={partName}
                  component={canFilterByType ? 'button' : 'span'}
                  type={canFilterByType ? 'button' : undefined}
                  variant={isActive ? 'filled' : 'light'}
                  color={isActive ? 'blue' : 'gray'}
                  size="sm"
                  style={{ cursor: canFilterByType ? 'pointer' : undefined }}
                  onClick={
                    canFilterByType
                      ? () =>
                        setEdgeTypeFilter((current) => (current === partName ? null : partName))
                      : undefined
                  }
                >
                  {partName}: {count.toLocaleString()}
                </Badge>
              );
            })}
          </Group>
        ) : null}
        <ScrollArea.Autosize
          mah={POPOVER_EDGE_LIST_MAX_HEIGHT_PX}
          type="auto"
          offsetScrollbars
          styles={{
            viewport: scrollViewportMinHeight ? { minHeight: scrollViewportMinHeight } : undefined,
          }}
        >
          <Text size="sm" component="div" style={{ whiteSpace: 'pre-wrap' }}>
            <OsuLink text={linkText} disableSeparators />
          </Text>
        </ScrollArea.Autosize>
      </Popover.Dropdown>
    </Popover>
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

function SnappingStatusBadge({
  count,
  percentage,
  style,
}: {
  count: number;
  percentage: number;
  style?: CSSProperties;
}) {
  return (
    <Badge color={count > 0 ? 'yellow' : 'green'} variant="light" style={style}>
      {count.toLocaleString()} ({percentage.toFixed(1)}%)
    </Badge>
  );
}

function SnappingDifficultyTableRow({
  mode,
  difficulty,
  snappingColumns,
  clickableCellHoverColor,
}: {
  mode: Mode;
  difficulty: ObjectsOverviewDifficulty;
  snappingColumns: ObjectsSnappingBucket[];
  clickableCellHoverColor: string;
}) {
  const roundedEdgePartNameMap = useMemo(
    () => buildRoundedEdgePartNameMap(difficulty),
    [difficulty]
  );

  return (
    <Table.Tr>
      <DifficultyTableCell>
        <Group gap="xs" wrap="nowrap">
          <GameModeIcon mode={mode} size={16} starRating={difficulty.starRating} />
          <Text size="sm" fw={600}>
            {difficulty.version}
          </Text>
        </Group>
      </DifficultyTableCell>
      <Table.Td>
        <Group gap={6} wrap="nowrap" justify="center">
          <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode)} />
          <Text size="sm">{formatGameModeLabel(mode)}</Text>
        </Group>
      </Table.Td>
      <Table.Td>
        <Text size="sm">{difficulty.objectCount.toLocaleString()}</Text>
      </Table.Td>
      <Table.Td>
        <Text size="sm">{difficulty.edgeCount.toLocaleString()}</Text>
      </Table.Td>
      {snappingColumns.map((column) => {
        const bucket = difficulty.snappings.find((candidate) => candidate.label === column.label);
        const timesMs = bucket?.edgeTimesMs ?? [];
        const count = bucket?.count ?? 0;
        const isClickable = timesMs.length > 0;
        return (
          <Table.Td
            key={`${difficulty.mode}-${difficulty.version}-${column.label}`}
            style={{
              cursor: isClickable ? 'pointer' : undefined,
              transition: isClickable ? 'background-color 120ms' : undefined,
            }}
            onMouseEnter={
              isClickable
                ? (event) => {
                  event.currentTarget.style.backgroundColor = clickableCellHoverColor;
                }
                : undefined
            }
            onMouseLeave={
              isClickable
                ? (event) => {
                  event.currentTarget.style.backgroundColor = '';
                }
                : undefined
            }
          >
            <EdgeTimesPopover
              headingLabel={`${column.label} snaps`}
              difficultyVersion={difficulty.version}
              timesMs={timesMs}
              roundedEdgePartNameMap={roundedEdgePartNameMap}
            >
              <SnappingTableValue count={count} percentage={bucket?.percentage ?? 0} />
            </EdgeTimesPopover>
          </Table.Td>
        );
      })}
      <Table.Td
        style={{
          cursor: (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0 ? 'pointer' : undefined,
        }}
      >
        <Group justify="center" wrap="nowrap">
          <EdgeTimesPopover
            headingLabel="Unsnapped objects"
            difficultyVersion={difficulty.version}
            timesMs={difficulty.unsnappedEdgeTimesMs ?? []}
            fullWidth={false}
            hoverHighlightColor={clickableCellHoverColor}
            roundedEdgePartNameMap={roundedEdgePartNameMap}
          >
            <SnappingStatusBadge
              count={difficulty.unsnappedCount}
              percentage={difficulty.unsnappedPercentage}
              style={{
                cursor:
                  (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0 ? 'pointer' : undefined,
              }}
            />
          </EdgeTimesPopover>
        </Group>
      </Table.Td>
    </Table.Tr>
  );
}

interface SnappingsOverviewProps {
  groupedDifficulties: ObjectsModeGroup[];
  totalUnsnappedCount: number;
  totalEdgeCount: number;
}

export default function SnappingsOverview({
  groupedDifficulties,
  totalUnsnappedCount,
  totalEdgeCount,
}: SnappingsOverviewProps) {
  const theme = useMantineTheme();
  const clickableCellHoverColor = theme.colors.dark[4];
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
              Click on a cell to see all timestamps for that snapping.
            </Text>
          </Stack>
          <Badge color={totalUnsnappedCount > 0 ? 'yellow' : 'green'} variant="light">
            Unsnapped: {totalUnsnappedCount.toLocaleString()} ({totalUnsnappedPercentage.toFixed(1)}
            %)
          </Badge>
        </Group>
        <AppTable highlightOnHover={false}>
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
                  <SnappingDifficultyTableRow
                    key={`${group.mode}-${difficulty.version}`}
                    mode={group.mode}
                    difficulty={difficulty}
                    snappingColumns={snappingColumns}
                    clickableCellHoverColor={clickableCellHoverColor}
                  />
                ))}
              </Fragment>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
