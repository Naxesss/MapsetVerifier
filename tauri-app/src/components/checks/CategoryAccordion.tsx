import React from 'react';
import { Accordion, Badge, Group, Stack, Text, Title } from '@mantine/core';
import LevelIcon from '../icons/LevelIcon.tsx';
import {ApiBeatmapSetCheckResult, Level} from '../../Types';
import { groupChecks } from './groupChecks';
import CheckGroup from "./CheckGroup.tsx";
import GameModeIcon from "../icons/GameModeIcon.tsx";

interface CategoryAccordionProps {
  data: ApiBeatmapSetCheckResult;
  showMinor: boolean;
}

const CategoryAccordion: React.FC<CategoryAccordionProps> = ({ data, showMinor }) => {
  const categoriesData = React.useMemo(() => {
    return [
      { name: 'General', checks: data.general.checkResults, mode: data.general.mode, difficultyLevel: data.general.difficultyLevel },
      ...data.difficulties.map(d => ({ name: d.category, checks: d.checkResults, mode: d.mode, difficultyLevel: d.difficultyLevel }))
    ].map(cat => {
      const groups = groupChecks(cat.checks, showMinor);
      const severityOrder: Level[] = ['Error','Problem','Warning','Minor','Info'];
      const allItems = groups.flatMap(g => g.items);
      const normalizedLevels = allItems.map(i => i.level === 'Check' ? 'Info' : i.level) as Level[];
      let categoryHighest: Level = 'Info';
      for (const lvl of severityOrder) {
        if (normalizedLevels.includes(lvl)) { categoryHighest = lvl; break; }
      }
      const minorCount = allItems.filter(i => i.level === 'Minor').length;
      const total = groups.reduce((sum, g) => sum + g.items.length, 0) - minorCount;
      // Sort groups: primary by check name (case-insensitive); categoryHighest is identical per group so doesn't impact ordering.
      const sortedGroups = [...groups].sort((a, b) => {
        const nameA = (data.checks[a.id]?.name ?? '').toLowerCase();
        const nameB = (data.checks[b.id]?.name ?? '').toLowerCase();
        if (nameA && nameB) return nameA.localeCompare(nameB);
        if (nameA) return -1; // nameA exists, nameB empty
        if (nameB) return 1;  // nameB exists, nameA empty
        return 0;
      });
      return { ...cat, groups: sortedGroups, categoryHighest, total, minorCount };
    });
  }, [data.general.checkResults, data.difficulties, data.general.mode, data.general.difficultyLevel, showMinor, data.checks]);

  return (
    <Accordion variant="contained" multiple>
      {categoriesData.map(cat => (
        <Accordion.Item key={cat.name} value={cat.name}>
          <Accordion.Control>
            <Group wrap="nowrap" gap="xs" align="center">
              <LevelIcon level={cat.categoryHighest} size={24} />
              {cat.mode && (
                <GameModeIcon mode={cat.mode} size={24} />
              )}
              <Title order={5}>{cat.name}</Title>
              {cat.difficultyLevel && (
                <Badge size="xs" color="grape" variant="light">{cat.difficultyLevel}</Badge>
              )}
              <Badge size="xs" color={cat.total ? 'red' : 'green'} variant="light">{cat.total} issue{cat.total !== 1 ? 's' : ''}</Badge>
              {showMinor && cat.minorCount > 0 && (
                <Badge size="xs" color="yellow" variant="light">{cat.minorCount} minor</Badge>
              )}
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            {cat.total > 0 || cat.minorCount > 0 ? (
              <Stack gap="0">
                {cat.groups.map(g => (
                  <CheckGroup
                    key={g.id}
                    id={g.id}
                    items={g.items}
                    name={data.checks[g.id]?.name}
                  />
                ))}
              </Stack>
            ) : (
              <Text>No issues found.</Text>
            )}
          </Accordion.Panel>
        </Accordion.Item>
      ))}
    </Accordion>
  );
};

export default CategoryAccordion;
