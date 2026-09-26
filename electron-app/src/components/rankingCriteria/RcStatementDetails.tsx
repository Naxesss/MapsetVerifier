import { Anchor, Badge, Flex, Group, Loader, Paper, Stack, Text, Title } from '@mantine/core';
import { IconExternalLink } from '@tabler/icons-react';
import { useMemo } from 'react';
import RcLeadText from './RcLeadText';
import RcMarkdown from './RcMarkdown';
import RcOutdatedNotice from './RcOutdatedNotice';
import {
  difficultyStarRating,
  formatDifficulties,
  KIND_COLOR,
  openExternal,
  pageMode,
} from './rcUtils';
import { useRankingCriteriaPage } from './useRankingCriteria';
import { ApiRcCheckLink, ApiRcPage, ApiRcStatement } from '../../Types';
import ClickablePanel from '../details/ClickablePanel';
import { useDetailNavigation } from '../details/detailNavigation';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';

/** The templates of one check linking to the statement, opening its documentation in place. */
function LinkedCheck({ links }: { links: ApiRcCheckLink[] }) {
  const navigation = useDetailNavigation();
  const { getCheckById } = useDocumentationChecks();
  const check = getCheckById(links[0].checkId);
  const cameFrom =
    navigation?.previous?.kind === 'check' && navigation.previous.check.id === links[0].checkId;

  const content = (
    <Group wrap="nowrap">
      <Group gap="xs" style={{ flex: 1 }}>
        <Text fw="bold">{links[0].checkName}</Text>
        {cameFrom && (
          <Badge size="xs" variant="light" color="gray">
            You came from here
          </Badge>
        )}
      </Group>
      <Group gap="md">
        {links.map((link) => (
          <Group key={link.templateKey} gap={6} wrap="nowrap">
            <LevelIcon level={link.level} size={18} />
            <Text size="sm" c="dimmed">
              {link.templateKey}
            </Text>
          </Group>
        ))}
      </Group>
    </Group>
  );

  if (!navigation || !check) {
    return (
      <Paper p="sm" radius="md" withBorder>
        {content}
      </Paper>
    );
  }

  return (
    <ClickablePanel onClick={() => navigation.open({ kind: 'check', check })}>
      {content}
    </ClickablePanel>
  );
}

/**
 * The statement's own lines of the page markdown, through those of the statements nested in it,
 * with its indentation removed so a nested list item renders as a list of its own.
 */
function statementMarkdown(statement: ApiRcStatement, page: ApiRcPage) {
  const nested = new Set([statement.id]);
  let endLine = statement.endLine;
  // Statements are in page order, so a nested statement always comes after its parent.
  for (const other of page.statements) {
    if (other.parentId && nested.has(other.parentId)) {
      nested.add(other.id);
      endLine = Math.max(endLine, other.endLine);
    }
  }

  const allLines = page.markdown.split('\n');
  const lines = allLines.slice(statement.startLine - 1, endLine);
  const indent = lines[0].match(/^\s*/)?.[0].length ?? 0;

  // Footnotes are defined at the bottom of the page, so bring along the ones this text uses.
  const footnotes = [...new Set(lines.join('\n').match(/\[\^[^\]]+\](?!:)/g) ?? [])]
    .map((ref) => allLines.find((line) => line.startsWith(`${ref}:`)))
    .filter((line) => line !== undefined);
  if (footnotes.length > 0) lines.push('', ...footnotes);

  return lines
    .map((line) => line.slice(Math.min(indent, line.match(/^\s*/)![0].length)))
    .join('\n');
}

/** Letters and digits only, to tell whether two texts say the same regardless of markdown. */
function plainWords(text: string) {
  return text.toLowerCase().replace(/[^\p{L}\p{N}]+/gu, '');
}

/**
 * The full text of the statement as written on the wiki, including its examples and sub-rules. Left
 * out when it is only the sentence the title already shows.
 */
function StatementText({ statement }: { statement: ApiRcStatement }) {
  const page = useRankingCriteriaPage(statement.page);

  const markdown = useMemo(
    () => (page.data ? statementMarkdown(statement, page.data) : null),
    [page.data, statement]
  );

  if (page.isLoading) return <Loader size="sm" />;
  if (!markdown || plainWords(markdown) === plainWords(statement.lead)) return null;

  return (
    <Stack gap="xs">
      <Title order={2}>In the ranking criteria</Title>
      <Paper withBorder radius="md" px="md" pt="sm" pb={4}>
        <RcMarkdown page={{ key: statement.page, markdown }} compact />
      </Paper>
    </Stack>
  );
}

/** The title of a statement, below the sentence it finishes when it is nested. */
export function RcStatementTitle({ statement }: { statement: ApiRcStatement }) {
  return (
    <Stack gap={2}>
      {statement.parentLead && (
        <Text size="sm" c="dimmed">
          <RcLeadText>{statement.parentLead}</RcLeadText>
        </Text>
      )}
      <Text fw="bold" size="lg">
        <RcLeadText>{statement.lead}</RcLeadText>
      </Text>
    </Stack>
  );
}

/** Details of a ranking criteria statement, laid out like the check documentation. */
export default function RcStatementDetails({ statement }: { statement: ApiRcStatement }) {
  const mode = pageMode(statement.page);
  const difficulties = formatDifficulties(statement.difficulties);

  const linksByCheck = new Map<number, ApiRcCheckLink[]>();
  for (const link of statement.links) {
    linksByCheck.set(link.checkId, [...(linksByCheck.get(link.checkId) ?? []), link]);
  }

  return (
    <Flex direction="column" gap="lg">
      <Flex justify="space-between" align="center" gap="md">
        <Group gap="xs">
          {mode && (
            <GameModeIcon
              size={16}
              mode={mode}
              starRating={
                statement.difficulties.length > 0
                  ? difficultyStarRating(statement.difficulties[0])
                  : undefined
              }
            />
          )}
          <Badge size="xs" variant="light" color={KIND_COLOR[statement.kind]}>
            {statement.kind}
          </Badge>
          <Text size="sm" c="dimmed">
            {[statement.pageTitle, ...statement.path].join(' › ')}
            {difficulties && ` (${difficulties})`}
          </Text>
        </Group>
        <Anchor
          size="sm"
          href={statement.wikiUrl}
          onClick={(event) => {
            event.preventDefault();
            void openExternal(statement.wikiUrl);
          }}
        >
          <Group gap={4} wrap="nowrap">
            osu! wiki
            <IconExternalLink size={14} />
          </Group>
        </Anchor>
      </Flex>

      <Stack gap="xs">
        <Title order={2}>Checks</Title>
        <RcOutdatedNotice statement={statement} />
        {linksByCheck.size > 0 ? (
          [...linksByCheck.values()].map((links) => (
            <LinkedCheck key={links[0].checkId} links={links} />
          ))
        ) : (
          <Text c="dimmed">
            {statement.coverage === 'Covered' || statement.coverage === 'Partial'
              ? statement.coverage === 'Covered'
                ? 'Covered through the sub-rules nested under it.'
                : 'Some of the sub-rules nested under it are covered by checks.'
              : statement.coverage === 'Informational'
                ? 'Allowances clarify what is acceptable, so there is nothing for a check to enforce.'
                : statement.coverage === 'Manual'
                  ? 'This needs human judgement, so no check is expected.'
                  : 'No check covers this yet.'}
          </Text>
        )}
        {statement.notes && <Text size="sm">{statement.notes}</Text>}
      </Stack>

      <StatementText statement={statement} />
    </Flex>
  );
}
