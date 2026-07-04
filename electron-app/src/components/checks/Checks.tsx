import { Alert, Text, Box, useMantineTheme, Group, Flex, Collapse, Stack } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import React, { useCallback, useEffect, useMemo } from 'react';
import BeatmapActionButtons from './BeatmapActionButtons';
import ChecksResults from './ChecksResults';
import DifficultyInfo from './DifficultyInfo';
import DifficultyLevelOverride from './DifficultyLevelOverride';
import GameModeSelector from './GameModeSelector';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyTabSelector, { GENERAL_TAB_ID } from '../common/DifficultyTabSelector';
import { useBeatmapBackground } from './hooks/useBeatmapBackground';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { getCategoryHighestLevel } from './utils/levelUtils';
import { useBeatmap } from '../../context/BeatmapContext';
import {
  useBeatmapReparse,
  useRegisterBeatmapReparse,
} from '../../context/BeatmapReparseRegistry.tsx';
import { useSettings } from '../../context/SettingsContext';
import { ApiCategoryCheckResult, Level, Mode } from '../../Types';
import StackTraceMessage from '../common/StackTraceMessage.tsx';

function Checks() {
  const theme = useMantineTheme();
  const { selectedFolder: folder, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const { settings } = useSettings();
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
  const [displayedCategory, setDisplayedCategory] = React.useState<string | undefined>('General');
  const [isDifficultyContentVisible, setIsDifficultyContentVisible] = React.useState(true);
  const [hoveredDifficulty, setHoveredDifficulty] = React.useState<
    ApiCategoryCheckResult | undefined
  >(undefined);
  const [selectedMode, setSelectedMode] = React.useState<Mode | undefined>();
  const difficultyTransitionDurationMs = 220;
  const checkResultsTransitionDurationMs = 320;

  useEffect(() => {
    // Reset selected category when changing beatmap
    if (folder) {
      setSelectedCategory('General');
      setDisplayedCategory('General');
      setIsDifficultyContentVisible(true);
      setHoveredDifficulty(undefined);
    }
  }, [folder]);

  const {
    data,
    isLoading,
    isFetching,
    isError,
    error,
    beatmapFolderPath,
    progress,
    structure,
    refetch,
  } = useBeatmapChecks({
    folder,
    songFolder: settings.songFolder,
    includeCheckRunDelta: settings.showCheckRunDelta,
    createSnapshot: settings.autoCreateSnapshotOnCheckRun,
  });
  const areCheckResultsExpanded = !!data && !isLoading && !isFetching;
  const levelIconsLoading = isLoading;

  const { bgUrl } = useBeatmapBackground(folder, settings.songFolder);

  const {
    overrides,
    applyOverride,
    clearOverride,
    getOverrideResult,
    getOverrideLevel,
    isLoading: isOverrideLoading,
    reset: resetOverrides,
  } = useDifficultyOverride({ beatmapFolderPath });

  useRegisterBeatmapReparse(
    useCallback(() => {
      resetOverrides();
    }, [resetOverrides])
  );

  const difficultiesForTabs = useMemo((): ApiCategoryCheckResult[] => {
    if (data?.difficulties) return data.difficulties;
    if (!structure?.difficulties) return [];

    return structure.difficulties.map((difficulty) => ({
      category: difficulty.category,
      beatmapId: difficulty.beatmapId ?? undefined,
      checkResults: [],
      mode: difficulty.mode,
      starRating: difficulty.starRating ?? null,
    }));
  }, [data?.difficulties, structure?.difficulties]);

  const selectedDifficulty = difficultiesForTabs.find((d) => d.category === selectedCategory);
  const displayedDifficulty = data?.difficulties?.find((d) => d.category === displayedCategory);
  const selectedOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  const displayedOverrideResult = displayedCategory
    ? getOverrideResult(displayedCategory)
    : undefined;
  const currentOverrideLevel = displayedCategory ? getOverrideLevel(displayedCategory) : undefined;

  const handleDifficultyContentTransitionEnd = React.useCallback(() => {
    if (isDifficultyContentVisible) return;

    const nextCategory = selectedCategory ?? 'General';
    setDisplayedCategory(nextCategory);
    setIsDifficultyContentVisible(true);
  }, [isDifficultyContentVisible, selectedCategory]);

  const handleCheckRunHistoryCleared = React.useCallback(() => {
    void refetch();
  }, [refetch]);

  const checkResultsSharedProps = {
    showMinor: settings.showMinor,
    hiddenMinorCheckIds: settings.hiddenMinorCheckIds,
    selectedCategory: displayedCategory,
    showCheckRunDelta: settings.showCheckRunDelta,
    checkRunDeltaShowUnchanged: settings.checkRunDeltaShowUnchanged,
    beatmapFolderPath,
    onCheckRunHistoryCleared: handleCheckRunHistoryCleared,
  };

  const categoryHighestLevels = useMemo(() => {
    if (!data) return {};
    const levels: Record<string, Level> = {};
    levels['General'] = getCategoryHighestLevel(
      data.general.checkResults,
      settings.showMinor,
      settings.hiddenMinorCheckIds
    );
    for (const diff of data.difficulties) {
      // Check if there's an override result for this category
      const overrideResult = overrides[diff.category]?.result;
      if (overrideResult) {
        levels[diff.category] = getCategoryHighestLevel(
          overrideResult.categoryResult.checkResults,
          settings.showMinor,
          settings.hiddenMinorCheckIds
        );
      } else {
        levels[diff.category] = getCategoryHighestLevel(
          diff.checkResults,
          settings.showMinor,
          settings.hiddenMinorCheckIds
        );
      }
    }
    return levels;
  }, [data, settings.showMinor, settings.hiddenMinorCheckIds, overrides]);

  const groupedDifficulties = useMemo(() => {
    if (difficultiesForTabs.length === 0) return [];

    // Group difficulties by mode
    const modeGroups: Record<Mode, ApiCategoryCheckResult[]> = {
      Standard: [],
      Taiko: [],
      Catch: [],
      Mania: [],
    };

    for (const diff of difficultiesForTabs) {
      const mode = diff.mode ?? 'Standard';
      modeGroups[mode].push(diff);
    }

    // Sort each group by star rating (ascending)
    for (const mode of Object.keys(modeGroups) as Mode[]) {
      modeGroups[mode].sort((a, b) => (a.starRating ?? 0) - (b.starRating ?? 0));
    }

    // Create ordered array of mode groups (only include modes that have difficulties)
    const orderedModes: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

    return orderedModes
      .filter((mode) => modeGroups[mode].length > 0)
      .map((mode) => ({
        mode,
        difficulties: modeGroups[mode],
      }));
  }, [difficultiesForTabs]);

  useEffect(() => {
    if (groupedDifficulties.length > 0 && !selectedMode) {
      setSelectedMode(groupedDifficulties[0].mode);
    }
  }, [groupedDifficulties, selectedMode]);

  const selectedGroup =
    groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

  useEffect(() => {
    if (difficultiesForTabs.length === 0) return;

    const nextCategory = selectedCategory ?? 'General';
    const categoryExists =
      nextCategory === 'General' ||
      difficultiesForTabs.some((difficulty) => difficulty.category === nextCategory);

    if (!categoryExists) {
      setSelectedCategory('General');
      setDisplayedCategory('General');
      setIsDifficultyContentVisible(true);
      return;
    }

    if (!data) return;

    if (displayedCategory === nextCategory) {
      setIsDifficultyContentVisible(true);
      return;
    }

    setIsDifficultyContentVisible(false);
  }, [data, difficultiesForTabs, selectedCategory, displayedCategory]);

  if (!folder) {
    return (
      <Alert
        icon={<IconAlertTriangle />}
        color="yellow"
        title="Song folder not set"
        withCloseButton
      >
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
      <BeatmapHeader bgUrl={bgUrl}>
        <Group gap="sm">
          <BeatmapActionButtons
            beatmapFolderPath={beatmapFolderPath}
            beatmapId={beatmapInfo?.beatmapId ?? undefined}
            beatmapSetId={beatmapInfo?.beatmapSetId ?? undefined}
            onReparse={triggerReparse}
          />
          {groupedDifficulties.length > 0 && (
            <GameModeSelector
              groupedDifficulties={groupedDifficulties}
              selectedMode={selectedMode}
              onModeChange={setSelectedMode}
              categoryHighestLevels={categoryHighestLevels}
              levelLoading={levelIconsLoading}
            />
          )}
        </Group>
        {selectedGroup && (
          <DifficultyTabSelector
            tabs={selectedGroup.difficulties.map((diff) => ({
              id: diff.category,
              label: diff.category,
              starRating: diff.starRating,
              level: categoryHighestLevels[diff.category] ?? 'Check',
              levelLoading: levelIconsLoading,
            }))}
            selectedId={selectedCategory}
            onSelect={setSelectedCategory}
            activeOnHover
            hoveredId={hoveredDifficulty?.category}
            onHover={(id) =>
              setHoveredDifficulty(
                id && id !== GENERAL_TAB_ID
                  ? difficultiesForTabs.find((d) => d.category === id)
                  : undefined
              )
            }
            hoverRestoreId={selectedDifficulty?.category}
            highlightGeneralWhenIdle
            generalLevel={categoryHighestLevels[GENERAL_TAB_ID] ?? 'Check'}
            levelLoading={levelIconsLoading}
            showLevelIcons
          />
        )}
      </BeatmapHeader>
      {isError && (
        <Alert icon={<IconAlertCircle />} color="red" title="Error loading checks" m="md">
          <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
            {error?.message}
          </Text>
          {error?.details && (
            <Text mt="sm" size="xs" style={{ whiteSpace: 'pre-wrap' }}>
              {error.details}
            </Text>
          )}
          {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
        </Alert>
      )}
      {(isLoading || isFetching || data) && (
        <Flex gap="sm" p="md" direction="column" bg="dark.6">
          {(isLoading || isFetching) && (
            <ChecksResults
              isLoading
              isError={false}
              progress={progress}
              {...checkResultsSharedProps}
            />
          )}

          <Collapse
            in={areCheckResultsExpanded}
            transitionDuration={checkResultsTransitionDurationMs}
            animateOpacity
          >
            {data && (
              <Stack gap="sm">
                <DifficultyInfo
                  hoveredDifficulty={hoveredDifficulty}
                  selectedCategory={selectedCategory}
                  categoryHighestLevels={categoryHighestLevels}
                  currentOverrideResult={selectedOverrideResult}
                />
                <Collapse
                  in={isDifficultyContentVisible}
                  transitionDuration={difficultyTransitionDurationMs}
                  animateOpacity={false}
                  onTransitionEnd={handleDifficultyContentTransitionEnd}
                >
                  {displayedDifficulty && (
                    <DifficultyLevelOverride
                      selectedDifficulty={displayedDifficulty}
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
                    isLoading={false}
                    isError={isError}
                    error={error}
                    overrideResult={displayedOverrideResult}
                    {...checkResultsSharedProps}
                  />
                </Collapse>
              </Stack>
            )}
          </Collapse>
        </Flex>
      )}
    </Box>
  );
}

export default Checks;
