import {
  ActionIcon,
  Alert,
  Anchor,
  CloseButton,
  Group,
  Paper,
  Progress,
  Skeleton,
  Stack,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconAlertCircle, IconExternalLink, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import RcCoverageFilter from './RcCoverageFilter';
import RcMarkdown from './RcMarkdown';
import RcPageSelect from './RcPageSelect';
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
import { ApiRcStatement, RcCoverage } from '../../Types';
import DetailModal from '../details/DetailModal';

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

// Manual is striped rather than a colour of its own: like its row icon it is grey, but it is not
// work left for a check.
const BAR_SECTIONS: { coverage: RcCoverage; label: string; color: string; striped?: boolean }[] = [
  { coverage: 'Covered', label: 'Covered', color: 'green' },
  { coverage: 'Partial', label: 'Partly covered', color: 'yellow' },
  { coverage: 'Outdated', label: 'Outdated', color: 'red' },
  { coverage: 'Uncovered', label: 'Not covered', color: 'gray.6' },
  { coverage: 'Manual', label: 'Manual', color: 'gray.6', striped: true },
];

/** Every rule and guideline split by coverage status, in the colours of the status icons. */
function CoverageBar({ rules }: { rules: ApiRcStatement[] }) {
  const counted = rules.filter((rule) => rule.coverage !== 'Informational');

  return (
    <Progress.Root size="md" radius="xl">
      {BAR_SECTIONS.map(({ coverage, label, color, striped }) => {
        const count = counted.filter((rule) => rule.coverage === coverage).length;
        if (count === 0) return null;

        return (
          <Tooltip key={coverage} label={`${label}: ${count}`} withinPortal>
            <Progress.Section
              value={(count / counted.length) * 100}
              color={color}
              striped={striped}
            />
          </Tooltip>
        );
      })}
    </Progress.Root>
  );
}

function EmptyState({ children }: { children: string }) {
  return (
    <Text size="sm" c="dimmed" ta="center" py="xl">
      {children}
    </Text>
  );
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

interface StatementListProps {
  statements: ApiRcStatement[];
  /** Intros and parents shown only for the statements nested in them. */
  contextIds: Set<string>;
  onOpen: (statement: ApiRcStatement) => void;
}

/** Statements in wiki order, each nested one below the intro or parent it belongs to. */
function StatementRows({
  statements,
  contextIds,
  showPage,
  onOpen,
  shownIds = new Set(statements.map((statement) => statement.id)),
}: StatementListProps & { showPage?: boolean; shownIds?: Set<string> }) {
  return (
    <Stack gap="xs">
      {statements.map((statement) =>
        statement.intro ? (
          <RcIntroRow key={statement.id} statement={statement} />
        ) : (
          <RcStatementRow
            key={statement.id}
            statement={statement}
            showPage={showPage && !contextIds.has(statement.id)}
            muted={contextIds.has(statement.id)}
            // The parent is right above it, so its lead needs no repeating.
            hideParentLead={!!statement.parentId && shownIds.has(statement.parentId)}
            onOpen={onOpen}
          />
        )
      )}
    </Stack>
  );
}

/** Statements of one page, grouped under the headings they appear under in the wiki. */
function StatementSections({ statements, contextIds, onOpen }: StatementListProps) {
  if (statements.every((statement) => contextIds.has(statement.id))) {
    return <EmptyState>Nothing on this page matches the filter.</EmptyState>;
  }

  const shownIds = new Set(statements.map((statement) => statement.id));
  const sections = new Map<string, ApiRcStatement[]>();
  for (const statement of statements) {
    const key = statement.path.join(' › ') || statement.pageTitle;
    sections.set(key, [...(sections.get(key) ?? []), statement]);
  }

  return (
    <Stack gap="xl">
      {[...sections.entries()].map(([section, sectionStatements]) => (
        <Stack key={section} gap="xs">
          <Text fw={700}>{section}</Text>
          <StatementRows
            statements={sectionStatements}
            contextIds={contextIds}
            shownIds={shownIds}
            onOpen={onOpen}
          />
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

  const rules = statements.filter((statement) => !statement.intro);
  const matches = (statement: ApiRcStatement) =>
    !statement.intro &&
    matchesCoverageFilter(statement.coverage, filter) &&
    matchesSearch(statement, query);

  // Intros and parent rules stay in the list, muted, above the statements that match, so a nested
  // statement is still read under the sentence it finishes.
  const byId = new Map(statements.map((statement) => [statement.id, statement]));
  const contextIds = new Set<string>();
  for (const statement of statements.filter(matches)) {
    for (let id = statement.parentId; id; id = byId.get(id)?.parentId) {
      const parent = byId.get(id);
      if (parent && !matches(parent)) contextIds.add(parent.id);
    }
  }
  const filtered = statements.filter(
    (statement) => matches(statement) || contextIds.has(statement.id)
  );
  const hasMatches = filtered.length > contextIds.size;

  const overall = coverageOf(statements);
  const percentage = overall.total > 0 ? Math.round((overall.covered / overall.total) * 100) : 0;

  const source = overview.data?.source;
  const sourceUrl = source
    ? `https://github.com/${source.repository}/tree/${source.commit}/wiki/Ranking_criteria`
    : null;
  const pages = overview.data?.pages ?? [];
  const currentPage = pages.find((page) => page.key === pageKey);
  const isSearching = query.trim().length > 0;

  const statementsOf = (key: string) => statements.filter((statement) => statement.page === key);
  const filteredOf = (key: string) => filtered.filter((statement) => statement.page === key);

  // The filter counts what the list below can show: search results, or the current page's rules.
  const scopedRules = isSearching
    ? rules.filter((statement) => matchesSearch(statement, query))
    : rules.filter((statement) => statement.page === pageKey);
  const showFilter = isSearching || !!currentPage?.hasStatements;

  const pageCoverage = currentPage?.hasStatements
    ? coverageOf(statementsOf(currentPage.key))
    : null;
  const wikiUrl = currentPage?.wikiUrl;

  // Three blocks (header, controls, list) spaced by `sm`, like the beatmap sidebar's search row and
  // list; everything inside a block by `xs`.
  return (
    <Stack gap="sm">
      <Stack gap="xs">
        <Group justify="space-between" align="baseline">
          <Text fw={700} size="md">
            {isLoading
              ? 'Loading coverage…'
              : `${overall.covered} of ${overall.total} checkable rules and guidelines covered (${percentage}%)`}
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
        <CoverageBar rules={rules} />
      </Stack>

      {/*
        Stays in view while scrolling the list. Padded and spaced like the beatmap sidebar's search
        row, so both line up once it sticks; negative margins keep the page spacing unchanged.
      */}
      <Stack
        gap="sm"
        py="xs"
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 2,
          margin: 'calc(var(--mantine-spacing-xs) * -1) 0',
          background: 'var(--mantine-color-body)',
        }}
      >
        <Group gap="sm">
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
                <CloseButton
                  aria-label="Clear search"
                  onClick={() => setSearchInput('')}
                  size="sm"
                />
              ) : null
            }
          />
          {wikiUrl && !isSearching && (
            <Tooltip label="Open this page on the osu! wiki" withinPortal>
              <ActionIcon
                variant="default"
                size="input-sm"
                aria-label="Open this page on the osu! wiki"
                onClick={() => void openExternal(wikiUrl)}
              >
                <IconExternalLink size={18} stroke={1.5} />
              </ActionIcon>
            </Tooltip>
          )}
        </Group>
        {/* The filter on the left, what it applies to on the right. */}
        <Group justify="space-between" gap="xs" mih={26}>
          {showFilter ? (
            <RcCoverageFilter rules={scopedRules} value={filter} onChange={setFilter} />
          ) : (
            <Text size="xs" c="dimmed">
              This page has no rules or guidelines of its own.
            </Text>
          )}
          {isSearching ? (
            <Text size="xs" c="dimmed">
              Results from all pages
            </Text>
          ) : (
            pageCoverage && (
              <Text size="xs" c="dimmed">
                {pageCoverage.covered} of {pageCoverage.total} checkable covered
              </Text>
            )
          )}
        </Group>
      </Stack>

      {(overview.error || isError) && (
        <Alert icon={<IconAlertCircle />} color="red">
          Failed to load the ranking criteria.
        </Alert>
      )}

      {ruleId && !isLoading && !selected && (
        <Alert icon={<IconAlertCircle />} color="orange">
          <Text span ff="monospace">
            {ruleId}
          </Text>{' '}
          is not in the current snapshot. It may have been removed from the ranking criteria.
        </Alert>
      )}

      {isSearching ? (
        !hasMatches ? (
          <EmptyState>No rules match your search.</EmptyState>
        ) : (
          <StatementRows
            statements={filtered}
            contextIds={contextIds}
            showPage
            onOpen={openStatement}
          />
        )
      ) : !currentPage ? null : !currentPage.hasStatements ? (
        <ReadOnlyPage pageKey={currentPage.key} />
      ) : isLoading ? (
        <RowSkeleton />
      ) : (
        <StatementSections
          statements={filteredOf(currentPage.key)}
          contextIds={contextIds}
          onOpen={openStatement}
        />
      )}

      <Text size="xs" c="dimmed">
        Content from the osu! wiki, licensed under CC BY-NC 4.0.
      </Text>

      <DetailModal
        view={selected ? { kind: 'rule', statement: selected } : null}
        onClose={closeStatement}
      />
    </Stack>
  );
}

export default RankingCriteria;
