import { useCallback, useState } from 'react';
import {
  TIMELINE_SCROLL_TICK_STEP_OPTIONS,
  type TimelineScrollTickStep,
} from '../constants.ts';

const DEFAULT_TICK_STEP: TimelineScrollTickStep = 2;

export function useTimelineScrollTickStep() {
  const [tickStep, setTickStepState] = useState<TimelineScrollTickStep>(DEFAULT_TICK_STEP);

  const setTickStep = useCallback((value: TimelineScrollTickStep) => {
    if (!TIMELINE_SCROLL_TICK_STEP_OPTIONS.includes(value)) {
      return;
    }

    setTickStepState(value);
  }, []);

  return { tickStep, setTickStep };
}
