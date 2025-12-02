import { Badge, Group, Stack, Text, Title } from '@mantine/core';
import React from 'react';
import CheckGroup from './CheckGroup.tsx';
import { groupChecks } from './groupChecks';
import { ApiBeatmapSetCheckResult, ApiCategoryCheckResult, Level } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import LevelIcon from '../icons/LevelIcon.tsx';

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
      const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
      const allItems = groups.flatMap((g) => g.items);
      const normalizedLevels = allItems.map((i) =>
        i.level === 'Check' ? 'Info' : i.level
      ) as Level[];
      let categoryHighest: Level = 'Info';
      for (const lvl of severityOrder) {
        if (normalizedLevels.includes(lvl)) {
          categoryHighest = lvl;
          break;
        }
      }
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
        groups: sortedGroups,
        categoryHighest,
        total,
        minorCount,
      };
    }

    const allCategories = [
      {
        name: 'General',
        checks: data.general.checkResults,
        mode: data.general.mode,
        difficultyLevel: data.general.difficultyLevel,
      },
      ...data.difficulties.map((d) => ({
        name: d.category,
        checks: d.checkResults,
        mode: d.mode,
        difficultyLevel: d.difficultyLevel,
      })),
    ];

    const cat = allCategories.find((c) => c.name === selectedCategory) ?? allCategories[0];
    const groups = groupChecks(cat.checks, showMinor);
    const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
    const allItems = groups.flatMap((g) => g.items);
    const normalizedLevels = allItems.map((i) =>
      i.level === 'Check' ? 'Info' : i.level
    ) as Level[];
    let categoryHighest: Level = 'Info';
    for (const lvl of severityOrder) {
      if (normalizedLevels.includes(lvl)) {
        categoryHighest = lvl;
        break;
      }
    }
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
    return { ...cat, groups: sortedGroups, categoryHighest, total, minorCount };
  }, [
    data.general.checkResults,
    data.difficulties,
    data.general.mode,
    data.general.difficultyLevel,
    showMinor,
    data.checks,
    selectedCategory,
    overrideResult,
  ]);

  return (
    <Stack gap="md">
      <Group wrap="nowrap" gap="xs" align="center">
        <LevelIcon level={categoryData.categoryHighest} size={24} />
        {categoryData.mode && <GameModeIcon mode={categoryData.mode} size={24} />}
        <Title order={5}>{categoryData.name}</Title>
        {categoryData.difficultyLevel && categoryData.name !== 'General' && (
          <Badge size="xs" color="grape" variant="light">
            {categoryData.difficultyLevel}
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
