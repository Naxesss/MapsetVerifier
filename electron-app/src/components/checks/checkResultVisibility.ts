import type {
  ApiBeatmapSetCheckResult,
  ApiCategoryOverrideCheckResult,
  ApiCheckResult,
} from '../../Types';

export function pickVisibleCheckResults(
  checks: ApiCheckResult[] | undefined,
  options: { showMinor: boolean; hiddenMinorCheckIds: readonly number[] }
): ApiCheckResult[] {
  if (!Array.isArray(checks) || checks.length === 0) return [];

  const hiddenMinor = new Set(options.hiddenMinorCheckIds);

  return checks.filter((c) => {
    if (c.level !== 'Minor') return true;
    if (!options.showMinor) return false;
    return !hiddenMinor.has(c.id);
  });
}

/** Raw API results for the category shown in the checks list (matches CheckCategory override rules). */
export function getRawCheckResultsForSelectedCategory(
  data: ApiBeatmapSetCheckResult,
  selectedCategory: string | undefined,
  overrideResult?: ApiCategoryOverrideCheckResult
): ApiCheckResult[] {
  const overrideCategoryResult = overrideResult?.categoryResult;
  if (overrideCategoryResult && overrideCategoryResult.category === selectedCategory) {
    return overrideCategoryResult.checkResults;
  }

  const categories: { name: string; checks: ApiCheckResult[] }[] = [
    { name: 'General', checks: data.general.checkResults },
    ...data.difficulties.map((d) => ({
      name: d.category,
      checks: d.checkResults,
    })),
  ];
  const resolved = categories.find((c) => c.name === selectedCategory) ?? categories[0];
  return resolved?.checks ?? [];
}

/** Minor outcomes exist but are stripped by hiddenMinorCheckIds while show minors is on. */
export function hasMinorResultsHiddenByUserFilter(
  rawCheckResults: readonly ApiCheckResult[],
  options: { showMinor: boolean; hiddenMinorCheckIds: readonly number[] }
): boolean {
  if (
    !options.showMinor ||
    options.hiddenMinorCheckIds.length === 0 ||
    rawCheckResults.length === 0
  ) {
    return false;
  }
  const hidden = new Set(options.hiddenMinorCheckIds);
  return rawCheckResults.some((c) => c.level === 'Minor' && hidden.has(c.id));
}