import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import SnapshotApi from '../../../client/SnapshotApi';
import { ApiSnapshotComparison } from '../../../Types';

interface UseSnapshotCompareArgs {
  setKey?: string;
  baseId?: string;
  targetId?: string;
}

/** What changed between two snapshots. Snapshots never change, so a comparison is cached for good. */
export function useSnapshotCompare({ setKey, baseId, targetId }: UseSnapshotCompareArgs) {
  return useQuery<ApiSnapshotComparison, FetchError>({
    queryKey: ['snapshot-compare', setKey, baseId, targetId],
    queryFn: () => SnapshotApi.compare(setKey!, baseId!, targetId!),
    enabled: !!setKey && !!baseId && !!targetId && baseId !== targetId,
    staleTime: Infinity,
    retry: false,
    refetchOnWindowFocus: false,
    // Keep showing the last comparison while the next one loads, but only of the same mapset:
    // another mapset's changes must never appear under this one's name.
    placeholderData: (previous, previousQuery) =>
      previousQuery?.queryKey[1] === setKey ? previous : undefined,
  });
}
