import { Badge, Group, Stack, Text } from '@mantine/core';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import { ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult, Level } from '../../Types';
import { countWord } from '../../utils/countWord';

type DisplayLevel = Exclude<Level, 'Check'>;

const LEVEL_ORDER: DisplayLevel[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];

const LEVEL_BADGE_COLORS: Record<DisplayLevel, string> = {
  Error: 'gray',
  Problem: 'red',
  Warning: 'orange',
  Minor: 'lime',
  Info: 'green',
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
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
}

const CheckCategory: React.FC<CheckCategoryProps> = ({
  data,
  showMinor,
  selectedCategory,
  overrideResult,
}) => {
  const overrideCategoryResult = overrideResult?.categoryResult;
  const categoryData = React.useMemo(() => {
    // If we have an override result for the selected category, use it
    if (overrideCategoryResult && overrideCategoryResult.category === selectedCategory) {
      const groups = groupChecks(overrideCategoryResult.checkResults, showMinor);
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
    const groups = groupChecks(cat.checks, showMinor);
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
    data.general.difficultyLevel,
    data.general.starRating,
    showMinor,
    data.checks,
    selectedCategory,
    overrideResult,
  ]);

  return (
    <Stack gap="md">
      <Group wrap="wrap" gap="xs" align="center">
        {LEVEL_ORDER.map((level) => {
          const count = categoryData.levelCounts[level];
          if (count === 0) return null;

          return (
            <Badge key={level} size="xs" color={LEVEL_BADGE_COLORS[level]} variant="light">
              {countWord(count, level.toLowerCase())}
            </Badge>
          );
        })}
      </Group>
      {categoryData.totalCount > 0 ? (
        <Stack gap="sm">
          {categoryData.groups.map((g) => (
            <CheckGroup
              key={g.id}
              id={g.id}
              items={g.items}
              name={
                overrideCategoryResult && overrideCategoryResult.category === selectedCategory
                  ? overrideResult.checks[g.id]?.name
                  : data.checks[g.id]?.name
              }
            />
          ))}
        </Stack>
      ) : (
        <Text>No issues found.</Text>
      )}
    </Stack>
  );
};

export default CheckCategory;
