import { useMutation } from '@tanstack/react-query';
import React from 'react';
import { FetchError } from '../../../client/ApiHelper';
import BeatmapApi from '../../../client/BeatmapApi';
import { ApiCategoryOverrideCheckResult } from '../../../Types';

interface UseDifficultyOverrideArgs {
  beatmapFolderPath?: string;
}

interface OverrideState {
  [difficultyName: string]: {
    overrideLevel: string;
    result: ApiCategoryOverrideCheckResult;
  };
}

export function useDifficultyOverride({ beatmapFolderPath }: UseDifficultyOverrideArgs) {
  const [overrides, setOverrides] = React.useState<OverrideState>({});
  /** The level each difficulty is being re-run at right now. */
  const [pendingLevels, setPendingLevels] = React.useState<Record<string, string>>({});
  // The latest choice per difficulty wins: each run gets an id, and a run only applies its result
  // while it is still the latest one for its difficulty. Choosing another level, going back to the
  // default or a reset makes older runs stale, so they are ignored when they finish.
  const nextRequestIdRef = React.useRef(0);
  const latestRequestIdsRef = React.useRef<Record<string, number>>({});

  const {
    mutateAsync,
    error,
    reset: resetMutation,
  } = useMutation<
    ApiCategoryOverrideCheckResult,
    FetchError,
    { difficultyName: string; overrideDifficulty: string }
  >({
    mutationFn: async ({ difficultyName, overrideDifficulty }) => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapApi.runCheckOverride(beatmapFolderPath, difficultyName, overrideDifficulty);
    },
  });

  const invalidateRequest = (difficultyName: string) => {
    const requestId = ++nextRequestIdRef.current;
    latestRequestIdsRef.current[difficultyName] = requestId;
    return requestId;
  };

  const removePending = (difficultyName: string) => {
    setPendingLevels((prev) => {
      if (!(difficultyName in prev)) return prev;
      const next = { ...prev };
      delete next[difficultyName];
      return next;
    });
  };

  const applyOverride = (difficultyName: string, overrideDifficulty: string) => {
    const requestId = invalidateRequest(difficultyName);
    const isLatest = () => latestRequestIdsRef.current[difficultyName] === requestId;
    setPendingLevels((prev) => ({ ...prev, [difficultyName]: overrideDifficulty }));

    mutateAsync({ difficultyName, overrideDifficulty })
      .then((result) => {
        if (!isLatest()) return;
        setOverrides((prev) => ({
          ...prev,
          [difficultyName]: { overrideLevel: overrideDifficulty, result },
        }));
      })
      // The error is kept by the mutation; the switch falls back to the last applied level.
      .catch(() => {})
      .finally(() => {
        if (isLatest()) removePending(difficultyName);
      });
  };

  const clearOverride = (difficultyName: string) => {
    invalidateRequest(difficultyName);
    removePending(difficultyName);
    setOverrides((prev) => {
      const next = { ...prev };
      delete next[difficultyName];
      return next;
    });
  };

  const clearAllOverrides = React.useCallback(() => {
    setOverrides({});
  }, []);

  const getOverrideResult = React.useCallback(
    (difficultyName: string): ApiCategoryOverrideCheckResult | undefined => {
      return overrides[difficultyName]?.result;
    },
    [overrides]
  );

  const getOverrideLevel = React.useCallback(
    (difficultyName: string): string | undefined => {
      return overrides[difficultyName]?.overrideLevel;
    },
    [overrides]
  );

  /** The level a difficulty is being re-run at, shown as chosen while its checks load. */
  const getPendingLevel = React.useCallback(
    (difficultyName: string): string | undefined => pendingLevels[difficultyName],
    [pendingLevels]
  );

  const reset = React.useCallback(() => {
    latestRequestIdsRef.current = {};
    setOverrides({});
    setPendingLevels({});
    resetMutation();
  }, [resetMutation]);

  return {
    overrides,
    applyOverride,
    clearOverride,
    clearAllOverrides,
    getOverrideResult,
    getOverrideLevel,
    getPendingLevel,
    error,
    reset,
  };
}
