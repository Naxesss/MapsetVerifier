import { useCallback, useMemo } from 'react';
import { useDifficultyRowDnd } from './useDifficultyRowDnd.ts';
import { useDifficultyRowVisibility } from './useDifficultyRowVisibility.ts';
import { usePerModeDifficultyOrder } from './usePerModeDifficultyOrder.ts';
import { useTimelineZoom } from './useTimelineZoom.ts';
import { useSettings } from '../../../../context/SettingsContext.tsx';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { TimelineControllerValue } from '../context/types.ts';
import type { ObjectsModeGroup } from '../types.ts';

export type UseObjectsTimelineControllerInput = {
  startTimeMs: number;
  endTimeMs: number;
  groupedDifficulties: ObjectsModeGroup[];
  difficulties: ObjectsOverviewDifficulty[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  stopPanning: () => void;
};

export function useObjectsTimelineController({
  startTimeMs,
  endTimeMs,
  groupedDifficulties,
  difficulties,
  selectedMode,
  onModeChange,
  stopPanning,
}: UseObjectsTimelineControllerInput): TimelineControllerValue {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const { zoom, setZoom, tickIntervalMs, timelineWidth, adjustZoom, formatZoomLabel } =
    useTimelineZoom(durationMs);
  const { visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible } =
    useDifficultyRowVisibility(groupedDifficulties);
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
  const { settings, setSettings } = useSettings();
  const timelineThemeVariant = settings.timelineThemeVariant;
  const { orderedDifficulties, moveDifficulty } = usePerModeDifficultyOrder({
    groupedDifficulties,
    activeMode,
    difficulties,
  });
  const {
    sensors,
    collisionDetection,
    autoScroll,
    measuring,
    handleDragStart,
    handleDragEnd,
    handleDragCancel,
  } = useDifficultyRowDnd({
    moveDifficulty,
    stopPanning,
  });

  const setTimelineThemeVariant = useCallback(
    (variant: typeof timelineThemeVariant) => {
      setSettings((prev) => ({ ...prev, timelineThemeVariant: variant }));
    },
    [setSettings]
  );

  const scale = useMemo(
    () => ({ startTimeMs, endTimeMs, durationMs }),
    [startTimeMs, endTimeMs, durationMs]
  );

  const zoomSlice = useMemo(
    () => ({
      zoom,
      setZoom,
      tickIntervalMs,
      timelineWidth,
      adjustZoom,
      formatZoomLabel,
    }),
    [zoom, setZoom, tickIntervalMs, timelineWidth, adjustZoom, formatZoomLabel]
  );

  const mode = useMemo(
    () => ({
      groupedDifficulties,
      selectedMode,
      onModeChange,
      activeMode,
    }),
    [groupedDifficulties, selectedMode, onModeChange, activeMode]
  );

  const rows = useMemo(() => ({ orderedDifficulties }), [orderedDifficulties]);

  const visibility = useMemo(
    () => ({
      visibilityByDifficulty,
      toggleDifficultyVisibility,
      setManyVisible,
    }),
    [visibilityByDifficulty, toggleDifficultyVisibility, setManyVisible]
  );

  const display = useMemo(
    () => ({
      timelineThemeVariant,
      setTimelineThemeVariant,
    }),
    [timelineThemeVariant, setTimelineThemeVariant]
  );

  const dnd = useMemo(
    () => ({
      sensors,
      collisionDetection,
      autoScroll,
      measuring,
      onDragStart: handleDragStart,
      onDragEnd: handleDragEnd,
      onDragCancel: handleDragCancel,
    }),
    [
      sensors,
      collisionDetection,
      autoScroll,
      measuring,
      handleDragStart,
      handleDragEnd,
      handleDragCancel,
    ]
  );

  return useMemo(
    () => ({
      scale,
      zoom: zoomSlice,
      mode,
      rows,
      visibility,
      display,
      dnd,
    }),
    [scale, zoomSlice, mode, rows, visibility, display, dnd]
  );
}
