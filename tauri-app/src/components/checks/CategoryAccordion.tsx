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
      const total = groups.reduce((sum, g) => sum + g.items.length, 0);
      return { ...cat, groups, categoryHighest, total };
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
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            {cat.total > 0 ? (
              <Stack gap="xs">
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
