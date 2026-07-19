import {
  Box,
  Button,
  Group,
  Modal,
  Paper,
  SegmentedControl,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  useMantineTheme,
} from '@mantine/core';
import { IconArrowsMaximize } from '@tabler/icons-react';
import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type PointerEvent,
} from 'react';
import {
  DifficultyChartPanel,
  INLINE_PLOT_HEIGHT,
  MODAL_PLOT_HEIGHT,
} from './DifficultyChartPanel.tsx';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext';
import { ChartHoverFloatingPanel } from '../../charts/timeSeries/ChartHoverFloatingPanel.tsx';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type { ChartDefinition, ChartRow } from './difficultyChartModel.ts';
import type { DifficultyChartState } from './hooks/useDifficultyChartState.ts';
import type { DifficultySpikeDisplayMode } from '../../../context/SettingsContext';
import type { ChartHoverPayload } from '../../charts/timeSeries/types.ts';

function formatChartDuration(durationMs: number) {
  return formatChartTime(Math.max(0, Math.round(durationMs / 1000)));
}

function formatSeconds(seconds: number) {
  return formatChartTime(Math.max(0, Math.round(seconds)));
}

function formatStrainResolution(msPerPeak: number): string {
  const s = msPerPeak / 1000;
  const trimmed = s.toFixed(3).replace(/\.?0+$/, '');
  return `${trimmed}s`;
}

function MetricStat({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <Text size="xs" c="dimmed">
        {label}
      </Text>
      <Text fw={600}>{value}</Text>
    </Stack>
  );
}

function computePeakFromRows(
  rows: ChartRow[],
  seriesKeys: string[]
): { maxValue: number; peakTimeSeconds: number } {
  let best: { value: number; timeSeconds: number } | null = null;

  for (const row of rows) {
    const timeMs = row.timeMs;
    if (typeof timeMs !== 'number') continue;

    const timeSeconds = timeMs / 1000;

    for (const key of seriesKeys) {
      const v = row[key];
      if (typeof v !== 'number') continue;

      if (!best || v > best.value) {
        best = { value: v, timeSeconds };
      }
    }
  }

  return {
    maxValue: best?.value ?? 0,
    peakTimeSeconds: best?.timeSeconds ?? 0,
  };
}

function formatChartMetricValue(value: number, valueSuffix: string | undefined): string {
  if (valueSuffix === '%') {
    return `${Math.round(value)}${valueSuffix}`;
  }
  return `${value.toFixed(2)}${valueSuffix ?? ''}`;
}

const SPIKE_DISPLAY_MODE_OPTIONS: { value: DifficultySpikeDisplayMode; label: string }[] = [
  { value: 'both', label: 'Both' },
  { value: 'starRatingOnly', label: 'Star Rating only' },
  { value: 'spikesOnly', label: 'Spikes only' },
];

type DifficultyChartCardProps = {
  chart: ChartDefinition;
  chartState: DifficultyChartState;
};

