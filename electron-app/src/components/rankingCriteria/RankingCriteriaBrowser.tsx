import { ActionIcon, Alert, Anchor, Group, Progress, Stack, Text, Tooltip } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import {
  IconAlertCircle,
  IconClipboardCheck,
  IconExternalLink,
  IconSearchOff,
} from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { COVERAGE_ORDER, COVERAGE_STATUSES } from './coverageStatus';
import RcMarkdown from './RcMarkdown';
import RcPageSelect from './RcPageSelect';
import RcStatementRow, { RcIntroRow } from './RcStatementRow';
import { isCoverable, isCovered, matchesSearch } from './rcUtils';
import {
  useAllRankingCriteriaPages,
  useRankingCriteriaOverview,
  useRankingCriteriaPage,
} from './useRankingCriteria';
import { useReferencePanel } from '../../context/ReferencePanelContext';
import { openExternal } from '../../hooks/useOpenExternal';
import { ApiRcStatement } from '../../Types';
import { formatDate } from '../../utils/dateTime';
import { isDevBuild } from '../../utils/devSettings';
import EmptyState from '../common/EmptyState';
import { SectionTitle } from '../common/Headings';
import { ListSkeleton } from '../common/LoadingSkeletons';
import SearchInput from '../common/SearchInput';
import SectionCard from '../common/SectionCard';
import StickyToolbar from '../common/StickyToolbar';

/** How many of the given statements a check could cover, and how many one does. */
function coverageOf(statements: ApiRcStatement[]) {
  const coverable = statements.filter((statement) => isCoverable(statement.coverage));
  return {
    covered: coverable.filter((statement) => isCovered(statement.coverage)).length,
    total: coverable.length,
  };
}

/** Every rule and guideline split by coverage status, in the colours of the status icons. */
function CoverageBar({ rules }: { rules: ApiRcStatement[] }) {
  const counted = rules.filter((rule) => rule.coverage !== 'Informational');

  return (
    <Progress.Root size="md" radius="xl">
      {COVERAGE_ORDER.map((coverage) => {
        const { label, color, striped } = COVERAGE_STATUSES[coverage];
        const count = counted.filter((rule) => rule.coverage === coverage).length;
        if (count === 0) return null;

        return (
          <Tooltip key={coverage} label={`${label}: ${count}`} withinPortal>
            <Progress.Section
              value={(count / counted.length) * 100}
              color={color === 'gray' ? 'gray.6' : color}
              striped={striped}
            />
          </Tooltip>
        );
      })}
    </Progress.Root>
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
          <SectionTitle>{section}</SectionTitle>
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

  if (page.isLoading) return <ListSkeleton />;
  if (!page.data) return null;

  return (
    <SectionCard>
      <RcMarkdown page={page.data} />
    </SectionCard>
  );
}

/** The ranking criteria list of the documentation panel: one wiki page at a time, or search results. */
function RankingCriteriaBrowser() {
  const navigate = useNavigate();
  const { rcPage: pageKey, showRcPage, openRule, close } = useReferencePanel();

  const [searchInput, setSearchInput] = useState('');
  const [query] = useDebouncedValue(searchInput, 300);

  const overview = useRankingCriteriaOverview();
  const { statements, isLoading, isError } = useAllRankingCriteriaPages(overview.data);

  const rules = statements.filter((statement) => !statement.intro);
  const matches = (statement: ApiRcStatement) =>
    !statement.intro && matchesSearch(statement, query);

  // While searching, intros and parent rules stay in the list, muted, above the statements that
  // match, so a nested statement is still read under the sentence it finishes.
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

  const pageCoverage = currentPage?.hasStatements
    ? coverageOf(statementsOf(currentPage.key))
    : null;
  const wikiUrl = currentPage?.wikiUrl;

  return (
    <Stack gap="md">
      <StickyToolbar>
        <Group gap="sm">
          <RcPageSelect
            pages={pages}
            value={pageKey}
            disabled={isSearching}
            coverageOf={(key) => (isLoading ? null : coverageOf(statementsOf(key)))}
            onChange={showRcPage}
          />
          <SearchInput
            style={{ flex: 1, minWidth: 180 }}
            placeholder="Search rules…"
            hint="Searches rule text, section and linked check names across all pages."
            value={searchInput}
            onChange={setSearchInput}
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
          {isDevBuild && (
            <Tooltip label="Review curation one rule at a time (dev only)" withinPortal>
              <ActionIcon
                variant="default"
                size="input-sm"
                aria-label="Review curation"
                onClick={() => {
                  close();
                  navigate(
                    currentPage?.hasStatements && !isSearching
                      ? `/ranking-criteria/review?page=${encodeURIComponent(pageKey)}`
                      : '/ranking-criteria/review'
                  );
                }}
              >
                <IconClipboardCheck size={18} stroke={1.5} />
              </ActionIcon>
            </Tooltip>
          )}
        </Group>
      </StickyToolbar>

      {/* Overall coverage, below the toolbar. */}
      <Stack gap="xs">
        <Group justify="space-between" align="baseline">
          <Text fw={700} size="sm">
            {isLoading
              ? 'Loading coverage…'
              : `${overall.covered} of ${overall.total} checkable rules and guidelines covered (${percentage}%)`}
            {!isLoading && (isSearching || pageCoverage) && (
              <Text span size="sm" c="dimmed" fw={400}>
                {isSearching
                  ? ' · searching all pages'
                  : ` · ${currentPage?.title ?? 'this page'} ${pageCoverage!.covered} of ${pageCoverage!.total}`}
              </Text>
            )}
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

      {(overview.error || isError) && (
        <Alert icon={<IconAlertCircle />} color="red">
          Couldn&apos;t load the ranking criteria.
        </Alert>
      )}

      {isSearching ? (
        !hasMatches ? (
          <EmptyState icon={IconSearchOff} title="No rules match your search" />
        ) : (
          <StatementRows statements={filtered} contextIds={contextIds} showPage onOpen={openRule} />
        )
      ) : !currentPage ? null : !currentPage.hasStatements ? (
        <ReadOnlyPage pageKey={currentPage.key} />
      ) : isLoading ? (
        <ListSkeleton />
      ) : (
        <StatementSections
          statements={filteredOf(currentPage.key)}
          contextIds={contextIds}
          onOpen={openRule}
        />
      )}

      <Text size="xs" c="dimmed">
        Content from the osu! wiki, licensed under CC BY-NC 4.0.
      </Text>
    </Stack>
  );
}

export default RankingCriteriaBrowser;
