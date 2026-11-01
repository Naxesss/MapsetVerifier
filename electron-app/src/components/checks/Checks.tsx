import { Alert, Text, Flex, Collapse, Stack } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import React, { useCallback, useMemo } from 'react';
import ChecksResults from './ChecksResults';
import DifficultyInfo from './DifficultyInfo';
import DifficultyLevelOverride from './DifficultyLevelOverride';
import BeatmapHeader from '../common/BeatmapHeader';
import DifficultyPicker, { GENERAL_TAB_ID, PICKER_ICON_SIZE } from '../common/DifficultyPicker';
import { useBeatmapChecks } from './hooks/useBeatmapChecks';
import { useDifficultyOverride } from './hooks/useDifficultyOverride';
import { getCategoryHighestLevel, getHighestLevel } from './utils/levelUtils';
import { useBeatmap } from '../../context/BeatmapContext';
import { useRegisterBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { useSettings } from '../../context/SettingsContext';
import { ApiCategoryCheckResult, Level } from '../../Types';
import { resolveDevOnlySetting } from '../../utils/devSettings';
import { ListSkeleton } from '../common/LoadingSkeletons.tsx';
import StackTraceMessage from '../common/StackTraceMessage.tsx';
import LevelIcon from '../icons/LevelIcon';

function Checks() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const showCheckSpeedStats = resolveDevOnlySetting(settings.showCheckSpeedStats);
  const [selectedCategory, setSelectedCategory] = React.useState<string | undefined>('General');
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
  const levelsLoading = isLoading;

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

  const levelOf = (category: string): Level => categoryHighestLevels[category] ?? 'Check';
  const levelIcon = (level: Level) => (
    <LevelIcon level={level} size={PICKER_ICON_SIZE} loading={levelsLoading} />
  );
  const pickerDifficulties = difficultiesForTabs.map((diff) => ({
    id: diff.category,
    label: diff.category,
    mode: diff.mode ?? 'Standard',
    starRating: diff.starRating,
    status: levelIcon(levelOf(diff.category)),
  }));

  // A selected difficulty that no longer exists (e.g. after a reparse) falls back to General.
  if (
    selectedCategory &&
    selectedCategory !== 'General' &&
    difficultiesForTabs.length > 0 &&
    !difficultiesForTabs.some((difficulty) => difficulty.category === selectedCategory)
  ) {
    setSelectedCategory('General');
  }

  return (
    <>
      <BeatmapHeader>
        {difficultiesForTabs.length > 0 && (
          <DifficultyPicker
            difficulties={pickerDifficulties}
            general={{ status: levelIcon(levelOf(GENERAL_TAB_ID)) }}
            modeStatus={(_, ids) => levelIcon(getHighestLevel(ids.map(levelOf)))}
            selectedId={selectedCategory}
            onSelect={setSelectedCategory}
          />
        )}
      </BeatmapHeader>
      {isError && (
        <Alert icon={<IconAlertCircle />} color="red" title="Couldn't run the checks" m="md">
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
        // pt="sm": the selected difficulty row describes the picker's selection, so it follows
        // the header's row gap instead of the larger gap before page content.
        <Flex gap="sm" px="md" pb="md" pt="sm" direction="column" bg="dark.6">
          {(isLoading || isFetching) && (
            <ChecksResults isLoading progress={progress} {...checkResultsSharedProps} />
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
                  levelControl={
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
