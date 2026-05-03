import {
  Alert,
  Box,
  Flex,
  LoadingOverlay,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  useMantineTheme,
} from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { useEffect, useMemo, useState } from 'react';
import { DifficultyChartCard } from './DifficultyChartCard.tsx';
import {
  buildCharts,
  MODE_ORDER,
  normalizeMode,
  type DifficultyModeGroup,
} from './difficultyChartModel.ts';
import { DifficultyGameModeSelector } from './DifficultyGameModeSelector.tsx';
import { SummaryCard } from './DifficultySummaryCards.tsx';
import { useDifficultyOverview } from './hooks/useDifficultyOverview.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import type { DifficultyOverviewDifficulty, Mode } from '../../../Types';

interface DifficultyOverviewProps {
  reloadFlag: number;
}

function DifficultyOverview({ reloadFlag }: DifficultyOverviewProps) {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const [selectedMode, setSelectedMode] = useState<Mode | undefined>();
  const { data, isLoading, isError, error, refetch } = useDifficultyOverview({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    refetch();
  }, [reloadFlag]);

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

  useEffect(() => {
    if (groupedDifficulties.length === 0) {
      setSelectedMode(undefined);
      return;
    }

    if (!selectedMode || !groupedDifficulties.some((group) => group.mode === selectedMode)) {
      setSelectedMode(groupedDifficulties[0].mode);
    }
  }, [groupedDifficulties, selectedMode]);

  const selectedGroup =
    groupedDifficulties.find((group) => group.mode === selectedMode) ?? groupedDifficulties[0];
  const selectedDifficulties = selectedGroup?.difficulties ?? [];
  const charts = useMemo(
    () => buildCharts(selectedDifficulties, data?.msPerPeak),
    [data?.msPerPeak, selectedDifficulties]
  );
  const starRatingChart = charts.find((c) => c.title === 'Star Rating');
  const sliderVelocityChart = charts.find((c) => c.title === 'Slider velocity');
  const skillCharts = charts.filter(
    (c) => c.title !== 'Star Rating' && c.title !== 'Slider velocity'
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
    return (
      <Alert icon={<IconAlertTriangle />} color="yellow" title="No beatmapset selected">
        <Text size="sm">Select a beatmapset from the sidebar to analyze difficulty data.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />

      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing difficulty overview">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>
                {error.stackTrace}
              </Text>
            )}
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
              subValue="400ms strain windows"
            />
          </SimpleGrid>

          <Stack gap="md">
            {charts.length > 0 ? (
              <>
                {starRatingChart && <DifficultyChartCard chart={starRatingChart} />}
                {sliderVelocityChart && <DifficultyChartCard chart={sliderVelocityChart} />}
                {skillCharts.length > 0 && (
                  <>
                    <Text fw={600} c={theme.colors.gray[2]}>
                      Skill Strain Analysis
                    </Text>
                    <SimpleGrid cols={{ base: 1, lg: 2, xl: 3 }} spacing="md">
                      {skillCharts.map((chart) => (
                        <DifficultyChartCard key={chart.title} chart={chart} />
                      ))}
                    </SimpleGrid>
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
