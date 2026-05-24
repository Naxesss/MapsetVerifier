import { Badge, Group, Popover, ScrollArea, Text, UnstyledButton } from '@mantine/core';
import { type ReactNode, useMemo, useState } from 'react';
import { InfoIconTooltip } from '../../../common/InfoIconTooltip.tsx';
import OsuLink from '../../../common/OsuLink.tsx';
import { formatEditorTimestamp } from '../timelineUtils.ts';
import type { ObjectsTypeEntry } from '../../../../Types';

const POPOVER_LIST_MAX_HEIGHT_PX = 280;
const POPOVER_LIST_APPROX_LINE_HEIGHT_PX = 22;
const POPOVER_DROPDOWN_MAX_WIDTH_PX = 380;

const DETAIL_FILTER_TOOLTIP_LABEL = 'Click badges below to filter objects by type';

function buildOsuTimestampLinkText(entries: ObjectsTypeEntry[]) {
  if (entries.length === 0) return '';
  return [...entries]
    .sort((left, right) => left.timeMs - right.timeMs || left.detail.localeCompare(right.detail))
    .map(({ timeMs, detail }) => `${formatEditorTimestamp(timeMs)} - - ${detail}`)
    .join('\n');
}

export function ObjectTypeEntriesPopover({
  headingLabel,
  difficultyVersion,
  entries,
  children,
  hoverHighlightColor,
}: {
  headingLabel: string;
  difficultyVersion: string;
  entries: ObjectsTypeEntry[];
  children: ReactNode;
  hoverHighlightColor?: string;
}) {
  const [detailFilter, setDetailFilter] = useState<string | null>(null);

  const sortedEntries = useMemo(
    () =>
      [...entries].sort(
        (left, right) => left.timeMs - right.timeMs || left.detail.localeCompare(right.detail)
      ),
    [entries]
  );

  const countsByDetail = useMemo(() => {
    const counts = new Map<string, number>();
    for (const entry of sortedEntries) {
      counts.set(entry.detail, (counts.get(entry.detail) ?? 0) + 1);
    }
    return counts;
  }, [sortedEntries]);

  const detailBadges = useMemo(() => {
    return Array.from(countsByDetail.entries()).sort((left, right) => {
      if (right[1] !== left[1]) {
        return right[1] - left[1];
      }
      return left[0].localeCompare(right[0]);
    });
  }, [countsByDetail]);

  const filteredEntries = useMemo(() => {
    if (!detailFilter) {
      return sortedEntries;
    }
    return sortedEntries.filter((entry) => entry.detail === detailFilter);
  }, [sortedEntries, detailFilter]);

  const linkText = useMemo(() => buildOsuTimestampLinkText(filteredEntries), [filteredEntries]);

  const scrollViewportMinHeight = useMemo(() => {
    const lines = sortedEntries.length;
    if (lines === 0) return undefined;
    return Math.min(
      POPOVER_LIST_MAX_HEIGHT_PX,
      Math.max(POPOVER_LIST_APPROX_LINE_HEIGHT_PX, lines * POPOVER_LIST_APPROX_LINE_HEIGHT_PX)
    );
  }, [sortedEntries.length]);

  if (entries.length === 0) {
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
      onDismiss={() => setDetailFilter(null)}
    >
      <Popover.Target>
        <UnstyledButton
          type="button"
          p={0}
          style={{
            width: '100%',
            display: 'block',
            textAlign: 'inherit',
            verticalAlign: 'inherit',
            cursor: 'pointer',
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
            {detailBadges.length > 1 ? (
              <InfoIconTooltip label={DETAIL_FILTER_TOOLTIP_LABEL} iconSize={14} />
            ) : null}
          </Group>
        </Group>
        {detailBadges.length > 1 ? (
          <Group gap={6} mb="sm" wrap="wrap" align="flex-start">
            {detailBadges.map(([detail, count]) => {
              const isActive = detailFilter === detail;
              return (
                <Badge
                  key={detail}
                  component="button"
                  type="button"
                  variant={isActive ? 'filled' : 'light'}
                  color={isActive ? 'blue' : 'gray'}
                  size="sm"
                  style={{ cursor: 'pointer' }}
                  onClick={() =>
                    setDetailFilter((current) => (current === detail ? null : detail))
                  }
                >
                  {detail}: {count.toLocaleString()}
                </Badge>
              );
            })}
          </Group>
        ) : null}
        <ScrollArea.Autosize
          mah={POPOVER_LIST_MAX_HEIGHT_PX}
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
