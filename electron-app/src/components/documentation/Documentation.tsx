import { Alert, CloseButton, Flex, Group, Loader, SimpleGrid, Space, Tabs, Text, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconAlertCircle, IconLayoutGrid, IconSearch } from '@tabler/icons-react';
import React, { useEffect, useMemo, useState } from 'react';
import BeatmapChecks from './BeatmapChecks.tsx';
import DocumentationCheck from './DocumentationCheck';
import { dedupeDocumentationChecksById, filterDocumentationChecks } from './filterDocumentationChecks';
import GeneralChecks from './GeneralChecks';
import { useDocumentationChecks } from './hooks/useDocumentationChecks';
import { formatGameModeLabel } from '../../utils/gameMode';
import ErrorIcon from '../icons/ErrorIcon.tsx';
import MinorIcon from '../icons/MinorIcon.tsx';
import NoIssueIcon from '../icons/NoIssueIcon.tsx';
import ProblemIcon from '../icons/ProblemIcon.tsx';
import WarningIcon from '../icons/WarningIcon.tsx';
import type { ApiDocumentationCheck, Mode } from '../../Types';

const BEATMAP_TAB_TO_MODE: Record<string, Mode> = {
  standard: 'Standard',
  taiko: 'Taiko',
  catch: 'Catch',
  mania: 'Mania',
};

/** Static header: not a child of search state, so it does not re-render on every keystroke. */
function DocumentationIconsSection() {
  return (
    <Alert icon={<IconLayoutGrid />} variant="light" color="gray" radius="md" title="Icons">
      <SimpleGrid
        cols={3}
        style={{
          gridTemplateColumns: '32px min-content auto',
          alignItems: 'center',
        }}
      >
        <DocumentationIconExplanation
          icon={<NoIssueIcon />}
          title="Check"
          category="Checks"
          description="No issues were found."
        />
        <DocumentationIconExplanation
          icon={<MinorIcon />}
          title="Minor"
          category="Checks"
          description="One or more negligible issues were found."
        />
        <DocumentationIconExplanation
          icon={<WarningIcon />}
          title="Warning"
          category="Checks"
          description="One or more guideline breaking issues were found."
        />
        <DocumentationIconExplanation
          icon={<ProblemIcon />}
          title="Problem"
          category="Checks"
          description="One or more rule breaking issues were found."
        />
        <DocumentationIconExplanation
          icon={<ErrorIcon />}
          title="Error"
          category="Checks"
          description="An error occurred preventing a complete check."
        />
      </SimpleGrid>
    </Alert>
  );
}

interface DocumentationSearchFieldProps {
  /** Called when the debounced query changes, and synchronously when the field is cleared. */
  onSearchApplied: (query: string) => void;
}

/** Local `searchInput` updates every keystroke (only this component re-renders). Parent gets `onSearchApplied` after debounce. */
function DocumentationSearchField({ onSearchApplied }: DocumentationSearchFieldProps) {
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearchQuery] = useDebouncedValue(searchInput, 300);

  useEffect(() => {
    onSearchApplied(debouncedSearchQuery);
  }, [debouncedSearchQuery, onSearchApplied]);

  return (
    <TextInput
      placeholder="Search all checks (name, category, author, mode)…"
      value={searchInput}
      onChange={(e) => setSearchInput(e.currentTarget.value)}
      leftSection={<IconSearch size={18} stroke={1.5} />}
      rightSection={
        searchInput ? (
          <CloseButton
            aria-label="Clear search"
            onClick={() => {
              setSearchInput('');
              onSearchApplied('');
            }}
            size="sm"
          />
        ) : null
      }
      mb="md"
    />
  );
}

