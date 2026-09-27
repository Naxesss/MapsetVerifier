import { Anchor, Badge, Flex, Group, Paper, Stack, Text } from '@mantine/core';
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
import { SectionTitle } from '../common/Headings';
import { TextSkeleton } from '../common/LoadingSkeletons';
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
        {cameFrom && <Badge color="gray">You came from here</Badge>}
      </Group>
      <Group gap="md">
        {links.map((link) => (
          <Group key={link.templateKey} gap="xs" wrap="nowrap">
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
 * The statement's own lines of the page markdown, through those of the statements nested in it. A
 * nested statement comes below the lines of the statements it is nested in, such as "The audio file
 * of a beatmap must...", so it reads as on the wiki. Indentation is removed down to the outermost
 * line, keeping the nesting as a list.
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

  const byId = new Map(page.statements.map((other) => [other.id, other]));
  const ancestors: ApiRcStatement[] = [];
  for (let id = statement.parentId; id; id = byId.get(id)?.parentId) {
    const parent = byId.get(id);
    if (parent) ancestors.unshift(parent);
  }

  const allLines = page.markdown.split('\n');
  const lines = [
    // Only each parent's own lines, not its other nested statements.
    ...ancestors.flatMap((parent) => allLines.slice(parent.startLine - 1, parent.endLine)),
    ...allLines.slice(statement.startLine - 1, endLine),
  ];
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

/**
 * The full text of the statement as written on the wiki, including its examples and sub-rules. Always
 * shown, even when it is only the title's sentence, so every statement reads the same way.
 */
function StatementText({ statement }: { statement: ApiRcStatement }) {
  const page = useRankingCriteriaPage(statement.page);

  const markdown = useMemo(
    () => (page.data ? statementMarkdown(statement, page.data) : null),
    [page.data, statement]
  );

  if (page.isLoading) return <TextSkeleton lines={3} />;
  if (!markdown) return null;

  return (
    <Stack gap="xs">
      <SectionTitle>In the ranking criteria</SectionTitle>
      <Paper withBorder radius="md" px="md" pt="sm" pb="xs">
        <RcMarkdown page={{ key: statement.page, markdown }} compact />
      </Paper>
    </Stack>
  );
}

/** The title of a statement, below the sentence it finishes when it is nested. */
export function RcStatementTitle({ statement }: { statement: ApiRcStatement }) {
  return (
    <Stack gap="2xs">
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
          <Badge color={KIND_COLOR[statement.kind]}>{statement.kind}</Badge>
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
          <Group gap="xs" wrap="nowrap">
            osu! wiki
            <IconExternalLink size={14} />
          </Group>
        </Anchor>
      </Flex>

      <Stack gap="xs">
        <SectionTitle>Checks</SectionTitle>
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
