import { ApiCheckResult, Level } from '../../../Types';

const SEVERITY_ORDER: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];

export function getCategoryHighestLevel(checks: ApiCheckResult[], showMinor: boolean): Level {
  if (!Array.isArray(checks) || checks.length === 0) return 'Info';
  const effective = showMinor ? checks : checks.filter((c) => c.level !== 'Minor');
  const normalizedLevels = effective.map((c) => (c.level === 'Check' ? 'Info' : c.level)) as Level[];
  for (const lvl of SEVERITY_ORDER) {
    if (normalizedLevels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

export function getHighestLevel(levels: Level[]): Level {
  for (const lvl of SEVERITY_ORDER) {
    if (levels.includes(lvl)) {
      return lvl;
    }
  }
  return 'Info';
}