/** Lists + data hooks: re-renders when applied search or tabs change, not on every keystroke. */
function DocumentationChecksBrowser() {
  const [appliedSearchQuery, setAppliedSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState('general');
  const {
    allChecks,
    generalChecks,
    beatmapChecks,
    isLoading: allChecksLoading,
    isError: allChecksError,
  } = useDocumentationChecks();

  const dedupedChecks = useMemo(() => dedupeDocumentationChecksById(allChecks), [allChecks]);
  const filteredAllChecks = useMemo(
    () => filterDocumentationChecks(dedupedChecks, appliedSearchQuery),
    [dedupedChecks, appliedSearchQuery],
  );

  const isSearching = appliedSearchQuery.trim().length > 0;

  const categoryCountSummary = useMemo(() => {
    if (allChecksLoading) return 'Loading check counts…';
    if (activeTab === 'general') {
      const n = generalChecks?.length ?? 0;
      return `Showing ${n} General check${n === 1 ? '' : 's'}.`;
    }
    const mode = BEATMAP_TAB_TO_MODE[activeTab];
    if (!mode) return '';
    const n = beatmapChecks[mode]?.length ?? 0;
    return `Showing ${n} ${formatGameModeLabel(mode)} check${n === 1 ? '' : 's'}.`;
  }, [activeTab, allChecksLoading, generalChecks, beatmapChecks]);

  return (
    <>
      <DocumentationSearchField onSearchApplied={setAppliedSearchQuery} />
      {isSearching ? (
        <Group gap="xs">
          {allChecksLoading && <Loader size="sm" />}
          {allChecksError && (
            <Alert icon={<IconAlertCircle />} color="red">
              Failed to load checks for search.
            </Alert>
          )}
          {!allChecksLoading && !allChecksError && (
            <>
              <Text size="xs" c="dimmed">
                Showing {filteredAllChecks.length} of {dedupedChecks.length} checks across all categories
              </Text>
              {filteredAllChecks.length === 0 ? (
                <Text size="xs" c="dimmed">
                  No checks match your search.
                </Text>
              ) : (
                filteredAllChecks.map((check: ApiDocumentationCheck) => (
                  <DocumentationCheck key={check.id} check={check} />
                ))
              )}
            </>
          )}
        </Group>
      ) : (
        <Tabs value={activeTab} onChange={(v) => setActiveTab(v ?? 'general')}>
          <Tabs.List grow>
            <Tabs.Tab value="general">General</Tabs.Tab>
            <Tabs.Tab value="standard">{formatGameModeLabel('Standard')}</Tabs.Tab>
            <Tabs.Tab value="taiko">{formatGameModeLabel('Taiko')}</Tabs.Tab>
            <Tabs.Tab value="catch">{formatGameModeLabel('Catch')}</Tabs.Tab>
            <Tabs.Tab value="mania">{formatGameModeLabel('Mania')}</Tabs.Tab>
          </Tabs.List>
          <Text size="xs" c="dimmed" mt="md">
            {categoryCountSummary}
          </Text>
          <Tabs.Panel value="general" pt="sm">
            <GeneralChecks />
          </Tabs.Panel>
          <Tabs.Panel value="standard" pt="sm">
            <BeatmapChecks mode="Standard" />
          </Tabs.Panel>
          <Tabs.Panel value="taiko" pt="sm">
            <BeatmapChecks mode="Taiko" />
          </Tabs.Panel>
          <Tabs.Panel value="catch" pt="sm">
            <BeatmapChecks mode="Catch" />
          </Tabs.Panel>
          <Tabs.Panel value="mania" pt="sm">
            <BeatmapChecks mode="Mania" />
          </Tabs.Panel>
        </Tabs>
      )}
    </>
  );
}

function Documentation() {
  return (
    <>
      <DocumentationIconsSection />
      <Space h="md" />
      <DocumentationChecksBrowser />
    </>
  );
}

interface InfoIconExplanationProp {
  icon: React.ReactNode;
  title: string;
  category: string;
  description: string;
}

function DocumentationIconExplanation(props: InfoIconExplanationProp) {
  return (
    <>
      {props.icon}
      <Flex direction="column">
        <Text fw="bold">{props.title}</Text>
        <Text fs="italic">{props.category}</Text>
      </Flex>
      <Text>{props.description}</Text>
    </>
  );
}

export default Documentation;
