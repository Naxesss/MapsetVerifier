import { Box, Transition } from '@mantine/core';
import { useState } from 'react';
import BeatmapCard from './BeatmapCard.tsx';
import { Beatmap } from '../../Types.ts';

export interface CurrentBeatmapData {
  beatmap: Beatmap;
  folderPath: string;
  lookupRoot: string;
}

interface CurrentBeatmapCardProps {
  current: CurrentBeatmapData | null;
  selectedFolderPath?: string;
  onSelectFolderPath: (folderPath?: string) => void;
}

const SWAP_DURATION_MS = 220;
const POP_DOWN_TRANSITION = {
  in: { opacity: 1, transform: 'scale(1) translateY(0)' },
  out: { opacity: 0, transform: 'scale(0.95) translateY(-12px)' },
  common: { transformOrigin: 'top center' },
  transitionProperty: 'transform, opacity',
} as const;

export default function CurrentBeatmapCard({
  current,
  selectedFolderPath,
  onSelectFolderPath,
}: CurrentBeatmapCardProps) {
  const [renderedCurrent, setRenderedCurrent] = useState<CurrentBeatmapData | null>(null);
  const [visible, setVisible] = useState(false);
  const [pending, setPending] = useState<CurrentBeatmapData | null>(null);
  const currentKey = current?.folderPath ?? null;
  const [prevKey, setPrevKey] = useState(currentKey);

  if (currentKey !== prevKey) {
    setPrevKey(currentKey);

    if (!current) {
      setPending(null);
      setVisible(false);
    } else if (!renderedCurrent) {
      setRenderedCurrent(current);
      setVisible(true);
    } else if (renderedCurrent.folderPath === current.folderPath) {
      setRenderedCurrent(current);
    } else {
      setPending(current);
      setVisible(false);
    }
  }

  return (
    <Transition
      mounted={visible && !!renderedCurrent}
      transition={POP_DOWN_TRANSITION}
      duration={SWAP_DURATION_MS}
      timingFunction="ease"
      onExited={() => {
        if (pending) {
          setRenderedCurrent(pending);
          setPending(null);
          setVisible(true);
        } else {
          setRenderedCurrent(null);
        }
      }}
    >
      {(styles) => (
        <div style={styles}>
          {renderedCurrent && (
            <Box mb="12px">
              <BeatmapCard
                beatmap={renderedCurrent.beatmap}
                songFolder={renderedCurrent.lookupRoot}
                isSelectedOverride={selectedFolderPath === renderedCurrent.folderPath}
                onSelect={() => onSelectFolderPath(renderedCurrent.folderPath)}
              />
            </Box>
          )}
        </div>
      )}
    </Transition>
  );
}
