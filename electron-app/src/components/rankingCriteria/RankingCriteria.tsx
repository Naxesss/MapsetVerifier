import {
  Alert,
  Anchor,
  CloseButton,
  Group,
  Paper,
  Progress,
  SegmentedControl,
  Skeleton,
  Stack,
  Text,
  TextInput,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconAlertCircle, IconExternalLink, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import RcMarkdown from './RcMarkdown';
import RcPageSelect from './RcPageSelect';
import RcStatementModal from './RcStatementModal';
import RcStatementRow, { RcIntroRow } from './RcStatementRow';
import {
  CoverageFilter,
  isCoverable,
  isCovered,
  matchesCoverageFilter,
  matchesSearch,
  openExternal,
  rankingCriteriaRoute,
} from './rcUtils';
import {
  useAllRankingCriteriaPages,
  useRankingCriteriaOverview,
  useRankingCriteriaPage,
} from './useRankingCriteria';
import { ApiRcStatement } from '../../Types';
import { countWord } from '../../utils/countWord';

const DEFAULT_PAGE = 'general';

function formatDate(value?: string | null) {
  if (!value) return null;

  return new Date(value).toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

/** How many of the given statements a check could cover, and how many one does. */
function coverageOf(statements: ApiRcStatement[]) {
  const coverable = statements.filter((statement) => isCoverable(statement.coverage));
  return {
    covered: coverable.filter((statement) => isCovered(statement.coverage)).length,
    total: coverable.length,
  };
}

function RowSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <Stack gap="xs" w="100%">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} height={56} radius="var(--mantine-radius-md)" />
      ))}
    </Stack>
  );
}

/** Statements of one page, grouped under the headings they appear under in the wiki. */
function StatementSections({
  statements,
  onOpen,
}: {
  statements: ApiRcStatement[];
  onOpen: (statement: ApiRcStatement) => void;
}) {
  if (statements.length === 0) {
    return (
      <Text size="sm" c="dimmed">
        Nothing on this page matches the filter.
      </Text>
    );
  }

  // Intros read as sub-headings, so the statements below them do not repeat them.
  const shownIds = new Set(statements.map((statement) => statement.id));
  const sections = new Map<string, ApiRcStatement[]>();
  for (const statement of statements) {
    const key = statement.path.join(' › ') || statement.pageTitle;
    sections.set(key, [...(sections.get(key) ?? []), statement]);
  }

  return (
    <Stack gap="lg">
      {[...sections.entries()].map(([section, sectionStatements]) => (
        <Stack key={section} gap="xs">
          <Text fw={700} size="sm" c="dimmed">
            {section}
          </Text>
          {sectionStatements.map((statement) =>
            statement.intro ? (
              <RcIntroRow key={statement.id} statement={statement} />
            ) : (
              <RcStatementRow
                key={statement.id}
                statement={statement}
                hideParentLead={!!statement.parentId && shownIds.has(statement.parentId)}
                onOpen={onOpen}
              />
            )
          )}
        </Stack>
      ))}
    </Stack>
  );
}

/** Pages without statements, such as Scaling BPM, are only there to read. */
function ReadOnlyPage({ pageKey }: { pageKey: string }) {
  const page = useRankingCriteriaPage(pageKey);

  if (page.isLoading) return <RowSkeleton />;
  if (!page.data) return null;

  return (
    <Paper p="lg" radius="md" withBorder>
      <RcMarkdown page={page.data} />
    </Paper>
  );
}

