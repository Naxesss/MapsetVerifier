import { Badge, Group, Stack, Text } from '@mantine/core';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import { countWord } from '../../utils/countWord';
import { getLevelLabel } from '../../utils/levelLabel';
import { InfoIconTooltip } from '../common/InfoIconTooltip.tsx';
import VirtualizedList from '../common/VirtualizedList.tsx';

type DisplayLevel = Exclude<Level, 'Check'>;

type CheckGroupUiState = {
  isOpen: boolean;
  showAll: boolean;
};

const LEVEL_ORDER: DisplayLevel[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
const VIRTUAL_GROUP_OVERSCAN = 6;

const LEVEL_BADGE_COLORS: Record<DisplayLevel, string> = {
  Error: 'gray',
  Problem: 'red',
  Warning: 'orange',
  Minor: 'lime',
  Info: 'teal',
};

function normalizeLevel(level: Level): DisplayLevel {
  return level === 'Check' ? 'Info' : level;
}

function getLevelCounts(levels: Level[]): Record<DisplayLevel, number> {
  const counts: Record<DisplayLevel, number> = {
    Error: 0,
    Problem: 0,
    Warning: 0,
    Minor: 0,
    Info: 0,
  };

  for (const level of levels) {
    counts[normalizeLevel(level)] += 1;
  }

  return counts;
}

interface CheckCategoryProps {
  data: ApiBeatmapSetCheckResult;
  showMinor: boolean;
  hiddenMinorCheckIds: readonly number[];
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
}

const defaultGroupState: CheckGroupUiState = { isOpen: true, showAll: false };

const CheckCategory: React.FC<CheckCategoryProps> = ({
  data,
  showMinor,
  hiddenMinorCheckIds,
  selectedCategory,
  overrideResult,
}) => {
  const [levelFilter, setLevelFilter] = React.useState<DisplayLevel | null>(null);
  const [groupUiState, setGroupUiState] = React.useState<Record<number, CheckGroupUiState>>({});

  const overrideCategoryResult = overrideResult?.categoryResult;
  const categoryData = React.useMemo(() => {
    if (overrideCategoryResult && overrideCategoryResult.category === selectedCategory) {
      const groups = groupChecks(
        overrideCategoryResult.checkResults,
        showMinor,
        hiddenMinorCheckIds
      );
      const allLevels = groups.flatMap((g) => g.items.map((item) => item.level));
      const levelCounts = getLevelCounts(allLevels);
      const totalCount = LEVEL_ORDER.reduce((sum, level) => sum + levelCounts[level], 0);
      const sortedGroups = [...groups].sort((a, b) => {
        const nameA = (overrideResult.checks[a.id]?.name ?? '').toLowerCase();
        const nameB = (overrideResult.checks[b.id]?.name ?? '').toLowerCase();
        if (nameA && nameB) return nameA.localeCompare(nameB);
        if (nameA) return -1;
        if (nameB) return 1;
        return 0;
      });
      return {
        name: overrideCategoryResult.category,
        checks: overrideCategoryResult.checkResults,
        mode: overrideCategoryResult.mode,
        difficultyLevel: overrideCategoryResult.difficultyLevel,
        starRating: overrideCategoryResult.starRating,
        groups: sortedGroups,
        levelCounts,
        totalCount,
      };
    }

    const allCategories = [
      {
        name: 'General',
        checks: data.general.checkResults,
        mode: data.general.mode,
        difficultyLevel: undefined,
        starRating: undefined,
      },
      ...data.difficulties.map((d) => ({
        name: d.category,
        checks: d.checkResults,
        mode: d.mode,
        difficultyLevel: d.difficultyLevel,
        starRating: d.starRating,
      })),
    ];

    const cat = allCategories.find((c) => c.name === selectedCategory) ?? allCategories[0];
    const groups = groupChecks(cat.checks, showMinor, hiddenMinorCheckIds);
    const allLevels = groups.flatMap((g) => g.items.map((item) => item.level));
    const levelCounts = getLevelCounts(allLevels);
    const totalCount = LEVEL_ORDER.reduce((sum, level) => sum + levelCounts[level], 0);
    const sortedGroups = [...groups].sort((a, b) => {
      const nameA = (data.checks[a.id]?.name ?? '').toLowerCase();
      const nameB = (data.checks[b.id]?.name ?? '').toLowerCase();
      if (nameA && nameB) return nameA.localeCompare(nameB);
      if (nameA) return -1;
      if (nameB) return 1;
      return 0;
    });
    return { ...cat, groups: sortedGroups, levelCounts, totalCount };
  }, [
    data.general.checkResults,
    data.difficulties,
    data.general.mode,
    data.checks,
    showMinor,
    hiddenMinorCheckIds,
    selectedCategory,
    overrideCategoryResult,
    overrideResult,
  ]);

  const filteredGroups = React.useMemo(() => {
    if (!levelFilter) return categoryData.groups;
    return categoryData.groups
      .map((g) => ({
        ...g,
        items: g.items.filter((item) => normalizeLevel(item.level) === levelFilter),
      }))
      .filter((g) => g.items.length > 0);
  }, [categoryData.groups, levelFilter]);

  const categoryCheckDefinitions =
    overrideCategoryResult && overrideCategoryResult.category === selectedCategory
      ? overrideResult.checks
      : data.checks;

  React.useEffect(() => {
    setGroupUiState({});
  }, [levelFilter, selectedCategory, overrideResult]);

  const toggleGroupOpen = React.useCallback((id: number) => {
    setGroupUiState((current) => {
      const state = current[id] ?? defaultGroupState;
      return {
        ...current,
        [id]: {
          ...state,
          isOpen: !state.isOpen,
        },
      };
    });
  }, []);

  const toggleGroupShowAll = React.useCallback((id: number) => {
    setGroupUiState((current) => {
      const state = current[id] ?? defaultGroupState;
      return {
        ...current,
        [id]: {
          ...state,
          showAll: !state.showAll,
        },
      };
    });
  }, []);

  const renderVirtualGroup = (group: (typeof filteredGroups)[number]) => {
    const state = groupUiState[group.id] ?? defaultGroupState;

    return (
      <CheckGroup
        id={group.id}
        items={group.items}
        isOpen={state.isOpen}
        name={categoryCheckDefinitions[group.id]?.name}
        showAll={state.showAll}
        onToggleOpen={toggleGroupOpen}
        onToggleShowAll={toggleGroupShowAll}
      />
    );
  };

  const toggleLevelFilter = (level: DisplayLevel) => {
    setLevelFilter((current) => (current === level ? null : level));
  };

  return (
    <Stack gap="md">
      <Group wrap="wrap" gap="xs" align="center">
        {LEVEL_ORDER.map((level) => {
          const count = categoryData.levelCounts[level];
          if (count === 0) return null;

          const isSelected = levelFilter === level;

          return (
            <Badge
              key={level}
              component="button"
              type="button"
              size="xs"
              color={LEVEL_BADGE_COLORS[level]}
              variant={isSelected ? 'filled' : 'light'}
              onClick={() => toggleLevelFilter(level)}
              style={{ cursor: 'pointer' }}
              title={isSelected ? 'Show all severities' : `Filter by this type`}
            >
              {level === 'Minor'
                ? `${count} ${getLevelLabel(level)}`
                : countWord(count, level.toLowerCase())}
            </Badge>
          );
        })}
        {categoryData.totalCount > 0 && (
          <InfoIconTooltip label="Click a badge to filter issues by severity" />
        )}
      </Group>
      {categoryData.totalCount > 0 ? (
        filteredGroups.length > 0 ? (
          <VirtualizedList
            items={filteredGroups}
            estimateSize={() => 104}
            getItemKey={(group) => group.id}
            renderItem={renderVirtualGroup}
            overscan={VIRTUAL_GROUP_OVERSCAN}
            rowGap="var(--mantine-spacing-sm)"
          />
        ) : (
          <Text size="sm" c="dimmed">
            No issues match this severity.
          </Text>
        )
      ) : (
        <Text>No issues found.</Text>
      )}
    </Stack>
  );
};

export default CheckCategory;
