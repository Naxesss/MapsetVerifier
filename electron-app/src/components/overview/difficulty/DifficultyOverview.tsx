import {
  Alert,
  Box,
  Flex,
  Grid,
  LoadingOverlay,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  useMantineTheme,
} from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import { DifficultyChartCard } from './DifficultyChartCard.tsx';
import {
  buildCharts,
  MODE_ORDER,
  normalizeMode,
  SAMPLE_VOLUME_CHART_TITLE,
  type DifficultyModeGroup,
} from './difficultyChartModel.ts';
import { DifficultyGameModeSelector } from './DifficultyGameModeSelector.tsx';
import { SummaryCard } from './DifficultySummaryCards.tsx';
import { useDifficultyChartState } from './hooks/useDifficultyChartState.ts';
import { useDifficultyOverview } from './hooks/useDifficultyOverview.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import NoBeatmapsetDisplay from '../../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';
import type { DifficultyOverviewDifficulty, Mode } from '../../../Types';

const EMPTY_DIFFICULTIES: DifficultyOverviewDifficulty[] = [];

function DifficultyOverview() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();
  const { data, isLoading, isFetching, isError, error } = useDifficultyOverview({
    folder,
    songFolder: settings.songFolder,
  });

  const groupedDifficulties = useMemo<DifficultyModeGroup[]>(() => {
    if (!data?.success) {
      return [];
    }

    const grouped = new Map<Mode, DifficultyOverviewDifficulty[]>();

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

  if (groupedDifficulties.length === 0) {
    if (selectedMode !== undefined) {
      setSelectedMode(undefined);
    }
  } else if (!selectedMode || !groupedDifficulties.some((group) => group.mode === selectedMode)) {
    setSelectedMode(groupedDifficulties[0].mode);
  }

  const selectedGroup =
    groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0];
  const selectedDifficulties = selectedGroup?.difficulties ?? EMPTY_DIFFICULTIES;
  const charts = useMemo(
    () => buildCharts(selectedDifficulties, data?.msPerPeak),
    [data?.msPerPeak, selectedDifficulties]
  );

  const durationMs = charts[0]?.durationMs ?? data?.msPerPeak ?? 0;
  const chartResetKey = useMemo(
    () =>
      `${folder ?? ''}:${selectedGroup?.mode ?? ''}:${selectedDifficulties.map((d) => d.label).join('|')}`,
    [folder, selectedDifficulties, selectedGroup?.mode]
  );

  const chartState = useDifficultyChartState(durationMs, selectedDifficulties, chartResetKey);

  const starRatingChart = charts.find((c) => c.title === 'Star Rating');
  const sliderVelocityChart = charts.find((c) => c.title === 'Slider velocity');
  const sampleVolumeChart = charts.find((c) => c.title === SAMPLE_VOLUME_CHART_TITLE);
  const skillCharts = charts.filter(
    (c) =>
      c.title !== 'Star Rating' &&
      c.title !== 'Slider velocity' &&
      c.title !== SAMPLE_VOLUME_CHART_TITLE
  );
  const distinctSkillCount = useMemo(
    () =>
      new Set(
        selectedDifficulties.flatMap((difficulty) =>
          difficulty.skills.map((skill) => skill.skillName)
        )
      ).size,
    [selectedDifficulties]
  );

  if (!folder) {
    return <NoBeatmapsetDisplay />;
  }

  return (
    <Box>
      <LoadingOverlay
        visible={isLoading || isFetching}
        zIndex={1000}
        overlayProps={{ radius: 'sm', blur: 2 }}
      />
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing difficulty overview">
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

      {data && data.success && (
        <Flex gap="md" p="md" direction="column">
          <DifficultyGameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedGroup?.mode}
            onModeChange={setSelectedMode}
          />

          <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
            <SummaryCard label="Difficulties" value={String(selectedDifficulties.length)} />
            <SummaryCard
              label="Skill charts"
              value={String(skillCharts.length)}
              subValue={`${distinctSkillCount} unique skills`}
            />
            <SummaryCard
              label="Peak interval"
              value={`${(data.msPerPeak / 1000).toFixed(1)}s`}
              subValue={`${data.msPerPeak}ms strain windows`}
            />
          </SimpleGrid>

          <Stack gap="md">
            {charts.length > 0 ? (
              <>
                {starRatingChart && (
                  <DifficultyChartCard chart={starRatingChart} chartState={chartState} />
                )}
                {sliderVelocityChart && (
                  <DifficultyChartCard chart={sliderVelocityChart} chartState={chartState} />
                )}
                {sampleVolumeChart && (
                  <DifficultyChartCard chart={sampleVolumeChart} chartState={chartState} />
                )}
                {skillCharts.length > 0 && (
                  <>
                    <Text fw={600} c={theme.colors.gray[2]}>
                      Skill Strain Analysis
                    </Text>
                    <Grid grow gutter="md">
                      {skillCharts.map((chart) => (
                        <Grid.Col key={chart.title} span={{ base: 12, lg: 6, xl: 4 }}>
                          <DifficultyChartCard chart={chart} chartState={chartState} />
                        </Grid.Col>
                      ))}
                    </Grid>
                  </>
                )}
              </>
            ) : (
              <Paper p="md" radius="md" withBorder>
                <Text c="dimmed" ta="center">
                  No difficulty strain data available.
                </Text>
              </Paper>
            )}
          </Stack>
        </Flex>
      )}
    </Box>
  );
}

export default DifficultyOverview;
