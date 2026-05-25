import { Alert, Box, Flex, LoadingOverlay, SimpleGrid, Text } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { useEffect, useMemo } from 'react';
import ObjectPercentagesOverview from './components/ObjectPercentagesOverview.tsx';
import ObjectsTimelineComparison from './components/ObjectsTimelineComparison.tsx';
import SnappingsOverview from './components/SnappingsOverview.tsx';
import { isHitsoundViewAvailable } from './hitsoundUtils.ts';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';
import { useObjectsOverviewModeSelection } from './hooks/useObjectsOverviewModeSelection.ts';
import { formatDuration, formatTime } from './timelineUtils.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { usePageHints } from '../../../context/PageHintsContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { type Mode, type ObjectsOverviewDifficulty } from '../../../Types';
import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode';
import NoBeatmapsetDisplay from '../../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';
import { SummaryCard } from '../difficulty/DifficultySummaryCards.tsx';
import type { ObjectsModeGroup } from './types.ts';

function ObjectsOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { setObjectsHasHitsoundModes } = usePageHints();
  const { settings } = useSettings();
  const { data, isLoading, isError, error } = useObjectsAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const groupedDifficulties = useMemo<ObjectsModeGroup[]>(() => {
    if (!data?.success) return [];

    const grouped = new Map<Mode, ObjectsOverviewDifficulty[]>();
    for (const difficulty of data.difficulties) {
      const mode = normalizeMode(difficulty.mode);
      const modeDifficulties = grouped.get(mode);

      if (modeDifficulties) {
        modeDifficulties.push(difficulty);
      } else {
        grouped.set(mode, [difficulty]);
      }
    }

    return MODE_ORDER.filter((mode) => grouped.has(mode)).map((mode) => ({
      mode,
      difficulties: grouped.get(mode) ?? [],
    }));
  }, [data]);

  const hasHitsoundModes = useMemo(
    () => groupedDifficulties.some((group) => isHitsoundViewAvailable(group.mode)),
    [groupedDifficulties]
  );

  useEffect(() => {
    setObjectsHasHitsoundModes(hasHitsoundModes);
    return () => setObjectsHasHitsoundModes(false);
  }, [hasHitsoundModes, setObjectsHasHitsoundModes]);

  const { selectedMode, setSelectedMode, selectedGroup } =
    useObjectsOverviewModeSelection(groupedDifficulties);

  const summary = useMemo(() => {
    if (!data?.success) return null;

    return data.difficulties.reduce(
      (accumulator, difficulty) => {
        accumulator.objectCount += difficulty.objectCount;
        accumulator.edgeCount += difficulty.edgeCount;
        accumulator.unsnappedCount += difficulty.unsnappedCount;
        return accumulator;
      },
      { objectCount: 0, edgeCount: 0, unsnappedCount: 0 }
    );
  }, [data]);

  if (!folder) {
    return <NoBeatmapsetDisplay />;
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />

      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing objects">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert icon={<IconAlertTriangle />} color="yellow" title="Analysis failed">
            <Text size="sm">{data.errorMessage}</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && summary && (
        <Flex gap="md" p="md" direction="column">
          <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
            <SummaryCard label="Difficulties" value={String(data.difficulties.length)} />
            <SummaryCard label="Hit objects" value={summary.objectCount.toLocaleString()} />
            <SummaryCard
              label={`Timeline range (${formatDuration(data.endTimeMs - data.startTimeMs)})`}
              value={`${formatTime(data.startTimeMs)} -> ${formatTime(data.endTimeMs)}`}
            />
          </SimpleGrid>

          <ObjectsTimelineComparison
            startTimeMs={data.startTimeMs}
            endTimeMs={data.endTimeMs}
            groupedDifficulties={groupedDifficulties}
            difficulties={selectedGroup?.difficulties ?? []}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
          />
          <SnappingsOverview
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
            difficulties={selectedGroup?.difficulties ?? []}
          />
          <ObjectPercentagesOverview
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
            difficulties={selectedGroup?.difficulties ?? []}
          />
        </Flex>
      )}
    </Box>
  );
}

export default ObjectsOverview;
