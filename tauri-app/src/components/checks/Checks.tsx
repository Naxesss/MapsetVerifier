import {
  Alert,
  Text,
  Box,
  useMantineTheme,
  Group,
  SegmentedControl,
  Tooltip,
  Button,
  Flex,
  Loader,
  Divider
} from '@mantine/core';
import { useParams } from 'react-router-dom';
import BeatmapHeader from './BeatmapHeader';
import ChecksResults from './ChecksResults';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { useSettings } from '../../context/SettingsContext.tsx';
import React, {useEffect, useMemo} from 'react';
import LevelIcon from "../icons/LevelIcon.tsx";
import { ApiCheckResult, Level, Mode } from '../../Types';
import {getDifficultyColor} from "../common/DifficultyColor.ts";

const MODE_ORDER: Record<Mode, number> = {
  Standard: 0,
  Taiko: 1,
  Catch: 2,
  Mania: 3,
};

function getCategoryHighestLevel(checks: ApiCheckResult[], showMinor: boolean): Level {
  if (!Array.isArray(checks) || checks.length === 0) return 'Info';
  const effective = showMinor ? checks : checks.filter((c) => c.level !== 'Minor');
  const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
  const normalizedLevels = effective.map((c) => (c.level === 'Check' ? 'Info' : c.level)) as Level[];
  for (const lvl of severityOrder) {
    if (normalizedLevels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

function Checks() {
  const theme = useMantineTheme();
  const { folder } = useParams();
  const { settings } = useSettings();
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
  
  useEffect(() => {
    // Reset selected category when changing beatmap
    if (folder) {
      setSelectedCategory('General');
    }
  }, [folder])

  const { data, isLoading, isError, error, beatmapFolderPath } = useBeatmapChecks({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder);

  const {
    overrides,
    applyOverride,
    clearOverride,
    getOverrideResult,
    getOverrideLevel,
    isLoading: isOverrideLoading,
  } = useDifficultyOverride({ beatmapFolderPath });

  const difficultyLevels = ['Easy', 'Normal', 'Hard', 'Insane', 'Expert'];

  const selectedDifficulty = data?.difficulties?.find((d) => d.category === selectedCategory);
  const currentOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  const currentOverrideLevel = selectedCategory ? getOverrideLevel(selectedCategory) : undefined;

  const difficultyOverrideControls = selectedDifficulty ? (
    <Group gap="sm" justify="flex-start" wrap="wrap">
      {(() => {
        const current = selectedDifficulty.difficultyLevel || 'Unknown';
        const selected = currentOverrideLevel || current;
        return (
          <Group gap="md" align="center">
            <Text size="sm">Difficulty Level</Text>
            <Tooltip label={`Override difficulty for ${selectedDifficulty.category}`} withArrow withinPortal>
              <SegmentedControl
                radius="md"
                data={difficultyLevels.map((lvl) => ({ label: lvl, value: lvl }))}
                value={selected}
                onChange={(val) => {
                  const mapped = val === 'Ultra' ? 'Expert' : val;
                  const isDefault = mapped === current || (current === 'Expert' && val === 'Ultra');
                  if (isDefault) {
                    clearOverride(selectedDifficulty.category);
                  } else {
                    applyOverride(selectedDifficulty.category, val);
                  }
                }}
                fullWidth={false}
                styles={{ root: { maxWidth: '100%' } }}
              />
            </Tooltip>
            {isOverrideLoading && <Loader size="xs" />}
          </Group>
        );
      })()}
    </Group>
  ) : null;

  const categoryHighestLevels = useMemo(() => {
    if (!data) return {};
    const levels: Record<string, Level> = {};
    levels['General'] = getCategoryHighestLevel(data.general.checkResults, settings.showMinor);
    for (const diff of data.difficulties) {
      // Check if there's an override result for this category
      const overrideResult = overrides[diff.category]?.result;
      if (overrideResult) {
        levels[diff.category] = getCategoryHighestLevel(overrideResult.checkResults, settings.showMinor);
      } else {
        levels[diff.category] = getCategoryHighestLevel(diff.checkResults, settings.showMinor);
      }
    }
    return levels;
  }, [data, settings.showMinor, overrides]);

  const sortedDifficulties = useMemo(() => {
    if (!data?.difficulties) return [];
    return [...data.difficulties].sort((a, b) => {
      // Primary sort: by game mode order
      const modeOrderA = a.mode ? MODE_ORDER[a.mode] ?? 999 : 999;
      const modeOrderB = b.mode ? MODE_ORDER[b.mode] ?? 999 : 999;
      if (modeOrderA !== modeOrderB) {
        return modeOrderA - modeOrderB;
      }
      // Secondary sort: by star rating (ascending)
      const starA = a.starRating ?? 0;
      const starB = b.starRating ?? 0;
      return starA - starB;
    });
  }, [data?.difficulties]);

  const categoryButtons = data?.difficulties ? (
    <Group gap="xs" wrap="wrap">
      <Button
        p="xs"
        variant={selectedCategory === 'General' ? 'outline' : 'light'}
        onClick={() => setSelectedCategory('General')}
      >
        <Flex gap="xs" align="center">
          <LevelIcon level={categoryHighestLevels['General'] || 'Info'} size={20} />
          General
        </Flex>
      </Button>
      {sortedDifficulties.map((diff) => (
        <Button
          key={diff.category}
          p="xs"
          variant={selectedCategory === diff.category ? 'light' : 'light'}
          onClick={() => setSelectedCategory(diff.category)}
          color={getDifficultyColor(diff.starRating ?? 0)}
        >
          <Flex gap="xs" align="center">
            <LevelIcon level={categoryHighestLevels[diff.category] || 'Info'} size={20} />
            {diff.category}
          </Flex>
        </Button>
      ))}
    </Group>
  ) : null;

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to run beatmap checks.</Text>
      </Alert>
    );
  }

  return (
    <Box
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <BeatmapHeader data={data} beatmapFolderPath={beatmapFolderPath} bgUrl={bgUrl} />
      <Flex gap="md" m="md" direction="column">
        {categoryButtons}
        {difficultyOverrideControls}
        <Divider />
        <ChecksResults
          data={data}
          isLoading={isLoading}
          isError={isError}
          error={error}
          showMinor={settings.showMinor}
          selectedCategory={selectedCategory}
          overrideResult={currentOverrideResult}
        />
      </Flex>
    </Box>
  );
}

export default Checks;
