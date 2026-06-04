import { useEffect, useRef, useState } from 'react';
import type { CheckProgress } from '../../../Types';

export type StairwayTask = {
  id: number;
  label: string;
};

export const MAX_STAIRWAY_STEPS = 5;

export function useCheckTaskStairway(progress: CheckProgress | null) {
  const [steps, setSteps] = useState<StairwayTask[]>([]);
  const prevActiveRef = useRef<Set<string>>(new Set());
  const nextIdRef = useRef(0);
  const labelToIdRef = useRef<Map<string, number>>(new Map());
  const startOrderRef = useRef<string[]>([]);

  useEffect(() => {
    if (!progress) {
      setSteps([]);
      prevActiveRef.current = new Set();
      startOrderRef.current = [];
      labelToIdRef.current.clear();
      return;
    }

    const active = new Set(progress.activeLabels);
    const prev = prevActiveRef.current;
    const started = progress.activeLabels.filter((label) => !prev.has(label));
    const completed = [...prev].filter((label) => !active.has(label));

    prevActiveRef.current = active;

    for (const label of completed) {
      labelToIdRef.current.delete(label);
      startOrderRef.current = startOrderRef.current.filter((entry) => entry !== label);
    }

    const startedIds: { label: string; id: number }[] = [];
    for (const label of started) {
      startOrderRef.current.push(label);
      const id = ++nextIdRef.current;
      labelToIdRef.current.set(label, id);
      startedIds.push({ label, id });
    }

    if (startedIds.length === 0) return;

    setSteps((current) => {
      const next = [...current, ...startedIds.map(({ label, id }) => ({ id, label }))];

      while (next.length > MAX_STAIRWAY_STEPS) {
        next.shift();
      }

      return next;
    });
  }, [progress]);

  const activeSet = progress ? new Set(progress.activeLabels) : new Set<string>();

  const primaryActiveLabel = (() => {
    for (let i = startOrderRef.current.length - 1; i >= 0; i--) {
      const label = startOrderRef.current[i];
      if (activeSet.has(label)) return label;
    }
    return undefined;
  })();

  const placeholder = (() => {
    if (!progress) return 'Starting checks…';
    const { activeLabels, completed, total } = progress;
    if (activeLabels.length > 0) return undefined;
    return completed >= total && total > 0 ? 'Finishing…' : 'Starting checks…';
  })();

  return { steps, activeSet, primaryActiveLabel, placeholder };
}
