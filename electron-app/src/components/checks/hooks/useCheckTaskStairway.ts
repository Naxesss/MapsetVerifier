import { useEffect, useRef, useState } from 'react';
import type { CheckProgress } from '../../../Types';

export type StairwayTask = {
  id: number;
  label: string;
};

export const MAX_STAIRWAY_STEPS = 5;

export function useCheckTaskStairway(progress: CheckProgress | null) {
  const [steps, setSteps] = useState<StairwayTask[]>([]);
  const [startOrder, setStartOrder] = useState<string[]>([]);
  const prevActiveRef = useRef<Set<string>>(new Set());
  const nextIdRef = useRef(0);
  const labelToIdRef = useRef<Map<string, number>>(new Map());

  useEffect(() => {
    let cancelled = false;

    void Promise.resolve().then(() => {
      if (cancelled) return;

      if (!progress) {
        setSteps([]);
        setStartOrder([]);
        prevActiveRef.current = new Set();
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
      }

      setStartOrder((current) => {
        const next = current.filter((label) => !completed.includes(label));
        return started.length > 0 ? [...next, ...started] : next;
      });

      const startedIds: { label: string; id: number }[] = [];
      for (const label of started) {
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
    });

    return () => {
      cancelled = true;
    };
  }, [progress]);

  const activeSet = progress ? new Set(progress.activeLabels) : new Set<string>();

  const primaryActiveLabel = (() => {
    for (let i = startOrder.length - 1; i >= 0; i--) {
      const label = startOrder[i];
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
