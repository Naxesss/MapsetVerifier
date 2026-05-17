import { useMemo } from 'react';
import { useDocumentation } from '../../../context/DocumentationContext';
import { ApiDocumentationCheck, Mode } from '../../../Types';

export function useDocumentationChecks() {
  const {
    status,
    error,
    generalChecks,
    beatmapChecks,
    allChecks,
    checksById,
    getCheckById,
  } = useDocumentation();

  const isLoading = status === 'idle' || status === 'loading';
  const isError = status === 'error';

  return {
    allChecks,
    checksById,
    getCheckById,
    isLoading,
    isError,
    generalChecks,
    beatmapChecks: {
      Standard: beatmapChecks.Standard,
      Taiko: beatmapChecks.Taiko,
      Catch: beatmapChecks.Catch,
      Mania: beatmapChecks.Mania,
    },
    documentationError: error,
  };
}

export function useBeatmapDocumentationChecks(mode: Mode) {
  const { status, error, beatmapChecks } = useDocumentation();

  const checks = useMemo(
    (): ApiDocumentationCheck[] | undefined => beatmapChecks[mode],
    [beatmapChecks, mode]
  );

  const isLoading = status === 'idle' || status === 'loading';
  const isError = status === 'error';

  return {
    checks,
    isLoading,
    isError,
    error,
  };
}

export function useGeneralDocumentationChecks() {
  const { status, error, generalChecks } = useDocumentation();

  const isLoading = status === 'idle' || status === 'loading';
  const isError = status === 'error';

  return {
    checks: generalChecks,
    isLoading,
    isError,
    error,
  };
}
