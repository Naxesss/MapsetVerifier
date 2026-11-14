import { Text, Flex, Alert, Group, Stack, Badge } from '@mantine/core';
import { IconAlertCircle, IconGavel } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import DocumentationOutcomeBlockquote from './DocumentationOutcomeBlockquote';
import MantineMarkdown from './MantineMarkdown';
import DocumentationApi from '../../client/DocumentationApi';
import { ApiDocumentationCheck, ApiDocumentationCheckDetails, Level } from '../../Types';
import { SectionTitle } from '../common/Headings.tsx';
import { TextSkeleton } from '../common/LoadingSkeletons.tsx';
import ConnectedLinks, { type ConnectedLink } from '../details/ConnectedLinks';
import { useDetailNavigation } from '../details/detailNavigation';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import LevelIcon from '../icons/LevelIcon.tsx';
import RcLeadText from '../rankingCriteria/RcLeadText';
import { useRcStatements } from '../rankingCriteria/useRankingCriteria';

/** Levels in the order checks list them, most severe first. */
const LEVEL_ORDER: Level[] = ['Problem', 'Warning', 'Minor', 'Info', 'Check', 'Error'];

/** The rules the check's outcomes enforce, one link each, with the levels of those outcomes. */
function LinkedRules({ details }: { details: ApiDocumentationCheckDetails }) {
  const navigation = useDetailNavigation();
  const ids = [...new Set(details.outcomes.flatMap((outcome) => outcome.ruleIds ?? []))];
  const { data, isLoading, error } = useRcStatements(ids);

  if (ids.length === 0) return null;
  if (isLoading) return <TextSkeleton lines={Math.min(ids.length, 3)} />;
  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red">
        Couldn&apos;t load the linked ranking criteria.
      </Alert>
    );
  }

  const links: ConnectedLink[] = (data ?? []).map((statement) => {
    const levels = LEVEL_ORDER.filter((level) =>
      details.outcomes.some(
        (outcome) => outcome.level === level && outcome.ruleIds?.includes(statement.id)
      )
    );

    return {
      key: statement.id,
      icon: IconGavel,
      label: <RcLeadText>{statement.lead}</RcLeadText>,
      meta: levels.map((level) => <LevelIcon key={level} level={level} size={16} />),
      cameFrom:
        navigation?.previous?.kind === 'rule' && navigation.previous.statement.id === statement.id,
      onOpen:
        navigation && !statement.retired
          ? () => navigation.open({ kind: 'rule', statement })
          : undefined,
      note: statement.retired
        ? 'No longer in the ranking criteria. This check needs updating.'
        : undefined,
    };
  });

  return <ConnectedLinks links={links} />;
}

/** The documentation of a check with its outcomes, shown in the documentation panel. */
export default function DocumentationCheckDetails({ check }: { check: ApiDocumentationCheck }) {
  const { data, isLoading, error } = useQuery<ApiDocumentationCheckDetails, Error>({
    queryKey: ['documentationCheckDetails', check.id],
    queryFn: () => DocumentationApi.getCheckDetails(check.id.toString()),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  return (
    <Flex direction="column" gap="lg">
      <Flex justify="space-between">
        <Group gap="xs">
          <Group gap={0}>
            {check.modes.map((mode) => (
              <GameModeIcon size={16} key={mode} mode={mode} />
            ))}
          </Group>
          <Badge>{check.category}</Badge>
        </Group>
        <Text size="sm" c="dimmed">
          Created by {check.author}
        </Text>
      </Flex>
      {isLoading && <TextSkeleton lines={4} />}
      {error && (
        <Alert icon={<IconAlertCircle />} color="red">
          Couldn&apos;t load the check documentation.
        </Alert>
      )}
      {data && (
        <>
          <Stack gap="md">
            <MantineMarkdown notesForBlockquotes>{data.description}</MantineMarkdown>
          </Stack>
          <LinkedRules details={data} />
          <SectionTitle>Outcomes</SectionTitle>
          <Stack gap="md">
            {data.outcomes.map((checkDetails, i) => (
              <DocumentationOutcomeBlockquote key={i} outcome={checkDetails} showRules={false} />
            ))}
          </Stack>
        </>
      )}
    </Flex>
  );
}
