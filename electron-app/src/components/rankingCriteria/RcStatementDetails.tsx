import { Anchor, Badge, Flex, Group, Paper, Stack, Text } from '@mantine/core';
import { IconExternalLink } from '@tabler/icons-react';
import { useMemo } from 'react';
import RcLeadText from './RcLeadText';
import RcMarkdown from './RcMarkdown';
import RcOutdatedNotice from './RcOutdatedNotice';
import { difficultyStarRating, formatDifficulties, KIND_COLOR, pageMode } from './rcUtils';
import { useRankingCriteriaPage } from './useRankingCriteria';
import { openExternal } from '../../hooks/useOpenExternal';
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
 * line, keeping the nesting as a list. With
 * `withParents`, the own lines of the statements it is nested in come first, so a sub-item such as
 * "...not be encoded upwards" is read under the sentence it finishes.
 */
function statementMarkdown(statement: ApiRcStatement, page: ApiRcPage, withParents: boolean) {
  const nested = new Set([statement.id]);
  let endLine = statement.endLine;
  // Statements are in page order, so a nested statement always comes after its parent.
  for (const other of page.statements) {
    if (other.parentId && nested.has(other.parentId)) {
      nested.add(other.id);
      endLine = Math.max(endLine, other.endLine);
    }
  }

  // 1-based line numbers to show, in page order.
  const shown = new Set<number>();
  for (let line = statement.startLine; line <= endLine; line++) shown.add(line);

  if (withParents) {
    const byId = new Map(page.statements.map((other) => [other.id, other]));
    const isNestedIn = (other: ApiRcStatement, ancestorId: string) => {
      for (let id = other.parentId; id; id = byId.get(id)?.parentId) {
        if (id === ancestorId) return true;
      }
      return false;
    };

    for (let parent = byId.get(statement.parentId ?? ''); parent; ) {
      // A parent's range can span its nested statements when a note follows them, as in "...which
      // both...", so leave out every line of theirs; only this statement's own block is shown.
      const nestedLines = new Set<number>();
      for (const other of page.statements.filter((other) => isNestedIn(other, parent!.id))) {
        for (let line = other.startLine; line <= other.endLine; line++) nestedLines.add(line);
      }
      for (let line = parent.startLine; line <= parent.endLine; line++) {
        if (!nestedLines.has(line)) shown.add(line);
      }
      parent = byId.get(parent.parentId ?? '');
    }
  }

  const allLines = page.markdown.split('\n');
  const lines = [...shown].sort((a, b) => a - b).map((line) => allLines[line - 1]);
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
 * shown, even when it is only the title's sentence, so every statement reads the same way. Without a title, it always shows, under
 * the sentences it is nested in, which the title would otherwise show.
 */
function StatementText({
  statement,
  withoutTitle,
}: {
  statement: ApiRcStatement;
  withoutTitle?: boolean;
}) {
  const page = useRankingCriteriaPage(statement.page);

  const markdown = useMemo(
    () => (page.data ? statementMarkdown(statement, page.data, !!withoutTitle) : null),
    [page.data, statement, withoutTitle]
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
      <SectionTitle component="span">
        <RcLeadText>{statement.lead}</RcLeadText>
      </SectionTitle>
    </Stack>
  );
}

interface RcStatementDetailsProps {
  statement: ApiRcStatement;
  /** Shown without RcStatementTitle, so the wiki text carries the lead and the parent's instead. */
  withoutTitle?: boolean;
}

/** Details of a ranking criteria statement, laid out like the check documentation. */
export default function RcStatementDetails({ statement, withoutTitle }: RcStatementDetailsProps) {
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

      <StatementText statement={statement} withoutTitle={withoutTitle} />
    </Flex>
  );
}
