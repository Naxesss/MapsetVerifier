import { Alert, Anchor, Badge, Button, Group, Paper, Stack, Text } from '@mantine/core';
import { IconAlertCircle, IconExternalLink, IconGavel } from '@tabler/icons-react';
import RcLeadText from './RcLeadText';
import RcOutdatedNotice from './RcOutdatedNotice';
import { formatDifficulties, KIND_COLOR } from './rcUtils';
import { useRcStatements } from './useRankingCriteria';
import { useReferencePanel } from '../../context/ReferencePanelContext';
import { openExternal } from '../../hooks/useOpenExternal';
import { ApiRcStatement } from '../../Types';
import { ListSkeleton } from '../common/LoadingSkeletons';

interface RuleReferencesProps {
  ruleIds?: string[];
}

/** What a reference says about the statement. */
function RuleSummary({ statement }: { statement: ApiRcStatement }) {
  const breadcrumb = [statement.pageTitle, ...statement.path].join(' › ');
  const difficulties = formatDifficulties(statement.difficulties);

  return (
    <Stack gap="xs">
      <Group gap="xs" wrap="nowrap">
        <Badge color={KIND_COLOR[statement.kind]}>{statement.kind}</Badge>
        <Text size="xs" c="dimmed" truncate>
          {breadcrumb}
          {difficulties && ` (${difficulties})`}
        </Text>
      </Group>
      {statement.parentLead && (
        <Text size="sm" c="dimmed">
          <RcLeadText>{statement.parentLead}</RcLeadText>
        </Text>
      )}
      <Text size="sm" fw={600}>
        <RcLeadText>{statement.lead}</RcLeadText>
      </Text>
      {statement.retired && (
        <Text size="xs" c="orange">
          This statement is no longer in the ranking criteria. The check linking to it needs
          updating.
        </Text>
      )}
      <RcOutdatedNotice statement={statement} />
    </Stack>
  );
}

function RuleReference({ statement }: { statement: ApiRcStatement }) {
  const { openRule } = useReferencePanel();

  return (
    <Paper p="sm" radius="md" withBorder>
      <Stack gap="xs">
        <RuleSummary statement={statement} />
        <Group gap="md">
          {!statement.retired && (
            <Button
              size="compact-xs"
              variant="light"
              leftSection={<IconGavel size={13} />}
              onClick={() => openRule(statement)}
            >
              Open in ranking criteria
            </Button>
          )}
          <Anchor
            size="xs"
            href={statement.wikiUrl}
            onClick={(event) => {
              event.preventDefault();
              void openExternal(statement.wikiUrl);
            }}
          >
            <Group gap="xs" wrap="nowrap">
              osu! wiki
              <IconExternalLink size={12} />
            </Group>
          </Anchor>
        </Group>
      </Stack>
    </Paper>
  );
}

/**
 * The ranking criteria statements an issue's outcome enforces, outside the documentation panel, each
 * with a button opening it there.
 */
export default function RuleReferences({ ruleIds }: RuleReferencesProps) {
  const ids = ruleIds ?? [];

  const { data, isLoading, error } = useRcStatements(ids);

  if (ids.length === 0) return null;
  if (isLoading) return <ListSkeleton rows={Math.min(ids.length, 3)} />;

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red">
        Couldn&apos;t load the linked ranking criteria.
      </Alert>
    );
  }

  return (
    <Stack gap="xs">
      {data?.map((statement) => <RuleReference key={statement.id} statement={statement} />)}
    </Stack>
  );
}
