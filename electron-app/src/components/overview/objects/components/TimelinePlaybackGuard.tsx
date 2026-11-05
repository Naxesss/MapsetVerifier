import { Box } from '@mantine/core';
import { useEffect, useRef } from 'react';
import { getWheelStepDirection } from '../hooks/useTimelineWheelSeek.ts';

interface TimelinePlaybackGuardProps {
  adjustZoom: (direction: -1 | 1) => void;
  onStep: (direction: -1 | 1) => void;
}

/**
 * Laid over the timeline while it plays, which blocks dragging, its scrollbar and hover cards in one
 * go. A plain scroll is left alone so that it still scrolls the page.
 */
export default function TimelinePlaybackGuard({ adjustZoom, onStep }: TimelinePlaybackGuardProps) {
  const ref = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    const handleWheel = (event: WheelEvent) => {
      if (event.ctrlKey || event.metaKey) {
        event.preventDefault();
        if (!event.shiftKey && event.deltaY !== 0) {
          adjustZoom(event.deltaY < 0 ? 1 : -1);
        }
        return;
      }

      const direction = getWheelStepDirection(event);
      if (direction !== null) {
        event.preventDefault();
        onStep(direction);
      }
    };

    element.addEventListener('wheel', handleWheel, { passive: false });
    return () => element.removeEventListener('wheel', handleWheel);
  }, [adjustZoom, onStep]);

  return <Box ref={ref} pos="absolute" inset={0} style={{ zIndex: 12 }} />;
}
