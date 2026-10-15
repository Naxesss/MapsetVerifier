import {
  Alert,
  Box,
  CloseButton,
  Group,
  Loader,
  Skeleton,
  SimpleGrid,
  Space,
  Stack,
  Tabs,
  Text,
  TextInput,
  useMantineTheme,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconAlertCircle, IconSearch } from '@tabler/icons-react';
import React, { useEffect, useMemo, useState } from 'react';
import BeatmapChecks from './BeatmapChecks.tsx';
import DocumentationCheck from './DocumentationCheck';
import {
  dedupeDocumentationChecksById,
  filterDocumentationChecks,
} from './filterDocumentationChecks';
import GeneralChecks from './GeneralChecks';
import { useDocumentationChecks } from './hooks/useDocumentationChecks';
import { countWord, pluralize } from '../../utils/countWord';
import { formatGameModeLabel } from '../../utils/gameMode';
import ErrorIcon from '../icons/ErrorIcon.tsx';
import InfoLevelIcon from '../icons/InfoLevelIcon.tsx';
import MinorIcon from '../icons/MinorIcon.tsx';
import NoIssueIcon from '../icons/NoIssueIcon.tsx';
import ProblemIcon from '../icons/ProblemIcon.tsx';
import SnapshotHasChangesIcon from '../icons/SnapshotHasChangesIcon.tsx';
import SnapshotNoChangesIcon from '../icons/SnapshotNoChangesIcon.tsx';
import WarningIcon from '../icons/WarningIcon.tsx';
import type { ApiDocumentationCheck, Mode } from '../../Types';

/**
 * Mounting the check list is CPU-heavy (per-row theme resolution across 100+ checks). Waiting
 * two rAFs lets the route swap paint first, so navigation feels instant and the list pops in
 * right after instead of blocking the transition.
 */
function useDeferredReady() {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let inner = 0;
    const outer = requestAnimationFrame(() => {
      inner = requestAnimationFrame(() => setReady(true));
    });
    return () => {
      cancelAnimationFrame(outer);
      cancelAnimationFrame(inner);
    };
  }, []);

  return ready;
}

function CheckListSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <Stack gap="xs" w="100%">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} height={56} radius="var(--mantine-radius-md)" />
      ))}
    </Stack>
  );
}

const BEATMAP_TAB_TO_MODE: Record<string, Mode> = {
  standard: 'Standard',
  taiko: 'Taiko',
  catch: 'Catch',
  mania: 'Mania',
};

interface InfoIconExplanationProp {
  icon: React.ReactNode;
  title: string;
  category: string;
  description: string;
}

function DocumentationIconExplanation(props: InfoIconExplanationProp) {
  const theme = useMantineTheme();
  const background = theme.variantColorResolver({
    variant: 'light',
    theme,
    color: 'gray',
  }).background;

  return (
    <Group
      wrap="nowrap"
      align="center"
      gap="md"
      p="sm"
      w="100%"
      style={{ background, borderRadius: theme.defaultRadius, minWidth: 0, height: '100%' }}
    >
      <Box style={{ flexShrink: 0, lineHeight: 0 }}>{props.icon}</Box>
      <Stack gap={0} style={{ flexShrink: 0 }}>
        <Text fw="bold">{props.title}</Text>
        <Text fs="italic" size="xs" c="dimmed">
          {props.category}
        </Text>
      </Stack>
      <Text style={{ flex: 1, minWidth: 0 }}>{props.description}</Text>
    </Group>
  );
}

