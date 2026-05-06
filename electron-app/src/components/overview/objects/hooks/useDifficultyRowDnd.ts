import {
  MeasuringStrategy,
  PointerSensor,
  closestCorners,
  type DragEndEvent,
  type DragStartEvent,
  useSensor,
  useSensors,
} from '@dnd-kit/core';

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

  const handleDragStart = (_event: DragStartEvent) => {
    stopPanning();
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const sourceKey = String(event.active.id);
    const targetKey = event.over ? String(event.over.id) : null;

    if (targetKey && sourceKey !== targetKey) {
      moveDifficulty(sourceKey, targetKey);
    }
  };

  const handleDragCancel = () => {};

  return {
    sensors,
    collisionDetection: closestCorners,
    autoScroll: false as const,
    measuring: {
      droppable: {
        strategy: MeasuringStrategy.Always,
      },
    },
    handleDragStart,
    handleDragEnd,
    handleDragCancel,
  };
}
