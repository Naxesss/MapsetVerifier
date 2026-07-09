import {
  ActionIcon,
  Badge,
  Box,
  Collapse,
  Flex,
  Group,
  ScrollArea,
  Stack,
  Tabs,
  Text,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import {
  IconChevronDown,
  IconChevronRight,
  IconCircleCheck,
  IconDelta,
  IconPlus,
  IconRefresh,
  IconTrendingDown,
  IconTrendingUp,
} from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import { getGroupCopyText } from './CheckGroup';
import IssueDetailDrawer, { copyToClipboard } from './IssueDetailDrawer';
import IssueRow from './IssueRow';
import BeatmapApi from '../../client/BeatmapApi';
import { ApiCheckDeltaIssue, ApiCheckResult, ApiCheckRunDelta, Level } from '../../Types';
import { countWord } from '../../utils/countWord';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import LevelIcon from '../icons/LevelIcon';

const VISIBLE_ISSUE_COUNT = 5;
const PANEL_MAX_HEIGHT = 360;

type DeltaTabId = 'new' | 'resolved' | 'worsened' | 'improved' | 'unchanged';

type DeltaTab = {
  id: DeltaTabId;
  label: string;
  color: string;
  issues: ApiCheckDeltaIssue[];
  icon: React.ReactNode;
};

type IssueGroup = {
  key: string;
  id: number;
  checkName: string;
  category: string;
  items: ApiCheckDeltaIssue[];
};

interface ChecksDeltaSummaryProps {
  delta?: ApiCheckRunDelta | null;
  showMinor: boolean;
  hiddenMinorCheckIds: readonly number[];
  selectedCategory?: string;
  showUnchanged: boolean;
  beatmapFolderPath?: string;
  onHistoryCleared?: () => void;
}

function isVisibleIssue(
  issue: ApiCheckDeltaIssue,
  showMinor: boolean,
  hiddenMinorCheckIds: readonly number[]
) {
  if (issue.level !== 'Minor') return true;
  if (!showMinor) return false;
  return !hiddenMinorCheckIds.includes(issue.id);
}

function formatRunTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function highestLevel(items: ApiCheckDeltaIssue[]): Level {
  const order: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info', 'Check'];
  for (const level of order) {
    if (items.some((item) => item.level === level)) return level;
  }
  return 'Info';
}

function groupIssues(issues: ApiCheckDeltaIssue[], mapsetWide: boolean): IssueGroup[] {
  const groups = new Map<string, IssueGroup>();

  for (const issue of issues) {
    const key = mapsetWide ? `${issue.category}\u001f${issue.id}` : String(issue.id);
    const existing = groups.get(key);
    if (existing) {
      existing.items.push(issue);
      continue;
    }

    groups.set(key, {
      key,
      id: issue.id,
      checkName: issue.checkName,
      category: issue.category,
      items: [issue],
    });
  }

  return Array.from(groups.values()).sort((a, b) =>
    a.checkName.localeCompare(b.checkName, undefined, { sensitivity: 'base' })
  );
}

function filterByCategory(issues: ApiCheckDeltaIssue[], category?: string) {
  if (!category) return issues;
  return issues.filter((issue) => issue.category === category);
}

function renderDeltaIssueLevelChange(issue: ApiCheckDeltaIssue) {
  if (!issue.previousLevel || issue.previousLevel === issue.level) {
    return undefined;
  }

  return (
    <Group gap={6}>
      <LevelIcon level={issue.previousLevel === 'Check' ? 'Info' : issue.previousLevel} size={14} />
      <Text size="xs" c="dimmed">
        to
      </Text>
      <LevelIcon level={issue.level === 'Check' ? 'Info' : issue.level} size={14} />
    </Group>
  );
}

function DeltaIssueGroup({
  group,
  mapsetWide,
  onIssueOpen,
}: {
  group: IssueGroup;
  mapsetWide: boolean;
  onIssueOpen: (issue: ApiCheckDeltaIssue) => void;
}) {
  const [open, setOpen] = useState(true);
  const [showAll, setShowAll] = useState(false);
  const highest = highestLevel(group.items);
  const firstItems = group.items.slice(0, VISIBLE_ISSUE_COUNT);
  const extraItems = group.items.slice(VISIBLE_ISSUE_COUNT);

  return (
    <Stack gap={0}>
      <UnstyledButton
        onClick={() => setOpen((value) => !value)}
        style={{ cursor: 'pointer', userSelect: 'none', width: '100%' }}
      >
        <Flex gap="xs" align="center" style={{ minWidth: 0 }}>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              transform: open ? 'rotate(90deg)' : 'rotate(0deg)',
              transition: 'transform 200ms ease',
            }}
          >
            <IconChevronRight size={16} />
          </span>
          <LevelIcon level={highest === 'Check' ? 'Info' : highest} size={16} />
          {mapsetWide ? (
            <Badge size="xs" variant="light" color="gray">
              {group.category}
            </Badge>
          ) : null}
          <Text size="sm" fw={700} style={{ minWidth: 0, overflowWrap: 'anywhere' }}>
            {group.checkName}
          </Text>
        </Flex>
      </UnstyledButton>

      <Collapse in={open}>
        <Stack gap="0" ml="xl">
          {firstItems.map((issue, index) => (
            <IssueRow
              key={`${group.key}-${index}`}
              item={{ id: issue.id, level: issue.level, message: issue.message }}
              onOpen={() => onIssueOpen(issue)}
              prefix={renderDeltaIssueLevelChange(issue)}
            />
          ))}
          <Collapse in={showAll}>
            <Stack gap="0">
              {extraItems.map((issue, index) => (
                <IssueRow
                  key={`${group.key}-extra-${index}`}
                  item={{ id: issue.id, level: issue.level, message: issue.message }}
                  onOpen={() => onIssueOpen(issue)}
                  prefix={renderDeltaIssueLevelChange(issue)}
                />
              ))}
            </Stack>
          </Collapse>
          {extraItems.length > 0 ? (
            <Text
              size="sm"
              role="button"
              tabIndex={0}
              onClick={() => setShowAll((value) => !value)}
              style={{ cursor: 'pointer', color: 'var(--mantine-color-blue-6)', fontWeight: 500 }}
            >
              {showAll ? 'Hide extra issues' : `Show ${extraItems.length} more`}
            </Text>
          ) : null}
        </Stack>
      </Collapse>
    </Stack>
  );
}