const DOCUMENTATION_ICON_EXPLANATIONS: InfoIconExplanationProp[] = [
  {
    icon: <NoIssueIcon />,
    title: 'Check',
    category: 'Checks',
    description: 'No issues were found.',
  },
  {
    icon: <MinorIcon />,
    title: 'Negligible',
    category: 'Checks',
    description: 'One or more negligible issues were found.',
  },
  {
    icon: <InfoLevelIcon />,
    title: 'Info',
    category: 'Checks',
    description: 'Informational notes or non-blocking observations.',
  },
  {
    icon: <WarningIcon />,
    title: 'Warning',
    category: 'Checks',
    description: 'One or more guideline breaking issues were found.',
  },
  {
    icon: <ProblemIcon />,
    title: 'Problem',
    category: 'Checks',
    description: 'One or more rule-breaking issues were found.',
  },
  {
    icon: <ErrorIcon />,
    title: 'Error',
    category: 'Checks',
    description: 'An error occurred preventing a complete check.',
  },
  {
    icon: <SnapshotHasChangesIcon />,
    title: 'Changes',
    category: 'Snapshots',
    description: 'This difficulty has at least one snapshot commit with changes.',
  },
  {
    icon: <SnapshotNoChangesIcon />,
    title: 'Unchanged',
    category: 'Snapshots',
    description: 'This difficulty has no recorded changes in snapshot history.',
  },
];

/** Static header: not a child of search state, so it does not re-render on every keystroke. */
function DocumentationIconsSection() {
  return (
    <Stack gap="xs">
      <Text fw={700} size="md">
        Icons
      </Text>
      <SimpleGrid cols={2} spacing="xs">
        {DOCUMENTATION_ICON_EXPLANATIONS.map((item) => (
          <DocumentationIconExplanation key={`${item.category}-${item.title}`} {...item} />
        ))}
      </SimpleGrid>
    </Stack>
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
    [dedupedChecks, appliedSearchQuery]
  );

  const isSearching = appliedSearchQuery.trim().length > 0;
  const ready = useDeferredReady();

  const categoryCountSummary = useMemo(() => {
    if (allChecksLoading) return 'Loading check counts…';
    if (activeTab === 'general') {
      const n = generalChecks?.length ?? 0;
      return `Showing ${countWord(n, 'check')} (General).`;
    }
    const mode = BEATMAP_TAB_TO_MODE[activeTab];
    if (!mode) return '';
    const n = beatmapChecks[mode]?.length ?? 0;
    return `Showing ${countWord(n, 'check')} (${formatGameModeLabel(mode)}).`;
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
          {!allChecksLoading && !allChecksError && !ready && <CheckListSkeleton />}
          {!allChecksLoading && !allChecksError && ready && (
            <Box className="mv-deferred-content-enter" w="100%">
              <Text size="xs" c="dimmed">
                Showing {filteredAllChecks.length} of {dedupedChecks.length}{' '}
                {pluralize(dedupedChecks.length, 'check')} across all categories
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
            </Box>
          )}
        </Group>
      ) : (
        <Tabs value={activeTab} onChange={(v) => setActiveTab(v ?? 'general')} keepMounted={false}>
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
            {ready ? (
              <Box className="mv-deferred-content-enter" w="100%">
                <GeneralChecks />
              </Box>
            ) : (
              <CheckListSkeleton />
            )}
          </Tabs.Panel>
          <Tabs.Panel value="standard" pt="sm">
            {ready ? (
              <Box className="mv-deferred-content-enter" w="100%">
                <BeatmapChecks mode="Standard" />
              </Box>
            ) : (
              <CheckListSkeleton />
            )}
          </Tabs.Panel>
          <Tabs.Panel value="taiko" pt="sm">
            {ready ? (
              <Box className="mv-deferred-content-enter" w="100%">
                <BeatmapChecks mode="Taiko" />
              </Box>
            ) : (
              <CheckListSkeleton />
            )}
          </Tabs.Panel>
          <Tabs.Panel value="catch" pt="sm">
            {ready ? (
              <Box className="mv-deferred-content-enter" w="100%">
                <BeatmapChecks mode="Catch" />
              </Box>
            ) : (
              <CheckListSkeleton />
            )}
          </Tabs.Panel>
          <Tabs.Panel value="mania" pt="sm">
            {ready ? (
              <Box className="mv-deferred-content-enter" w="100%">
                <BeatmapChecks mode="Mania" />
              </Box>
            ) : (
              <CheckListSkeleton />
            )}
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

export default Documentation;
