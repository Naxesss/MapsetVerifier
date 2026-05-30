import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { HitsoundLayerVisibility, TimelineViewMode } from '../hitsoundUtils.ts';
import type { TimelineThemeVariant } from '../timelineTheme/types.ts';
import type { ObjectsModeGroup } from '../types.ts';
import type {
  DragEndEvent,
  DragStartEvent,
  MeasuringConfiguration,
  SensorDescriptor,
  SensorOptions,
} from '@dnd-kit/core';
import type { RefObject } from 'react';

export type TimelineCrosshairState = {
  timestampMs: number;
};

export type TimelineScaleValue = {
  startTimeMs: number;
  endTimeMs: number;
  durationMs: number;
};

export type TimelineZoomValue = {
  zoom: number;
  setZoom: (value: number) => void;
  tickIntervalMs: number;
  timelineWidth: number;
  adjustZoom: (delta: -1 | 1) => void;
  formatZoomLabel: (value: number) => string;
};

export type TimelineModeValue = {
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  activeMode: Mode | undefined;
};

export type TimelineRowsValue = {
  orderedDifficulties: ObjectsOverviewDifficulty[];
};

export type TimelineVisibilityValue = {
  visibilityByDifficulty: Record<string, boolean | undefined>;
  toggleDifficultyVisibility: (difficulty: ObjectsOverviewDifficulty) => void;
  setManyVisible: (difficulties: ObjectsOverviewDifficulty[], visible: boolean) => void;
};

export type TimelineDisplaySettingsValue = {
  timelineThemeVariant: TimelineThemeVariant;
  setTimelineThemeVariant: (variant: TimelineThemeVariant) => void;
};

export type TimelineDndValue = {
  sensors: SensorDescriptor<SensorOptions>[];
  collisionDetection: typeof import('@dnd-kit/core').closestCorners;
  autoScroll: false;
  measuring: MeasuringConfiguration;
  onDragStart: (event: DragStartEvent) => void;
  onDragEnd: (event: DragEndEvent) => void;
  onDragCancel: () => void;
};

export type TimelineControllerValue = {
  scale: TimelineScaleValue;
  zoom: TimelineZoomValue;
  mode: TimelineModeValue;
  rows: TimelineRowsValue;
  visibility: TimelineVisibilityValue;
  display: TimelineDisplaySettingsValue;
  dnd: TimelineDndValue;
};

export type TimelinePanValue = {
  scrollRef: RefObject<HTMLDivElement | null>;
  isPanningTimeline: boolean;
  handleMouseDown: (event: React.MouseEvent<HTMLDivElement>) => void;
  handleMouseMove: (event: React.MouseEvent<HTMLDivElement>) => void;
  stopDragging: () => void;
};

export type TimelineCrosshairValue = {
  crosshair: TimelineCrosshairState | null;
  setCrosshair: (state: TimelineCrosshairState | null) => void;
};

export type TimelineFullViewValue = {
  viewMode: TimelineViewMode;
  setViewMode: (mode: TimelineViewMode) => void;
  hitsoundLayers: HitsoundLayerVisibility;
  setHitsoundLayers: React.Dispatch<React.SetStateAction<HitsoundLayerVisibility>>;
  playheadViewportX: number | null;
  snapPlayheadToTimestamp: ((timestampMs: number) => void) | null;
  rowHeight: number;
};
