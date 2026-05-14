import {
  MeasuringStrategy,
  PointerSensor,
  closestCorners,
  type DragEndEvent,
  type DragStartEvent,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import { useCallback } from 'react';

const DIFFICULTY_ROW_DND_MEASURING = {
  droppable: {
    strategy: MeasuringStrategy.Always,
  },
} as const;

export function useDifficultyRowDnd({
  moveDifficulty,
  stopPanning,
}: {
  moveDifficulty: (sourceKey: string, targetKey: string) => void;
  stopPanning: () => void;
}) {
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 6,
      },
    })
  );

  const handleDragStart = useCallback(
    (_event: DragStartEvent) => {
      stopPanning();
    },
    [stopPanning]
  );

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const sourceKey = String(event.active.id);
      const targetKey = event.over ? String(event.over.id) : null;

      if (targetKey && sourceKey !== targetKey) {
        moveDifficulty(sourceKey, targetKey);
      }
    },
    [moveDifficulty]
  );

  const handleDragCancel = useCallback(() => {}, []);

  return {
    sensors,
    collisionDetection: closestCorners,
    measuring: DIFFICULTY_ROW_DND_MEASURING,
    handleDragStart,
    handleDragEnd,
    handleDragCancel,
  };
}
