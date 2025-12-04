import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { FetchError } from '../../../client/ApiHelper';
import DocumentationApi from '../../../client/DocumentationApi';
import { ApiDocumentationCheck, Mode } from '../../../Types';

export function useDocumentationChecks() {
  const generalQuery = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationGeneralChecks'],
    queryFn: DocumentationApi.getGeneralDocumentation,
    staleTime: Infinity,
  });

  const standardQuery = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationBeatmapChecks', 'Standard'],
    queryFn: () => DocumentationApi.getBeatmapDocumentation('Standard'),
    staleTime: Infinity,
  });

  const taikoQuery = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationBeatmapChecks', 'Taiko'],
    queryFn: () => DocumentationApi.getBeatmapDocumentation('Taiko'),
    staleTime: Infinity,
  });

  const catchQuery = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationBeatmapChecks', 'Catch'],
    queryFn: () => DocumentationApi.getBeatmapDocumentation('Catch'),
    staleTime: Infinity,
  });

  const maniaQuery = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationBeatmapChecks', 'Mania'],
    queryFn: () => DocumentationApi.getBeatmapDocumentation('Mania'),
    staleTime: Infinity,
  });

  const allChecks = useMemo(() => {
    const checks: ApiDocumentationCheck[] = [];
    if (generalQuery.data) checks.push(...generalQuery.data);
    if (standardQuery.data) checks.push(...standardQuery.data);
    if (taikoQuery.data) checks.push(...taikoQuery.data);
    if (catchQuery.data) checks.push(...catchQuery.data);
    if (maniaQuery.data) checks.push(...maniaQuery.data);
    return checks;
  }, [generalQuery.data, standardQuery.data, taikoQuery.data, catchQuery.data, maniaQuery.data]);

  const checksById = useMemo(() => {
    const map = new Map<number, ApiDocumentationCheck>();
    for (const check of allChecks) {
      map.set(check.id, check);
    }
    return map;
  }, [allChecks]);

  const getCheckById = (id: number): ApiDocumentationCheck | undefined => {
    return checksById.get(id);
  };

  const isLoading =
    generalQuery.isLoading ||
    standardQuery.isLoading ||
    taikoQuery.isLoading ||
    catchQuery.isLoading ||
    maniaQuery.isLoading;

  const isError =
    generalQuery.isError ||
    standardQuery.isError ||
    taikoQuery.isError ||
    catchQuery.isError ||
    maniaQuery.isError;

  return {
    allChecks,
    checksById,
    getCheckById,
    isLoading,
    isError,
    generalChecks: generalQuery.data,
    beatmapChecks: {
      Standard: standardQuery.data,
      Taiko: taikoQuery.data,
      Catch: catchQuery.data,
      Mania: maniaQuery.data,
    },
  };
}

export function useBeatmapDocumentationChecks(mode: Mode) {
  const query = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationBeatmapChecks', mode],
    queryFn: () => DocumentationApi.getBeatmapDocumentation(mode),
    staleTime: Infinity,
  });

  return {
    checks: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  };
}

export function useGeneralDocumentationChecks() {
  const query = useQuery<ApiDocumentationCheck[], FetchError>({
    queryKey: ['documentationGeneralChecks'],
    queryFn: DocumentationApi.getGeneralDocumentation,
    staleTime: Infinity,
  });

  return {
    checks: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  };
}

