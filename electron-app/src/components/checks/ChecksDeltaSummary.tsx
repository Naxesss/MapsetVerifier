import { Badge, Box, Group, Stack, Tabs, Text } from '@mantine/core';
import { IconCircleCheck, IconDelta, IconPlus, IconTrendingUp } from '@tabler/icons-react';
import { useMemo } from 'react';
import IssueRow from './IssueRow';
import { ApiCheckDeltaIssue, ApiCheckRunDelta } from '../../Types';
import { countWord } from '../../utils/countWord';

type DeltaTab = {
  id: string;
  label: string;
  color: string;
  issues: ApiCheckDeltaIssue[];
  icon: React.ReactNode;
};

interface ChecksDeltaSummaryProps {
  delta?: ApiCheckRunDelta | null;
  showMinor: boolean;
  hiddenMinorCheckIds: readonly number[];
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

function DeltaIssueRow({ issue }: { issue: ApiCheckDeltaIssue }) {
  return (
    <Stack gap={2}>
      <Group gap="xs" wrap="wrap">
        <Badge size="xs" variant="light" color="gray">
          {issue.category}
        </Badge>
        <Text size="sm" fw={700}>
          {issue.checkName}
        </Text>
        {issue.previousLevel && issue.previousLevel !== issue.level ? (
          <Text size="xs" c="dimmed">
            {issue.previousLevel} to {issue.level}
          </Text>
        ) : null}
      </Group>
      <Box ml="xs">
        <IssueRow item={{ id: issue.id, level: issue.level, message: issue.message }} />
      </Box>
    </Stack>
  );
}

export default function ChecksDeltaSummary({
  delta,
  showMinor,
  hiddenMinorCheckIds,
}: ChecksDeltaSummaryProps) {
  const tabs = useMemo<DeltaTab[]>(() => {
    if (!delta) return [];

    const visible = (issues: ApiCheckDeltaIssue[]) =>
      issues.filter((issue) => isVisibleIssue(issue, showMinor, hiddenMinorCheckIds));

    return [
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
        id: 'unchanged',
        label: 'Unchanged',
        color: 'gray',
        issues: visible(delta.unchangedIssues),
        icon: <IconDelta size={14} />,
      },
    ];
  }, [delta, showMinor, hiddenMinorCheckIds]);

  if (!delta) return null;

  const activeTab = tabs.find((tab) => tab.issues.length > 0)?.id ?? tabs[0]?.id;
  const previousRunAt = formatRunTime(delta.previousRunAt);
  const visibleNewCount = tabs.find((tab) => tab.id === 'new')?.issues.length ?? 0;
  const visibleResolvedCount = tabs.find((tab) => tab.id === 'resolved')?.issues.length ?? 0;
  const visibleWorsenedCount = tabs.find((tab) => tab.id === 'worsened')?.issues.length ?? 0;

  return (
    <Box
      p="sm"
      style={{
        border: '1px solid var(--mantine-color-dark-4)',
        borderRadius: 'var(--mantine-radius-sm)',
        background: 'var(--mantine-color-dark-7)',
      }}
    >
      <Stack gap="xs">
        <Group justify="space-between" gap="xs">
          <Text size="sm" fw={800}>
            What Changed Since Last Check Run?
          </Text>
          {previousRunAt ? (
            <Text size="xs" c="dimmed">
              Since {previousRunAt}
            </Text>
          ) : null}
        </Group>

        <Tabs defaultValue={activeTab} keepMounted={false}>
          <Tabs.List>
            {tabs.map((tab) => (
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

          {tabs.map((tab) => (
            <Tabs.Panel key={tab.id} value={tab.id} pt="sm">
              {tab.issues.length > 0 ? (
                <Stack gap="sm">
                  {tab.issues.map((issue, index) => (
                    <DeltaIssueRow
                      key={`${tab.id}-${issue.category}-${issue.id}-${index}`}
                      issue={issue}
                    />
                  ))}
                </Stack>
              ) : (
                <Text size="sm" c="dimmed">
                  No {tab.label.toLowerCase()} issues.
                </Text>
              )}
            </Tabs.Panel>
          ))}
        </Tabs>

        <Text size="xs" c="dimmed">
          {countWord(visibleNewCount, 'new issue')},{' '}
          {countWord(visibleResolvedCount, 'resolved issue')},{' '}
          {countWord(visibleWorsenedCount, 'worsened issue')}.
        </Text>
      </Stack>
    </Box>
  );
}
