import { Alert, Text, Box, useMantineTheme, Group, Flex, LoadingOverlay } from '@mantine/core';
import React, { useEffect, useMemo } from 'react';
import BeatmapActionButtons from './BeatmapActionButtons';
import BeatmapHeader from '../common/BeatmapHeader';
import ChecksResults from './ChecksResults';
import DifficultyInfo from './DifficultyInfo';
import DifficultyLevelOverride from './DifficultyLevelOverride';
import DifficultySelector from './DifficultySelector';
import GameModeSelector from './GameModeSelector';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { getCategoryHighestLevel } from './utils/levelUtils';
import { useBeatmap } from '../../context/BeatmapContext';
import { useSettings } from '../../context/SettingsContext';
import { ApiCategoryCheckResult, Level, Mode } from '../../Types';

function Checks() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
  const [hoveredDifficulty, setHoveredDifficulty] = React.useState<ApiCategoryCheckResult | undefined>(undefined);
  const [selectedMode, setSelectedMode] = React.useState<Mode | undefined>();
  
  useEffect(() => {
    // Reset selected category when changing beatmap
    if (folder) {
      setSelectedCategory('General');
      setHoveredDifficulty(undefined);
    }
  }, [folder])

  const { data, isLoading, isError, error, beatmapFolderPath, refetch } = useBeatmapChecks({
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
    reset: resetOverrides,
  } = useDifficultyOverride({ beatmapFolderPath });

  const selectedDifficulty = data?.difficulties?.find((d) => d.category === selectedCategory);
  const currentOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  const currentOverrideLevel = selectedCategory ? getOverrideLevel(selectedCategory) : undefined;

  const categoryHighestLevels = useMemo(() => {
    if (!data) return {};
    const levels: Record<string, Level> = {};
    levels['General'] = getCategoryHighestLevel(data.general.checkResults, settings.showMinor);
    for (const diff of data.difficulties) {
      // Check if there's an override result for this category
      const overrideResult = overrides[diff.category]?.result;
      if (overrideResult) {
        levels[diff.category] = getCategoryHighestLevel(overrideResult.categoryResult.checkResults, settings.showMinor);
      } else {
        levels[diff.category] = getCategoryHighestLevel(diff.checkResults, settings.showMinor);
      }
    }
    return levels;
  }, [data, settings.showMinor, overrides]);

  const groupedDifficulties = useMemo(() => {
    if (!data?.difficulties) return [];

    // Group difficulties by mode
    const modeGroups: Record<Mode, ApiCategoryCheckResult[]> = {
      Standard: [],
      Taiko: [],
      Catch: [],
      Mania: [],
    };

    for (const diff of data.difficulties) {
      const mode = diff.mode ?? 'Standard';
      modeGroups[mode].push(diff);
    }

    // Sort each group by star rating (ascending)
    for (const mode of Object.keys(modeGroups) as Mode[]) {
      modeGroups[mode].sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0));
    }

    // Create ordered array of mode groups (only include modes that have difficulties)
    const orderedModes: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

    
    const result = orderedModes
      .filter((mode) => modeGroups[mode].length > 0)
      .map((mode) => ({
        mode,
        difficulties: modeGroups[mode],
      }));
    setSelectedMode(result[0].mode)
    
    return result;
  }, [data?.difficulties]);
  const selectedGroup = groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

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
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      <BeatmapHeader title={data?.title} artist={data?.artist} creator={data?.creator} bgUrl={bgUrl}>
        <Group gap="sm">
          <BeatmapActionButtons
            beatmapFolderPath={beatmapFolderPath}
            beatmapSetId={data?.beatmapSetId}
            onReparse={async () => {
              if (!beatmapFolderPath) return;
              resetOverrides();
              await refetch();
            }}
          />
          <GameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode}
            onModeChange={setSelectedMode}
            categoryHighestLevels={categoryHighestLevels}
          />
        </Group>
        {data?.difficulties && selectedGroup && (
          <DifficultySelector
            difficulties={selectedGroup.difficulties}
            selectedCategory={selectedCategory}
            hoveredDifficulty={hoveredDifficulty}
            categoryHighestLevels={categoryHighestLevels}
            onSelectCategory={setSelectedCategory}
            onHoverDifficulty={setHoveredDifficulty}
            selectedDifficulty={selectedDifficulty}
          />
        )}
      </BeatmapHeader>
      {data && (
        <Flex gap="sm" p="md" direction="column">
          <DifficultyInfo
            hoveredDifficulty={hoveredDifficulty}
            selectedCategory={selectedCategory}
            categoryHighestLevels={categoryHighestLevels}
            currentOverrideResult={currentOverrideResult}
          />
          {selectedDifficulty && (
            <DifficultyLevelOverride
              selectedDifficulty={selectedDifficulty}
              currentOverrideLevel={currentOverrideLevel}
              isLoading={isOverrideLoading}
              onOverrideChange={(category, level) => {
                if (level === null) {
                  clearOverride(category);
                } else {
                  applyOverride(category, level);
                }
              }}
            />
          )}
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
      )}
    </Box>
  );
}

export default Checks;
