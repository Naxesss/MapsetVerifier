import {
  Badge,
  Group,
  Paper,
  Popover,
  ScrollArea,
  Stack,
  Table,
  Text,
  Title,
  UnstyledButton,
  useMantineTheme,
} from '@mantine/core';
import { CSSProperties, Fragment, type ReactNode, useMemo } from 'react';
import { formatGameModeLabel, getModeAccentColor } from '../../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import OsuLink from '../../../common/OsuLink.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import { formatEditorTimestamp, getSnappingColumns } from '../timelineUtils.ts';
import type { ObjectsModeGroup } from '../types.ts';

function buildOsuTimestampLinkText(timesMs: number[]) {
  if (timesMs.length === 0) return '';
  return [...timesMs]
    .sort((a, b) => a - b)
    .map((ms) => `${formatEditorTimestamp(ms)} -`)
    .join('\n');
}

function EdgeTimesPopover({
  title,
  timesMs,
  children,
  fullWidth = true,
  hoverHighlightColor,
}: {
  title: string;
  timesMs: number[];
  children: ReactNode;
  fullWidth?: boolean;
  hoverHighlightColor?: string;
}) {
  const linkText = useMemo(() => buildOsuTimestampLinkText(timesMs), [timesMs]);

  if (timesMs.length === 0) {
    return <>{children}</>;
  }

  return (
    <Popover position="bottom" withArrow shadow="md" trapFocus={false}>
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
        <Text size="xs" c="dimmed" fw={600} mb="xs">
          {title}
        </Text>
        <ScrollArea.Autosize mah={280} type="auto" offsetScrollbars>
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
                  <Table.Tr key={`${group.mode}-${difficulty.version}`}>
                    <DifficultyTableCell>
                      <Group gap="xs" wrap="nowrap">
                        <GameModeIcon
                          mode={group.mode}
                          size={16}
                          starRating={difficulty.starRating}
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
                                  event.currentTarget.style.backgroundColor =
                                    clickableCellHoverColor;
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
                            title={`${column.label} snaps · ${difficulty.version}`}
                            timesMs={timesMs}
                          >
                            <SnappingTableValue
                              count={count}
                              percentage={bucket?.percentage ?? 0}
                            />
                          </EdgeTimesPopover>
                        </Table.Td>
                      );
                    })}
                    <Table.Td
                      style={{
                        cursor:
                          (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0
                            ? 'pointer'
                            : undefined,
                      }}
                    >
                      <Group justify="center" wrap="nowrap">
                        <EdgeTimesPopover
                          title={`Unsnapped objects · ${difficulty.version}`}
                          timesMs={difficulty.unsnappedEdgeTimesMs ?? []}
                          fullWidth={false}
                          hoverHighlightColor={clickableCellHoverColor}
                        >
                          <SnappingStatusBadge
                            count={difficulty.unsnappedCount}
                            percentage={difficulty.unsnappedPercentage}
                            style={{
                              cursor:
                                (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0
                                  ? 'pointer'
                                  : undefined,
                            }}
                          />
                        </EdgeTimesPopover>
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
