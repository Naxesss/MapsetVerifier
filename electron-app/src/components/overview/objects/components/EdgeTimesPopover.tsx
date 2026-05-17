import {
  Badge,
  Box,
  Group,
  Popover,
  ScrollArea,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { type ReactNode, useMemo, useState } from 'react';
import OsuLink from '../../../common/OsuLink.tsx';
import { formatEditorTimestamp, lookupEdgePartName } from '../timelineUtils.ts';

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

export function EdgeTimesPopover({
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