export function DifficultyChartCard({ chart, chartState }: DifficultyChartCardProps) {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings, setSettings } = useSettings();
  const [ignoreLowVolume, setIgnoreLowVolume] = useState(true);
  // Mirrors settings.difficultySpikeDisplayMode locally so the control responds instantly instead
  // of waiting on the settings context (shared app-wide and persisted to disk on every change) to
  // re-render this chart-heavy page. Synced during render (not an effect) when the setting
  // changes externally, e.g. from the Settings page - see https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes.
  const [prevSettingValue, setPrevSettingValue] = useState(settings.difficultySpikeDisplayMode);
  const [displayMode, setDisplayModeLocal] = useState(settings.difficultySpikeDisplayMode);
  if (prevSettingValue !== settings.difficultySpikeDisplayMode) {
    setPrevSettingValue(settings.difficultySpikeDisplayMode);
    setDisplayModeLocal(settings.difficultySpikeDisplayMode);
  }

  const setDisplayMode = useCallback(
    (value: DifficultySpikeDisplayMode) => {
      setDisplayModeLocal(value);
      setSettings((prev) => ({ ...prev, difficultySpikeDisplayMode: value }));
    },
    [setSettings]
  );
  const [modalOpened, setModalOpened] = useState(false);
  const [hover, setHover] = useState<ChartHoverPayload | null>(null);
  const [hoverSafeZone, setHoverSafeZone] = useState(false);
  const modalBodyRef = useRef<HTMLDivElement>(null);
  const [lastHover, setLastHover] = useState<ChartHoverPayload | null>(null);
  const hoverSafeZoneRef = useRef(false);

  useLayoutEffect(() => {
    hoverSafeZoneRef.current = hoverSafeZone;
  }, [hoverSafeZone]);

  useEffect(() => {
    let cancelled = false;

    void Promise.resolve().then(() => {
      if (cancelled) return;
      setIgnoreLowVolume(true);
      setModalOpened(false);
      setHover(null);
      setHoverSafeZone(false);
      setLastHover(null);
    });

    return () => {
      cancelled = true;
    };
  }, [folder]);

  const handleChartHover = useCallback((payload: ChartHoverPayload | null) => {
    if (payload) {
      setLastHover(payload);
      setHover(payload);
      return;
    }

    if (!hoverSafeZoneRef.current) {
      setHover(null);
    }
  }, []);

  const effectiveHover = hover ?? (hoverSafeZone ? lastHover : null);

  const handleModalBodyPointerLeave = useCallback((event: PointerEvent<HTMLDivElement>) => {
    const related = event.relatedTarget;
    if (related instanceof Node && modalBodyRef.current?.contains(related)) {
      return;
    }

    setHoverSafeZone(false);
    setHover(null);
    setLastHover(null);
  }, []);

  const hasSpikeSeries = useMemo(
    () => chart.series.some((item) => item.hideFromLegend),
    [chart.series]
  );

  const visibleSeries = useMemo(() => {
    if (!hasSpikeSeries) {
      return chart.series;
    }

    switch (displayMode) {
      case 'starRatingOnly':
        return chart.series.filter((item) => !item.hideFromLegend);
      case 'spikesOnly':
        // Promote the spike companions to first-class series: show them in the legend (using the
        // same label as the primary line, since it's the only line shown) and let them toggle
        // independently instead of following the (now-hidden) primary line.
        return chart.series
          .filter((item) => item.hideFromLegend)
          .map((item) => ({
            ...item,
            label: item.label.replace(/ \(spike\)$/, ''),
            hideFromLegend: false,
            visibilityId: undefined,
          }));
      case 'both':
      default:
        return chart.series;
    }
  }, [chart.series, displayMode, hasSpikeSeries]);

  const displayData = useMemo(() => {
    const threshold = chart.hideLowValuesThreshold;
    if (threshold === undefined || !ignoreLowVolume) {
      return chart.data;
    }

    const keys = visibleSeries.map((item) => item.key);
    const carry: Record<string, number | null> = {};
    for (const key of keys) {
      carry[key] = null;
    }

    return chart.data.map((row) => {
      const next: ChartRow = { ...row };
      for (const key of keys) {
        const v = next[key];
        if (typeof v !== 'number') continue;

        if (v > threshold) {
          carry[key] = v;
        } else if (carry[key] !== null) {
          next[key] = carry[key];
        } else {
          next[key] = null;
        }
      }
      return next;
    });
  }, [chart.data, chart.hideLowValuesThreshold, ignoreLowVolume, visibleSeries]);

  const seriesConfig = useMemo(
    () =>
      visibleSeries.map((item) => ({
        id: item.id,
        key: item.key,
        label: item.label,
        color: item.color,
        dashed: item.dashed,
        visibilityId: item.visibilityId,
        hideFromLegend: item.hideFromLegend,
        hoverKey: item.hoverKey,
      })),
    [visibleSeries]
  );

  const { peakValue, peakAtSeconds } = useMemo(() => {
    const threshold = chart.hideLowValuesThreshold;
    if ((threshold === undefined || !ignoreLowVolume) && !hasSpikeSeries) {
      return { peakValue: chart.maxValue, peakAtSeconds: chart.peakTimeSeconds };
    }

    const keys = visibleSeries.map((item) => item.key);
    const { maxValue, peakTimeSeconds } = computePeakFromRows(displayData, keys);
    return { peakValue: maxValue, peakAtSeconds: peakTimeSeconds };
  }, [
    chart.hideLowValuesThreshold,
    chart.maxValue,
    chart.peakTimeSeconds,
    displayData,
    hasSpikeSeries,
    ignoreLowVolume,
    visibleSeries,
  ]);

  const panelProps = {
    data: displayData,
    series: seriesConfig,
    durationMs: chart.durationMs,
    valueSuffix: chart.valueSuffix,
    interpolation: chart.interpolation,
    showDataPoints: chart.showDataPoints,
    chartState,
    hover: modalOpened ? effectiveHover : hover,
    onHover: modalOpened ? handleChartHover : setHover,
  };

  return (
    <>
      <Paper p="md" radius="md" bg={theme.colors.dark[5]} style={{ overflow: 'visible' }}>
        <Stack gap="sm">
          <Group justify="space-between" align="center" wrap="nowrap">
            <Text fw={600}>{chart.title}</Text>
            <Group gap="md" wrap="nowrap">
              {hasSpikeSeries && (
                <SegmentedControl
                  size="xs"
                  data={SPIKE_DISPLAY_MODE_OPTIONS}
                  value={displayMode}
                  onChange={(value) => setDisplayMode(value as DifficultySpikeDisplayMode)}
                />
              )}
              {chart.data.length > 0 ? (
                <Button
                  leftSection={<IconArrowsMaximize size={16} />}
                  variant="light"
                  size="sm"
                  onClick={() => setModalOpened(true)}
                >
                  Full view
                </Button>
              ) : null}
            </Group>
          </Group>

          <SimpleGrid cols={chart.showResolution ? 4 : 3} spacing="md">
            <MetricStat label="Peak" value={formatChartMetricValue(peakValue, chart.valueSuffix)} />
            <MetricStat label="Peak at" value={formatSeconds(peakAtSeconds)} />
            <MetricStat label="Duration" value={formatChartDuration(chart.durationMs)} />
            {chart.showResolution ? (
              <MetricStat label="Resolution" value={formatStrainResolution(chart.msPerPeak)} />
            ) : null}
          </SimpleGrid>

          {chart.hideLowValuesThreshold !== undefined && (
            <Group justify="flex-end">
              <Switch
                size="sm"
                checked={ignoreLowVolume}
                onChange={(e) => setIgnoreLowVolume(e.currentTarget.checked)}
                label={`Ignore ≤${chart.hideLowValuesThreshold}% volume`}
                styles={{
                  track: {
                    backgroundColor: ignoreLowVolume ? undefined : theme.colors.dark[8],
                  },
                }}
              />
            </Group>
          )}

          {chart.data.length > 0 ? (
            modalOpened ? (
              <Text size="sm" c="dimmed" ta="center" py="md">
                Chart open in full view
              </Text>
            ) : (
              <DifficultyChartPanel {...panelProps} plotHeight={INLINE_PLOT_HEIGHT} />
            )
          ) : (
            <Text c="dimmed" ta="center" py="xl">
              No chart data available
            </Text>
          )}
        </Stack>
      </Paper>

      <Modal
        opened={modalOpened}
        onClose={() => {
          setModalOpened(false);
          setHover(null);
          setHoverSafeZone(false);
          setLastHover(null);
        }}
        title={chart.title}
        size="100%"
        centered
      >
        {chart.data.length > 0 ? (
          <Box
            ref={modalBodyRef}
            pos="relative"
            style={{ minHeight: MODAL_PLOT_HEIGHT + 80 }}
            onPointerLeave={handleModalBodyPointerLeave}
          >
            <DifficultyChartPanel
              {...panelProps}
              plotHeight={MODAL_PLOT_HEIGHT}
              showZoomHint={false}
              showInlineHoverTooltip={false}
            />
            <ChartHoverFloatingPanel
              boundsRef={modalBodyRef}
              hover={effectiveHover?.peak ?? null}
              valueSuffix={chart.valueSuffix}
              resetKey={`${folder ?? ''}:${chart.title}`}
              onSafeZoneChange={setHoverSafeZone}
            />
          </Box>
        ) : null}
      </Modal>
    </>
  );
}