function RankingCriteria() {
  const { page: pageParam } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const pageKey = pageParam ?? DEFAULT_PAGE;
  const ruleId = searchParams.get('rule');

  const [searchInput, setSearchInput] = useState('');
  const [query] = useDebouncedValue(searchInput, 300);
  const [filter, setFilter] = useState<CoverageFilter>('all');

  const overview = useRankingCriteriaOverview();
  const { statements, isLoading, isError } = useAllRankingCriteriaPages(overview.data);

  const openStatement = (statement: ApiRcStatement) =>
    navigate(rankingCriteriaRoute(statement.page, statement.id));
  const closeStatement = () => navigate(rankingCriteriaRoute(pageKey), { replace: true });

  const selected = ruleId ? statements.find((statement) => statement.id === ruleId) : undefined;

  // Intros are only shown as sub-headings in the unfiltered list; their text is part of every
  // nested statement's search text, so search still finds them.
  const rules = statements.filter((statement) => !statement.intro);
  const filtered = statements.filter((statement) =>
    statement.intro
      ? filter === 'all' && query.trim().length === 0
      : matchesCoverageFilter(statement.coverage, filter) && matchesSearch(statement, query)
  );

  const overall = coverageOf(statements);
  const percentage = overall.total > 0 ? Math.round((overall.covered / overall.total) * 100) : 0;
  const coveredCount = statements.filter((statement) => isCovered(statement.coverage)).length;
  const uncoveredCount = statements.filter(
    (statement) => statement.coverage === 'Uncovered'
  ).length;

  const source = overview.data?.source;
  const sourceUrl = source
    ? `https://github.com/${source.repository}/tree/${source.commit}/wiki/Ranking_criteria`
    : null;
  const pages = overview.data?.pages ?? [];
  const currentPage = pages.find((page) => page.key === pageKey);
  const isSearching = query.trim().length > 0;

  const statementsOf = (key: string) => statements.filter((statement) => statement.page === key);
  const filteredOf = (key: string) => filtered.filter((statement) => statement.page === key);
  const filteredRulesOf = (key: string) => filteredOf(key).filter((statement) => !statement.intro);

  return (
    <>
      <Stack gap={6}>
        <Group justify="space-between" align="baseline">
          <Text fw={700} size="md">
            {isLoading
              ? 'Loading coverage…'
              : `${overall.covered} of ${overall.total} rules and guidelines covered by checks (${percentage}%)`}
          </Text>
          {source && sourceUrl && (
            <Text size="xs" c="dimmed">
              Snapshot of the osu! wiki at{' '}
              <Anchor
                size="xs"
                ff="monospace"
                href={sourceUrl}
                onClick={(event) => {
                  event.preventDefault();
                  void openExternal(sourceUrl);
                }}
              >
                {source.commit.slice(0, 8)}
              </Anchor>
              {source.commitDate && ` (${formatDate(source.commitDate)})`}
            </Text>
          )}
        </Group>
        <Progress value={percentage} color="green" size="sm" radius="xl" />
      </Stack>

      <Group gap="sm" mt="lg" mb="md">
        <RcPageSelect
          pages={pages}
          value={pageKey}
          disabled={isSearching}
          coverageOf={(key) => (isLoading ? null : coverageOf(statementsOf(key)))}
          onChange={(key) => navigate(rankingCriteriaRoute(key))}
        />
        <TextInput
          style={{ flex: 1, minWidth: 220 }}
          placeholder="Search rules (text, section, check name)…"
          value={searchInput}
          onChange={(event) => setSearchInput(event.currentTarget.value)}
          leftSection={<IconSearch size={18} stroke={1.5} />}
          rightSection={
            searchInput ? (
              <CloseButton aria-label="Clear search" onClick={() => setSearchInput('')} size="sm" />
            ) : null
          }
        />
        <SegmentedControl
          value={filter}
          onChange={(value) => setFilter(value as CoverageFilter)}
          data={[
            { value: 'all', label: `All ${rules.length}` },
            { value: 'covered', label: `Covered ${coveredCount}` },
            { value: 'uncovered', label: `Not covered ${uncoveredCount}` },
          ]}
        />
      </Group>

      {(overview.error || isError) && (
        <Alert icon={<IconAlertCircle />} color="red" mb="md">
          Failed to load the ranking criteria.
        </Alert>
      )}

      {ruleId && !isLoading && !selected && (
        <Alert icon={<IconAlertCircle />} color="orange" mb="md">
          <Text span ff="monospace">
            {ruleId}
          </Text>{' '}
          is not in the current snapshot. It may have been removed from the ranking criteria.
        </Alert>
      )}

      {isSearching ? (
        <Stack gap="xs">
          <Text size="xs" c="dimmed">
            Showing {filtered.length} of {countWord(rules.length, 'statement')} across all pages
          </Text>
          {filtered.length === 0 ? (
            <Text size="xs" c="dimmed">
              No rules match your search.
            </Text>
          ) : (
            filtered.map((statement) => (
              <RcStatementRow
                key={statement.id}
                statement={statement}
                showPage
                onOpen={openStatement}
              />
            ))
          )}
        </Stack>
      ) : (
        <>
          <Group justify="space-between">
            <Text size="xs" c="dimmed">
              {currentPage?.hasStatements
                ? `${currentPage.title}: ${coverageOf(statementsOf(currentPage.key)).covered} of ${
                    coverageOf(statementsOf(currentPage.key)).total
                  } covered · showing ${countWord(filteredRulesOf(currentPage.key).length, 'statement')}`
                : 'This page has no rules or guidelines of its own.'}
            </Text>
            {currentPage && (
              <Anchor
                size="xs"
                href={currentPage.wikiUrl}
                onClick={(event) => {
                  event.preventDefault();
                  void openExternal(currentPage.wikiUrl);
                }}
              >
                <Group gap={4} wrap="nowrap">
                  Open on osu! wiki
                  <IconExternalLink size={12} />
                </Group>
              </Anchor>
            )}
          </Group>
          <Stack pt="sm">
            {!currentPage ? null : !currentPage.hasStatements ? (
              <ReadOnlyPage pageKey={currentPage.key} />
            ) : isLoading ? (
              <RowSkeleton />
            ) : (
              <StatementSections statements={filteredOf(currentPage.key)} onOpen={openStatement} />
            )}
          </Stack>
        </>
      )}

      <Text size="xs" c="dimmed" mt="lg">
        Content from the osu! wiki, licensed under CC BY-NC 4.0.
      </Text>

      <RcStatementModal statement={selected ?? null} onClose={closeStatement} />
    </>
  );
}

export default RankingCriteria;
