import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import SnapshotApi from '../../../client/SnapshotApi';
import { ApiSnapshotWindow } from '../../../Types';

export interface SnapshotWindowContext {
  setKey: string;
  baseId: string;
  targetId: string;
  difficultyKey: string;
}

/**
 * A few objects of a difficulty around a change, before and after. Snapshots never change, so a
 * window is cached for good; while the next one loads the last stays on screen.
 */
export function useSnapshotWindow(
  context: SnapshotWindowContext,
  anchor: number,
  anchorEnd: number,
  /** How many objects the window has been scrolled from the change. */
  offset: number
) {
  return useQuery<ApiSnapshotWindow, FetchError>({
    queryKey: [
      'snapshot-window',
      context.setKey,
      context.baseId,
      context.targetId,
      context.difficultyKey,
      anchor,
      anchorEnd,
      offset,
    ],
    queryFn: () =>
      SnapshotApi.window(
        context.setKey,
        context.baseId,
        context.targetId,
        context.difficultyKey,
        anchor,
        anchorEnd,
        offset
      ),
    staleTime: Infinity,
    retry: false,
    refetchOnWindowFocus: false,
    placeholderData: keepPreviousData,
  });
}
