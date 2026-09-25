import { Alert, Anchor, Badge, Button, Group, Loader, Paper, Stack, Text } from '@mantine/core';
import { IconAlertCircle, IconExternalLink, IconGavel } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import RcOutdatedNotice from './RcOutdatedNotice';
import { formatDifficulties, KIND_COLOR, openExternal, rankingCriteriaRoute } from './rcUtils';
import RankingCriteriaApi from '../../client/RankingCriteriaApi';
import { ApiRcStatement } from '../../Types';

interface RuleReferencesProps {
  ruleIds?: string[];
}

function RuleReference({ statement }: { statement: ApiRcStatement }) {
  const navigate = useNavigate();
  const breadcrumb = [statement.pageTitle, ...statement.path].join(' › ');
  const difficulties = formatDifficulties(statement.difficulties);

  return (
    <Paper p="sm" radius="md" withBorder>
      <Stack gap={6}>
        <Group gap="xs" wrap="nowrap">
          <Badge size="xs" variant="light" color={KIND_COLOR[statement.kind]}>
            {statement.kind}
          </Badge>
          <Text size="xs" c="dimmed" truncate>
            {breadcrumb}
            {difficulties && ` (${difficulties})`}
          </Text>
        </Group>
        {statement.parentLead && (
          <Text size="sm" c="dimmed">
            {statement.parentLead}
          </Text>
        )}
        <Text size="sm" fw={600}>
          {statement.lead}
        </Text>
        {statement.retired && (
          <Text size="xs" c="orange">
            This statement is no longer in the ranking criteria. The check linking to it needs
            updating.
          </Text>
        )}
        <RcOutdatedNotice statement={statement} />
        <Group gap="md">
          {!statement.retired && (
            <Button
              size="compact-xs"
              variant="light"
              leftSection={<IconGavel size={13} />}
              onClick={() => navigate(rankingCriteriaRoute(statement.page, statement.id))}
            >
              Show in ranking criteria
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
            <Group gap={4} wrap="nowrap">
              osu! wiki
              <IconExternalLink size={12} />
            </Group>
          </Anchor>
        </Group>
      </Stack>
    </Paper>
  );
}

/** The ranking criteria statements an issue or outcome enforces, linking into the RC viewer. */
export default function RuleReferences({ ruleIds }: RuleReferencesProps) {
  const ids = ruleIds ?? [];

  const { data, isLoading, error } = useQuery<ApiRcStatement[], Error>({
    queryKey: ['rankingCriteriaStatements', ...ids],
    queryFn: () => RankingCriteriaApi.getStatements(ids),
    enabled: ids.length > 0,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  if (ids.length === 0) return null;
  if (isLoading) return <Loader size="xs" />;

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red">
        Failed to load the linked ranking criteria.
      </Alert>
    );
  }

  return (
    <Stack gap="xs">
      {data?.map((statement) => <RuleReference key={statement.id} statement={statement} />)}
    </Stack>
  );
}
