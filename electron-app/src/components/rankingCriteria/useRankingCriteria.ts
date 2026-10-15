import { useQueries, useQuery } from '@tanstack/react-query';
import RankingCriteriaApi from '../../client/RankingCriteriaApi';
import { ApiRcOverview, ApiRcPage, ApiRcStatement } from '../../Types';

export function useRankingCriteriaOverview() {
  return useQuery<ApiRcOverview, Error>({
    queryKey: ['rankingCriteriaOverview'],
    queryFn: RankingCriteriaApi.getOverview,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });
}

export function useRankingCriteriaPage(key: string | null | undefined) {
  return useQuery<ApiRcPage, Error>({
    queryKey: ['rankingCriteriaPage', key],
    queryFn: () => RankingCriteriaApi.getPage(key!),
    enabled: Boolean(key),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });
}

/** Every page with statements, so search and the summary can span the whole ranking criteria. */
export function useAllRankingCriteriaPages(overview: ApiRcOverview | undefined) {
  const keys = overview?.pages.filter((page) => page.hasStatements).map((page) => page.key) ?? [];

  const results = useQueries({
    queries: keys.map((key) => ({
      queryKey: ['rankingCriteriaPage', key],
      queryFn: () => RankingCriteriaApi.getPage(key),
      staleTime: Infinity,
      refetchOnWindowFocus: false,
    })),
  });

  const pages = results.map((result) => result.data).filter((page): page is ApiRcPage => !!page);
  const isLoading = !overview || results.some((result) => result.isLoading);
  const isError = results.some((result) => result.isError);

  const statements: ApiRcStatement[] = pages.flatMap((page) => page.statements);

  return { pages, statements, isLoading, isError };
}
