import { createContext, useContext } from 'react';
import type { Mode } from '../../Types';

/** Lets the page's BeatmapHeader tell the MapsetFrame where the header ends, so the art fits it. */
export const MapsetFrameContext = createContext<((el: HTMLElement | null) => void) | undefined>(
  undefined
);

export function useMapsetHeaderRef() {
  const registerHeader = useContext(MapsetFrameContext);
  if (!registerHeader) throw new Error('BeatmapHeader must be rendered inside MapsetFrame');
  return registerHeader;
}

/** The guest mapper of every difficulty of the mode shown in the difficulty picker. */
export type ModeMapper = { mode: Mode; mapper: string };

/** Lets the difficulty picker tell the MapsetFrame who mapped the whole mode shown, if one did. */
export const ModeMapperContext = createContext<
  ((mapper: ModeMapper | undefined) => void) | undefined
>(undefined);
