import { ApiCheckResult, Level } from '../../../Types';
import { pickVisibleCheckResults } from '../checkResultVisibility';

const SEVERITY_ORDER: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
/** When merging per-category levels (e.g. mode tab), `Check` is all-clear — lowest severity. */
const AGGREGATE_SEVERITY_ORDER: Level[] = [...SEVERITY_ORDER, 'Check'];

export function getCategoryHighestLevel(
  checks: ApiCheckResult[],
  showMinor: boolean,
  hiddenMinorCheckIds: readonly number[]
): Level {
  if (!Array.isArray(checks) || checks.length === 0) return 'Check';
  const effective = pickVisibleCheckResults(checks, { showMinor, hiddenMinorCheckIds });
  if (effective.length === 0) return 'Check';
  const normalizedLevels = effective.map((c) =>
    c.level === 'Check' ? 'Info' : c.level
  ) as Level[];
  for (const lvl of SEVERITY_ORDER) {
    if (normalizedLevels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

export function getHighestLevel(levels: Level[]): Level {
  if (levels.length === 0) return 'Check';
  for (const lvl of AGGREGATE_SEVERITY_ORDER) {
    if (levels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Check';
}
