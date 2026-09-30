import { Group, Stack, Text, useMantineTheme } from '@mantine/core';
import { IconCircleCheck } from '@tabler/icons-react';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import { DisplayLevel, normalizeLevel, SEVERITY_ORDER } from './utils/levelUtils';
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import { getLevelLabel } from '../../utils/levelLabel';
import EmptyState from '../common/EmptyState.tsx';
import FilterChip from '../common/FilterChip.tsx';
import VirtualizedList from '../common/VirtualizedList.tsx';
import { levelColorName } from '../icons/levelColor';
import LevelIcon from '../icons/LevelIcon.tsx';

type CheckGroupUiState = {
  isOpen: boolean;
  showAll: boolean;
};

const VIRTUAL_GROUP_OVERSCAN = 6;

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
  /** Kept by the parent so it survives switching difficulties. */
  levelFilter: DisplayLevel | null;
  onLevelFilterChange: (level: DisplayLevel | null) => void;
}

const defaultGroupState: CheckGroupUiState = { isOpen: true, showAll: false };

const CheckCategory: React.FC<CheckCategoryProps> = ({
  data,
  showMinor,
  hiddenMinorCheckIds,
  selectedCategory,
  overrideResult,
  levelFilter,
  onLevelFilterChange,
}) => {
  const theme = useMantineTheme();
  const [groupUiState, setGroupUiState] = React.useState<Record<number, CheckGroupUiState>>({});
  const [prevGroupUiToken, setPrevGroupUiToken] = React.useState({
    levelFilter,
    selectedCategory,
    overrideResult,
  });

  if (
    prevGroupUiToken.levelFilter !== levelFilter ||
    prevGroupUiToken.selectedCategory !== selectedCategory ||
    prevGroupUiToken.overrideResult !== overrideResult
  ) {
    setPrevGroupUiToken({ levelFilter, selectedCategory, overrideResult });
    setGroupUiState({});
  }

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
      const totalCount = SEVERITY_ORDER.reduce((sum, level) => sum + levelCounts[level], 0);
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
    const totalCount = SEVERITY_ORDER.reduce((sum, level) => sum + levelCounts[level], 0);
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
    onLevelFilterChange(levelFilter === level ? null : level);
  };

  // Rows on this page are all `sm` apart: the difficulty row, the severity badges and each check.
  return (
    <Stack gap="sm">
      <Group wrap="wrap" gap="xs" align="center">
        {SEVERITY_ORDER.map((level) => {
          const count = categoryData.levelCounts[level];
          const isSelected = levelFilter === level;
          // A selected severity stays visible at 0 after a difficulty switch. Turning it off hides it.
          if (count === 0 && !isSelected) return null;

          return (
            <FilterChip
              key={level}
              label={
                <>
                  {getLevelLabel(level)}{' '}
                  <Text span fz="0.85em" c="dimmed" ml="xs">
                    ({count})
                  </Text>
                </>
              }
              color={levelColorName(level)}
              icon={<LevelIcon level={level} size={14} />}
              active={isSelected}
              radius={theme.defaultRadius}
              onClick={() => toggleLevelFilter(level)}
            />
          );
        })}
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
        <EmptyState icon={IconCircleCheck} title="No issues found" />
      )}
    </Stack>
  );
};

export default CheckCategory;
