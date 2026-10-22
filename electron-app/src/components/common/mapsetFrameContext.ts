import { createContext, useContext } from 'react';

/** Lets the page's BeatmapHeader tell the MapsetFrame where the header ends, so the art fits it. */
export const MapsetFrameContext = createContext<((el: HTMLElement | null) => void) | undefined>(
  undefined
);

export function useMapsetHeaderRef() {
  const registerHeader = useContext(MapsetFrameContext);
  if (!registerHeader) throw new Error('BeatmapHeader must be rendered inside MapsetFrame');
  return registerHeader;
}
