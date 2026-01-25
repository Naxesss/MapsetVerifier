import { useMutation } from '@tanstack/react-query';
import React from 'react';
import { FetchError } from '../../../client/ApiHelper';
import BeatmapApi from '../../../client/BeatmapApi';
import {ApiCategoryOverrideCheckResult} from '../../../Types';

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

  const mutation = useMutation<
    ApiCategoryOverrideCheckResult,
    FetchError,
    { difficultyName: string; overrideDifficulty: string }
  >({
    mutationFn: async ({ difficultyName, overrideDifficulty }) => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapApi.runCheckOverride(beatmapFolderPath, difficultyName, overrideDifficulty);
    },
    onSuccess: (result, { difficultyName, overrideDifficulty }) => {
      setOverrides((prev) => ({
        ...prev,
        [difficultyName]: {
          overrideLevel: overrideDifficulty,
          result,
        },
      }));
    },
  });

  const applyOverride = React.useCallback(
    (difficultyName: string, overrideDifficulty: string) => {
      mutation.mutate({ difficultyName, overrideDifficulty });
    },
    [mutation]
  );

  const clearOverride = React.useCallback((difficultyName: string) => {
    setOverrides((prev) => {
      const next = { ...prev };
      delete next[difficultyName];
      return next;
    });
  }, []);

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

  const reset = React.useCallback(() => {
    setOverrides({});
    mutation.reset();
  }, [mutation]);

  return {
    overrides,
    applyOverride,
    clearOverride,
    clearAllOverrides,
    getOverrideResult,
    getOverrideLevel,
    isLoading: mutation.isPending,
    error: mutation.error,
    reset,
  };
}

