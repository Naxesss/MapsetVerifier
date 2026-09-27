import { Alert, Text, Group, Flex, Collapse, Stack } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import React, { useCallback, useMemo } from 'react';
import BeatmapActionButtons from './BeatmapActionButtons';
import ChecksResults from './ChecksResults';
import DifficultyInfo from './DifficultyInfo';
import DifficultyLevelOverride from './DifficultyLevelOverride';
import GameModeSelector from './GameModeSelector';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyTabSelector, { GENERAL_TAB_ID } from '../common/DifficultyTabSelector';
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
import { resolveDevOnlySetting } from '../../utils/devSettings';
import { ListSkeleton } from '../common/LoadingSkeletons.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';

function Checks() {
  const { selectedFolder: folder, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const { settings } = useSettings();
  const showCheckSpeedStats = resolveDevOnlySetting(settings.showCheckSpeedStats);
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
  const [selectedMode, setSelectedMode] = React.useState<Mode | undefined>();
  const checkResultsTransitionDurationMs = 320;

  const [prevFolder, setPrevFolder] = React.useState(folder);

  if (folder !== prevFolder) {
    setPrevFolder(folder);

    // Reset selected category when changing beatmap
    if (folder) {
      setSelectedCategory('General');
    }
  }

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
    includeCheckTimings: showCheckSpeedStats,
  });
  const areCheckResultsExpanded = !!data && !isLoading && !isFetching;
  const levelIconsLoading = isLoading;

  const {
    overrides,
    applyOverride,
    clearOverride,
    getOverrideResult,
    getOverrideLevel,
    getPendingLevel,
    reset: resetOverrides,
  } = useDifficultyOverride({ beatmapFolderPath });

  useRegisterBeatmapReparse(
    useCallback(() => {
      resetOverrides();
    }, [resetOverrides])
  );

  const dataDifficulties = data?.difficulties;
  const structureDifficulties = structure?.difficulties;

  const difficultiesForTabs = useMemo((): ApiCategoryCheckResult[] => {
    if (dataDifficulties) return dataDifficulties;
    if (!structureDifficulties) return [];

    return structureDifficulties.map((difficulty) => ({
      category: difficulty.category,
      beatmapId: difficulty.beatmapId ?? undefined,
      checkResults: [],
      mode: difficulty.mode,
      starRating: difficulty.starRating ?? null,
    }));
  }, [dataDifficulties, structureDifficulties]);

  const selectedDifficulty = difficultiesForTabs.find((d) => d.category === selectedCategory);
  const selectedOverrideResult = selectedCategory ? getOverrideResult(selectedCategory) : undefined;
  // While a new "Interpreted as" level runs, the switch already shows it and the results below
  // load like any other content.
  const pendingLevel = selectedCategory ? getPendingLevel(selectedCategory) : undefined;
  const isSelectedOverridePending = pendingLevel !== undefined;
  const currentOverrideLevel =
    pendingLevel ?? (selectedCategory ? getOverrideLevel(selectedCategory) : undefined);

  const handleCheckRunHistoryCleared = React.useCallback(() => {
    void refetch();
  }, [refetch]);

  const checkResultsSharedProps = {
    showMinor: settings.showMinor,
    hiddenMinorCheckIds: settings.hiddenMinorCheckIds,
    selectedCategory,
    showCheckRunDelta: settings.showCheckRunDelta,
    checkRunDeltaShowUnchanged: settings.checkRunDeltaShowUnchanged,
    beatmapFolderPath,
    onCheckRunHistoryCleared: handleCheckRunHistoryCleared,
    showCheckSpeedStats,
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

  if (groupedDifficulties.length > 0 && !selectedMode) {
    setSelectedMode(groupedDifficulties[0].mode);
  }

  const selectedGroup =
    groupedDifficulties.find((g) => g.mode === selectedMode) ?? groupedDifficulties[0];

  // A selected difficulty that no longer exists (e.g. after a reparse) falls back to General.
  if (
    selectedCategory &&
    selectedCategory !== 'General' &&
    difficultiesForTabs.length > 0 &&
    !difficultiesForTabs.some((difficulty) => difficulty.category === selectedCategory)
  ) {
    setSelectedCategory('General');
  }

  if (!folder) {
    return (
      <Alert
        icon={<IconAlertTriangle />}
        color="yellow"
        title="Song folder not set"
        withCloseButton
      >
        <Text size="sm">Please set the song folder in settings to run checks.</Text>
      </Alert>
    );
  }

  return (
    <>
      <BeatmapHeader>
        <Group gap="sm">
          <BeatmapActionButtons
            beatmapFolderPath={beatmapFolderPath}
            beatmapId={beatmapInfo?.beatmapId ?? undefined}
            beatmapSetId={beatmapInfo?.beatmapSetId ?? undefined}
            onReparse={triggerReparse}
          />
          {groupedDifficulties.length > 1 && (
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
                  difficulty={selectedDifficulty}
                  categoryHighestLevels={categoryHighestLevels}
                  currentOverrideResult={selectedOverrideResult}
                  actions={
                    // Shown with the row as soon as a difficulty is selected, so it doesn't pop in later.
                    selectedDifficulty && (
                      <DifficultyLevelOverride
                        selectedDifficulty={selectedDifficulty}
                        currentOverrideLevel={currentOverrideLevel}
                        onOverrideChange={(category, level) => {
                          if (level === null) {
                            clearOverride(category);
                          } else {
                            applyOverride(category, level);
                          }
                        }}
                      />
                    )
                  }
                />
                {isSelectedOverridePending ? (
                  <ListSkeleton rows={4} />
                ) : (
                  <ChecksResults
                    data={data}
                    isLoading={false}
                    isError={isError}
                    error={error}
                    overrideResult={selectedOverrideResult}
                    {...checkResultsSharedProps}
                  />
                )}
              </Stack>
            )}
          </Collapse>
        </Flex>
      )}
    </>
  );
}

export default Checks;
