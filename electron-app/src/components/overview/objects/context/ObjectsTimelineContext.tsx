import { createContext, useContext, type ReactNode } from 'react';
import { ROW_HEIGHT } from '../constants.ts';
import { DEFAULT_HITSOUND_LAYERS } from '../hitsoundUtils.ts';
import type {
  TimelineControllerValue,
  TimelineFullViewValue,
  TimelinePanValue,
} from './types.ts';

const TimelineControllerContext = createContext<TimelineControllerValue | null>(null);
const TimelinePanContext = createContext<TimelinePanValue | null>(null);
const TimelineFullViewContext = createContext<TimelineFullViewValue | null>(null);

export function TimelineControllerProvider({
  value,
  children,
}: {
  value: TimelineControllerValue;
  children: ReactNode;
}) {
  return (
    <TimelineControllerContext.Provider value={value}>
      {children}
    </TimelineControllerContext.Provider>
  );
}

export function TimelinePanProvider({
  value,
  children,
}: {
  value: TimelinePanValue;
  children: ReactNode;
}) {
  return (
    <TimelinePanContext.Provider value={value}>{children}</TimelinePanContext.Provider>
  );
}

export function TimelineFullViewProvider({
  value,
  children,
}: {
  value: TimelineFullViewValue;
  children: ReactNode;
}) {
  return (
    <TimelineFullViewContext.Provider value={value}>{children}</TimelineFullViewContext.Provider>
  );
}

export function useTimelineController(): TimelineControllerValue {
  const context = useContext(TimelineControllerContext);
  if (!context) {
    throw new Error('useTimelineController must be used within TimelineControllerProvider');
  }
  return context;
}

export function useTimelinePan(): TimelinePanValue {
  const context = useContext(TimelinePanContext);
  if (!context) {
    throw new Error('useTimelinePan must be used within TimelinePanProvider');
  }
  return context;
}

function useOptionalTimelineFullView(): TimelineFullViewValue | null {
  return useContext(TimelineFullViewContext);
}

export function useTimelineFullView(): TimelineFullViewValue {
  const context = useOptionalTimelineFullView();
  if (!context) {
    throw new Error('useTimelineFullView must be used within TimelineFullViewProvider');
  }
  return context;
}

export function useTimelineScale() {
  const { scale, zoom } = useTimelineController();
  return {
    startTimeMs: scale.startTimeMs,
    endTimeMs: scale.endTimeMs,
    durationMs: scale.durationMs,
    timelineWidth: zoom.timelineWidth,
    tickIntervalMs: zoom.tickIntervalMs,
  };
}

export function useTimelineDisplay() {
  const controller = useTimelineController();
  const fullView = useOptionalTimelineFullView();

  return {
    timelineThemeVariant: controller.display.timelineThemeVariant,
    viewMode: fullView?.viewMode ?? ('structure' as const),
    hitsoundLayers: fullView?.hitsoundLayers ?? DEFAULT_HITSOUND_LAYERS,
    rowHeight: fullView?.rowHeight ?? ROW_HEIGHT,
    playheadViewportX: fullView?.playheadViewportX ?? null,
  };
}

export function useTimelineVisibility() {
  return useTimelineController().visibility;
}
