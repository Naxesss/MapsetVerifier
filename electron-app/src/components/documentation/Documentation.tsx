import {
  ActionIcon,
  Box,
  Group,
  Popover,
  Stack,
  Text,
  Tooltip,
  useMantineTheme,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconHelpCircle, IconListSearch, IconSearchOff } from '@tabler/icons-react';
import React, { useEffect, useMemo, useState } from 'react';
import DocumentationCheckList from './DocumentationCheckList';
import DocumentationModeSelect, {
  documentationCategoryLabel,
  type DocumentationCategory,
} from './DocumentationModeSelect';
import {
  dedupeDocumentationChecksById,
  filterDocumentationChecks,
} from './filterDocumentationChecks';
import { useDocumentationChecks } from './hooks/useDocumentationChecks';
import { countWord, pluralize } from '../../utils/countWord';
import EmptyState from '../common/EmptyState.tsx';
import { MicroLabel } from '../common/Headings.tsx';
import SearchInput from '../common/SearchInput.tsx';
import StickyToolbar from '../common/StickyToolbar.tsx';
import ErrorIcon from '../icons/ErrorIcon.tsx';
import InfoLevelIcon from '../icons/InfoLevelIcon.tsx';
import MinorIcon from '../icons/MinorIcon.tsx';
import NoIssueIcon from '../icons/NoIssueIcon.tsx';
import ProblemIcon from '../icons/ProblemIcon.tsx';
import SnapshotHasChangesIcon from '../icons/SnapshotHasChangesIcon.tsx';
import SnapshotNoChangesIcon from '../icons/SnapshotNoChangesIcon.tsx';
import WarningIcon from '../icons/WarningIcon.tsx';

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
      gap="sm"
      p="xs"
      w="100%"
      style={{ background, borderRadius: theme.defaultRadius, minWidth: 0 }}
    >
      <Box style={{ flexShrink: 0, lineHeight: 0 }}>{props.icon}</Box>
      <Text size="sm" fw={600} w={90} style={{ flexShrink: 0 }}>
        {props.title}
      </Text>
      <Text size="sm" style={{ flex: 1, minWidth: 0 }}>
        {props.description}
      </Text>
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

const ICON_LEGEND_LABEL = 'What do the icons mean?';

/** The icon legend, folded into a popover next to the search so the checks start at the top. */
function DocumentationIconLegend() {
  const categories = [...new Set(DOCUMENTATION_ICON_EXPLANATIONS.map((item) => item.category))];

  return (
    <Popover position="bottom-end" shadow="md" width={440} withinPortal>
      <Tooltip label={ICON_LEGEND_LABEL}>
        <Popover.Target>
          <ActionIcon variant="default" size="input-sm" aria-label={ICON_LEGEND_LABEL}>
            <IconHelpCircle size={18} stroke={1.5} />
          </ActionIcon>
        </Popover.Target>
      </Tooltip>
      <Popover.Dropdown p="sm">
        <Stack gap="sm">
          {categories.map((category) => (
            <Stack key={category} gap="xs">
              <MicroLabel>{category}</MicroLabel>
              {DOCUMENTATION_ICON_EXPLANATIONS.filter((item) => item.category === category).map(
                (item) => (
                  <DocumentationIconExplanation key={`${item.category}-${item.title}`} {...item} />
                )
              )}
            </Stack>
          ))}
        </Stack>
      </Popover.Dropdown>
    </Popover>
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
    <SearchInput
      style={{ flex: 1, minWidth: 220 }}
      placeholder="Search checks…"
      hint="Searches check name, category, author and mode across all modes."
      value={searchInput}
      onChange={(value) => {
        setSearchInput(value);
        // Clearing applies right away instead of waiting for the debounce.
        if (value === '') onSearchApplied('');
      }}
    />
  );
}

/** Lists + data hooks: re-renders when applied search or the category change, not on every keystroke. */
function DocumentationChecksBrowser() {
  const [appliedSearchQuery, setAppliedSearchQuery] = useState('');
  const [category, setCategory] = useState<DocumentationCategory>('general');
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

  const countOf = (value: DocumentationCategory) => {
    if (allChecksLoading) return null;
    return value === 'general' ? (generalChecks?.length ?? 0) : (beatmapChecks[value]?.length ?? 0);
  };
  const categoryCount = countOf(category);
  const categoryLabel = documentationCategoryLabel(category);

  // Laid out like Ranking criteria: a sticky toolbar (what to browse, search, help; then what the
  // list shows), the list below.
  return (
    <Stack gap="sm">
      <StickyToolbar>
        <Group gap="sm" wrap="nowrap">
          <DocumentationModeSelect
            value={category}
            disabled={isSearching}
            countOf={countOf}
            onChange={setCategory}
          />
          <DocumentationSearchField onSearchApplied={setAppliedSearchQuery} />
          <DocumentationIconLegend />
        </Group>
        <Text size="xs" c="dimmed">
          {isSearching
            ? `Showing ${filteredAllChecks.length} of ${dedupedChecks.length} ${pluralize(dedupedChecks.length, 'check')} across all categories`
            : categoryCount === null
              ? 'Loading check counts…'
              : `Showing ${countWord(categoryCount, 'check')} (${documentationCategoryLabel(category)})`}
        </Text>
      </StickyToolbar>
      {isSearching ? (
        <DocumentationCheckList
          checks={filteredAllChecks}
          isLoading={allChecksLoading}
          isError={allChecksError}
          errorMessage="Couldn't load the checks to search."
          emptyState={<EmptyState icon={IconSearchOff} title="No checks match your search" />}
        />
      ) : (
        <DocumentationCheckList
          key={category}
          checks={category === 'general' ? generalChecks : beatmapChecks[category]}
          isLoading={allChecksLoading}
          isError={allChecksError}
          errorMessage={`Couldn't load the ${categoryLabel} checks.`}
          emptyState={
            <EmptyState icon={IconListSearch} title={`No ${categoryLabel} checks found`} />
          }
        />
      )}
    </Stack>
  );
}

function Documentation() {
  return <DocumentationChecksBrowser />;
}

export default Documentation;
