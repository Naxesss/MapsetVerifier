import { ApiCheckResult, Level } from '../../../Types';
import { pickVisibleCheckResults } from '../checkResultVisibility';

/** A level as shown on an issue: `Check` (a passing result) is shown as Info. */
export type DisplayLevel = Exclude<Level, 'Check'>;

/** Issue levels, most severe first. */
export const SEVERITY_ORDER: DisplayLevel[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
/** When merging per-category levels (e.g. mode tab), `Check` is all-clear — lowest severity. */
const AGGREGATE_SEVERITY_ORDER: Level[] = [...SEVERITY_ORDER, 'Check'];

export function normalizeLevel(level: Level): DisplayLevel {
  return level === 'Check' ? 'Info' : level;
}

/** The most severe of the given levels, or `Check` when there are none. */
export function getHighestLevel(levels: Level[]): Level {
  return AGGREGATE_SEVERITY_ORDER.find((level) => levels.includes(level)) ?? 'Check';
}

/** The most severe level of a group of issues, as shown on the group. */
export function getHighestIssueLevel(levels: Level[]): DisplayLevel {
  return normalizeLevel(getHighestLevel(levels));
}

export function getCategoryHighestLevel(
  checks: ApiCheckResult[],
  showMinor: boolean,
  hiddenMinorCheckIds: readonly number[]
): Level {
  if (!Array.isArray(checks) || checks.length === 0) return 'Check';
  const effective = pickVisibleCheckResults(checks, { showMinor, hiddenMinorCheckIds });
  if (effective.length === 0) return 'Check';
  return getHighestIssueLevel(effective.map((check) => check.level));
}