export default function ChecksDeltaSummary({
  delta,
  showMinor,
  hiddenMinorCheckIds,
  selectedCategory,
  showUnchanged,
  beatmapFolderPath,
  onHistoryCleared,
}: ChecksDeltaSummaryProps) {
  const [expanded, setExpanded] = useState(false);
  const [showMapsetWide, setShowMapsetWide] = useState(false);
  const [clearing, setClearing] = useState(false);
  const [selectedIssue, setSelectedIssue] = useState<ApiCheckDeltaIssue | null>(null);
  const { getCheckById } = useDocumentationChecks();

  const category = selectedCategory ?? 'General';

  const allTabs = useMemo<DeltaTab[]>(() => {
    if (!delta) return [];

    const visible = (issues: ApiCheckDeltaIssue[]) =>
      issues.filter((issue) => isVisibleIssue(issue, showMinor, hiddenMinorCheckIds));

    const tabs: DeltaTab[] = [
      {
        id: 'new',
        label: 'New',
        color: 'red',
        issues: visible(delta.newIssues),
        icon: <IconPlus size={14} />,
      },
      {
        id: 'resolved',
        label: 'Resolved',
        color: 'green',
        issues: visible(delta.resolvedIssues),
        icon: <IconCircleCheck size={14} />,
      },
      {
        id: 'worsened',
        label: 'Worsened',
        color: 'orange',
        issues: visible(delta.worsenedIssues),
        icon: <IconTrendingUp size={14} />,
      },
      {
        id: 'improved',
        label: 'Improved',
        color: 'teal',
        issues: visible(delta.improvedIssues ?? []),
        icon: <IconTrendingDown size={14} />,
      },
    ];

    if (showUnchanged) {
      tabs.push({
        id: 'unchanged',
        label: 'Unchanged',
        color: 'gray',
        issues: visible(delta.unchangedIssues),
        icon: <IconDelta size={14} />,
      });
    }

    return tabs;
  }, [delta, showMinor, hiddenMinorCheckIds, showUnchanged]);

  const scopedTabs = useMemo(() => {
    if (showMapsetWide) return allTabs;
    return allTabs.map((tab) => ({
      ...tab,
      issues: filterByCategory(tab.issues, category),
    }));
  }, [allTabs, category, showMapsetWide]);

  const mapsetWideCounts = useMemo(() => {
    const countFor = (id: DeltaTabId) => allTabs.find((tab) => tab.id === id)?.issues.length ?? 0;
    return {
      new: countFor('new'),
      resolved: countFor('resolved'),
      worsened: countFor('worsened'),
      improved: countFor('improved'),
      unchanged: countFor('unchanged'),
    };
  }, [allTabs]);

  const scopedCounts = useMemo(() => {
    const countFor = (id: DeltaTabId) =>
      scopedTabs.find((tab) => tab.id === id)?.issues.length ?? 0;
    return {
      new: countFor('new'),
      resolved: countFor('resolved'),
      worsened: countFor('worsened'),
      improved: countFor('improved'),
      unchanged: countFor('unchanged'),
    };
  }, [scopedTabs]);

  const otherDifficultyCount = useMemo(() => {
    if (showMapsetWide) return 0;
    return allTabs.reduce((sum, tab) => {
      const inOther = tab.issues.filter((issue) => issue.category !== category).length;
      return sum + inOther;
    }, 0);
  }, [allTabs, category, showMapsetWide]);

  const totalVisible = scopedTabs.reduce((sum, tab) => sum + tab.issues.length, 0);

  const [prevDeltaToken, setPrevDeltaToken] = useState({ delta, showMinor, hiddenMinorCheckIds });

  if (
    prevDeltaToken.delta !== delta ||
    prevDeltaToken.showMinor !== showMinor ||
    prevDeltaToken.hiddenMinorCheckIds !== hiddenMinorCheckIds
  ) {
    setPrevDeltaToken({ delta, showMinor, hiddenMinorCheckIds });

    if (delta) {
      const visible = (issues: ApiCheckDeltaIssue[]) =>
        issues.filter((issue) => isVisibleIssue(issue, showMinor, hiddenMinorCheckIds));
      const urgent = visible(delta.newIssues).length + visible(delta.worsenedIssues).length;
      setExpanded(urgent > 0);
      setShowMapsetWide(false);
    }
  }

  const [prevCategory, setPrevCategory] = useState(category);

  if (category !== prevCategory) {
    setPrevCategory(category);
    setShowMapsetWide(false);
  }

  if (!delta) return null;

  const previousRunAt = formatRunTime(delta.previousRunAt);
  const activeTab = scopedTabs.find((tab) => tab.issues.length > 0)?.id ?? scopedTabs[0]?.id;
  const summaryCounts = showMapsetWide ? mapsetWideCounts : scopedCounts;

  const summaryLine = [
    summaryCounts.new > 0 ? `${summaryCounts.new} new` : null,
    summaryCounts.resolved > 0 ? `${summaryCounts.resolved} resolved` : null,
    summaryCounts.worsened > 0 ? `${summaryCounts.worsened} worsened` : null,
    summaryCounts.improved > 0 ? `${summaryCounts.improved} improved` : null,
  ]
    .filter(Boolean)
    .join(' · ');

  const handleClearHistory = async () => {
    if (!beatmapFolderPath || clearing) return;
    setClearing(true);
    try {
      await BeatmapApi.clearCheckRunHistory(beatmapFolderPath);
      onHistoryCleared?.();
    } finally {
      setClearing(false);
    }
  };

  if (totalVisible === 0) return null;

  const selectedDrawerIssue: ApiCheckResult | null = selectedIssue
    ? {
        id: selectedIssue.id,
        level: selectedIssue.level,
        message: selectedIssue.message,
      }
    : null;

  return (
    <Box
      p="sm"
      mb="sm"
      style={{
        border: '1px solid var(--mantine-color-dark-4)',
        borderRadius: 'var(--mantine-radius-sm)',
        background: 'var(--mantine-color-dark-7)',
      }}
    >
      <Stack gap="xs">
        <Group justify="space-between" gap="xs" wrap="nowrap">
          <UnstyledButton
            onClick={() => setExpanded((value) => !value)}
            style={{ flex: 1, minWidth: 0, cursor: 'pointer' }}
          >
            <Group gap="xs" wrap="wrap">
              <IconChevronDown
                size={16}
                style={{
                  transform: expanded ? 'rotate(0deg)' : 'rotate(-90deg)',
                  transition: 'transform 200ms ease',
                }}
              />
              <Text size="sm" fw={800}>
                What changed since last check run?
              </Text>
              {!expanded && summaryLine ? (
                <Text size="xs" c="dimmed">
                  {summaryLine}
                </Text>
              ) : null}
              {!expanded && previousRunAt ? (
                <Text size="xs" c="dimmed">
                  Since {previousRunAt}
                </Text>
              ) : null}
            </Group>
          </UnstyledButton>
          {beatmapFolderPath ? (
            <Tooltip label="Reset comparison baseline">
              <ActionIcon
                variant="subtle"
                color="gray"
                size="sm"
                loading={clearing}
                onClick={() => void handleClearHistory()}
                aria-label="Reset comparison baseline"
              >
                <IconRefresh size={14} />
              </ActionIcon>
            </Tooltip>
          ) : null}
        </Group>

        <Collapse in={expanded}>
          <Stack gap="xs">
            <Group justify="space-between" gap="xs">
              {previousRunAt ? (
                <Text size="xs" c="dimmed">
                  Since {previousRunAt}
                  {!showMapsetWide ? ` · ${category}` : ''}
                </Text>
              ) : null}
              {!showMapsetWide && otherDifficultyCount > 0 ? (
                <Text
                  size="xs"
                  c="blue"
                  style={{ cursor: 'pointer' }}
                  onClick={() => setShowMapsetWide(true)}
                >
                  {countWord(otherDifficultyCount, 'change')} on other difficulties
                </Text>
              ) : null}
              {showMapsetWide ? (
                <Text
                  size="xs"
                  c="blue"
                  style={{ cursor: 'pointer' }}
                  onClick={() => setShowMapsetWide(false)}
                >
                  Show only {category}
                </Text>
              ) : null}
            </Group>

            <Tabs key={activeTab} defaultValue={activeTab} keepMounted={false}>
              <Tabs.List style={{ flexWrap: 'wrap' }}>
                {scopedTabs.map((tab) => (
                  <Tabs.Tab key={tab.id} value={tab.id} leftSection={tab.icon}>
                    <Group gap={6}>
                      <span>{tab.label}</span>
                      <Badge size="xs" color={tab.color} variant="light">
                        {tab.issues.length}
                      </Badge>
                    </Group>
                  </Tabs.Tab>
                ))}
              </Tabs.List>

              {scopedTabs.map((tab) => (
                <Tabs.Panel key={tab.id} value={tab.id} pt="sm">
                  {tab.issues.length > 0 ? (
                    <ScrollArea.Autosize mah={PANEL_MAX_HEIGHT} type="auto" scrollbars="y">
                      <Stack gap="sm">
                        {groupIssues(tab.issues, showMapsetWide).map((group) => (
                          <DeltaIssueGroup
                            key={`${tab.id}-${group.key}`}
                            group={group}
                            mapsetWide={showMapsetWide}
                            onIssueOpen={setSelectedIssue}
                          />
                        ))}
                      </Stack>
                    </ScrollArea.Autosize>
                  ) : (
                    <Text size="sm" c="dimmed">
                      No {tab.label.toLowerCase()} issues for{' '}
                      {showMapsetWide ? 'this mapset' : category}.
                    </Text>
                  )}
                </Tabs.Panel>
              ))}
            </Tabs>
          </Stack>
        </Collapse>
      </Stack>
      <IssueDetailDrawer
        opened={selectedIssue !== null}
        onClose={() => setSelectedIssue(null)}
        issue={selectedDrawerIssue}
        checkName={selectedIssue?.checkName}
        documentationCheck={selectedIssue ? getCheckById(selectedIssue.id) : undefined}
        onCopyIssue={() => {
          if (!selectedDrawerIssue) return;
          copyToClipboard(
            getGroupCopyText([selectedDrawerIssue], selectedIssue?.checkName),
            'Copied 1 issue.'
          );
        }}
      />
    </Box>
  );
}
