import { Badge, Group, Stack, Text } from '@mantine/core';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import {ApiBeatmapSetCheckResult, ApiCategoryOverrideCheckResult} from '../../Types';

interface CheckCategoryProps {
  data: ApiBeatmapSetCheckResult;
  showMinor: boolean;
  selectedCategory?: string;
  overrideResult?: ApiCategoryOverrideCheckResult;
}

const CheckCategory: React.FC<CheckCategoryProps> = ({ data, showMinor, selectedCategory, overrideResult }) => {
  const overrideCategoryResult = overrideResult?.categoryResult;
  const categoryData = React.useMemo(() => {
    // If we have an override result for the selected category, use it
    if (overrideCategoryResult && overrideCategoryResult.category === selectedCategory) {
      const groups = groupChecks(overrideCategoryResult.checkResults, showMinor);
      const allItems = groups.flatMap((g) => g.items);
      const minorCount = allItems.filter((i) => i.level === 'Minor').length;
      const total = groups.reduce((sum, g) => sum + g.items.length, 0) - minorCount;
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
        total,
        minorCount,
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
    const allItems = groups.flatMap((g) => g.items);
    const minorCount = allItems.filter((i) => i.level === 'Minor').length;
    const total = groups.reduce((sum, g) => sum + g.items.length, 0) - minorCount;
    const sortedGroups = [...groups].sort((a, b) => {
      const nameA = (data.checks[a.id]?.name ?? '').toLowerCase();
      const nameB = (data.checks[b.id]?.name ?? '').toLowerCase();
      if (nameA && nameB) return nameA.localeCompare(nameB);
      if (nameA) return -1;
      if (nameB) return 1;
      return 0;
    });
    return { ...cat, groups: sortedGroups, total, minorCount };
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
      <Group wrap="nowrap" gap="xs" align="center">
        <Badge size="xs" color={categoryData.total ? 'red' : 'green'} variant="light">
          {categoryData.total} issue{categoryData.total !== 1 ? 's' : ''}
        </Badge>
        {showMinor && categoryData.minorCount > 0 && (
          <Badge size="xs" color="yellow" variant="light">
            {categoryData.minorCount} minor
          </Badge>
        )}
      </Group>
      {categoryData.total > 0 || categoryData.minorCount > 0 ? (
        <Stack gap="0">
          {categoryData.groups.map((g) => (
            <CheckGroup
              key={g.id}
              id={g.id}
              items={g.items}
              name={
                overrideCategoryResult && overrideCategoryResult.category === selectedCategory ? overrideResult.checks[g.id]?.name : data.checks[g.id]?.name}
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
