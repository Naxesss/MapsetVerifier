import {
  Anchor,
  Badge,
  Flex,
  Group,
  Loader,
  Modal,
  Paper,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core';
import { IconExternalLink } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
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
import { ApiDocumentationCheck, ApiRcCheckLink, ApiRcPage, ApiRcStatement } from '../../Types';
import DocumentationCheckModal from '../documentation/DocumentationCheckModal';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import GameModeIcon from '../icons/GameModeIcon';
import LevelIcon from '../icons/LevelIcon';

interface RcStatementModalProps {
  statement: ApiRcStatement | null;
  onClose: () => void;
}

/** The templates of one check linking to the statement. */
function LinkedCheck({
  links,
  onOpen,
}: {
  links: ApiRcCheckLink[];
  onOpen: (checkId: number) => void;
}) {
  const theme = useMantineTheme();
  const [hovered, setHovered] = useState(false);
  const background = hovered
    ? theme.variantColorResolver({ variant: 'light', theme, color: 'blue' }).background
    : theme.variantColorResolver({ variant: 'light', theme, color: 'gray' }).background;

  return (
    <Group
      p="sm"
      w="100%"
      wrap="nowrap"
      style={{
        background,
        borderRadius: theme.defaultRadius,
        cursor: 'pointer',
        transition: 'background 0.2s',
      }}
      role="button"
      tabIndex={0}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onClick={() => onOpen(links[0].checkId)}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onOpen(links[0].checkId);
        }
      }}
    >
      <Text fw="bold" style={{ flex: 1 }}>
        {links[0].checkName}
      </Text>
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

/** The full text of the statement as written on the wiki, including its examples and sub-rules. */
function StatementText({ statement }: { statement: ApiRcStatement }) {
  const page = useRankingCriteriaPage(statement.page);

  const markdown = useMemo(
    () => (page.data ? statementMarkdown(statement, page.data) : null),
    [page.data, statement]
  );

  if (page.isLoading) return <Loader size="sm" />;
  if (!markdown) return null;

  return (
    <Paper withBorder radius="md" px="md" pt="sm" pb={4}>
      <RcMarkdown page={{ key: statement.page, markdown }} compact />
    </Paper>
  );
}

/** Details of a ranking criteria statement, laid out like the check documentation modal. */
export default function RcStatementModal({ statement, onClose }: RcStatementModalProps) {
  const { getCheckById } = useDocumentationChecks();
  const [openCheck, setOpenCheck] = useState<ApiDocumentationCheck | null>(null);

  // Keep showing the last statement while the modal animates closed.
  const [shown, setShown] = useState(statement);
  if (statement && statement !== shown) setShown(statement);

  const current = statement ?? shown;
  const mode = current ? pageMode(current.page) : null;
  const difficulties = current ? formatDifficulties(current.difficulties) : '';

  const linksByCheck = new Map<number, ApiRcCheckLink[]>();
  for (const link of current?.links ?? []) {
    linksByCheck.set(link.checkId, [...(linksByCheck.get(link.checkId) ?? []), link]);
  }

  return (
    <>
      <Modal
        opened={statement !== null}
        onClose={onClose}
        title={
          current && (
            <Stack gap={2}>
              {current.parentLead && (
                <Text size="sm" c="dimmed">
                  {current.parentLead}
                </Text>
              )}
              <Text fw="bold" size="lg">
                {current.lead}
              </Text>
            </Stack>
          )
        }
        yOffset="120px"
        size="80%"
        styles={{ content: { maxWidth: 1000 } }}
      >
        {current && (
          <Flex direction="column" gap="lg">
            <Flex justify="space-between" align="center" gap="md">
              <Group gap="xs">
                {mode && (
                  <GameModeIcon
                    size={16}
                    mode={mode}
                    starRating={
                      current.difficulties.length > 0
                        ? difficultyStarRating(current.difficulties[0])
                        : undefined
                    }
                  />
                )}
                <Badge size="xs" variant="light" color={KIND_COLOR[current.kind]}>
                  {current.kind}
                </Badge>
                <Text size="sm" c="dimmed">
                  {[current.pageTitle, ...current.path].join(' › ')}
                  {difficulties && ` (${difficulties})`}
                </Text>
              </Group>
              <Anchor
                size="sm"
                href={current.wikiUrl}
                onClick={(event) => {
                  event.preventDefault();
                  void openExternal(current.wikiUrl);
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
              <RcOutdatedNotice statement={current} />
              {linksByCheck.size > 0 ? (
                [...linksByCheck.values()].map((links) => (
                  <LinkedCheck
                    key={links[0].checkId}
                    links={links}
                    onOpen={(checkId) => setOpenCheck(getCheckById(checkId) ?? null)}
                  />
                ))
              ) : (
                <Text c="dimmed">
                  {current.coverage === 'Covered' || current.coverage === 'Partial'
                    ? current.coverage === 'Covered'
                      ? 'Covered through the sub-rules nested under it.'
                      : 'Some of the sub-rules nested under it are covered by checks.'
                    : current.coverage === 'Informational'
                      ? 'Allowances clarify what is acceptable, so there is nothing for a check to enforce.'
                      : current.coverage === 'Manual'
                        ? 'This needs human judgement, so no check is expected.'
                        : 'No check covers this yet.'}
                </Text>
              )}
              {current.notes && <Text size="sm">{current.notes}</Text>}
            </Stack>

            <Stack gap={4}>
              <Text size="sm" fw={700} c="dimmed">
                In the ranking criteria
              </Text>
              <StatementText statement={current} />
            </Stack>
          </Flex>
        )}
      </Modal>
      {openCheck && (
        <DocumentationCheckModal opened onClose={() => setOpenCheck(null)} check={openCheck} />
      )}
    </>
  );
}
