import { Badge, Group, Stack, Text, Title } from '@mantine/core';
import { IconStarFilled } from '@tabler/icons-react';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import { ApiBeatmapSetCheckResult, ApiCategoryCheckResult } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon.tsx';

interface CheckCategoryProps {
  data: ApiBeatmapSetCheckResult;
  showMinor: boolean;
  selectedCategory?: string;
  overrideResult?: ApiCategoryCheckResult;
}

const CheckCategory: React.FC<CheckCategoryProps> = ({ data, showMinor, selectedCategory, overrideResult }) => {
  const categoryData = React.useMemo(() => {
    // If we have an override result for the selected category, use it
    if (overrideResult && overrideResult.category === selectedCategory) {
      const groups = groupChecks(overrideResult.checkResults, showMinor);
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
        name: overrideResult.category,
        checks: overrideResult.checkResults,
        mode: overrideResult.mode,
        difficultyLevel: overrideResult.difficultyLevel,
        starRating: overrideResult.starRating,
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
        {categoryData.mode && <GameModeIcon mode={categoryData.mode} size={24} starRating={categoryData.starRating} />}
        <Title order={5}>{categoryData.name}</Title>
        {categoryData.difficultyLevel && categoryData.name !== 'General' && (
          <Badge size="xs" color="grape" variant="light">
            {categoryData.difficultyLevel}
          </Badge>
        )}
        {categoryData.starRating != null && categoryData.starRating > 0 && categoryData.name !== 'General' && (
          <Badge size="xs" color="blue" variant="light" leftSection={<IconStarFilled size={10} />}>
            {categoryData.starRating.toFixed(2)}
          </Badge>
        )}
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
            <CheckGroup key={g.id} id={g.id} items={g.items} name={data.checks[g.id]?.name} />
          ))}
        </Stack>
      ) : (
        <Text>No issues found.</Text>
      )}
    </Stack>
  );
};

export default CheckCategory;
