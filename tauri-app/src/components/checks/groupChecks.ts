import { ApiCheckResult } from '../../Types';

export interface GroupedChecks {
  id: number;
  items: ApiCheckResult[];
}

export function groupChecks(checks: ApiCheckResult[], showMinor: boolean): GroupedChecks[] {
  if (!Array.isArray(checks) || checks.length === 0) return [];
  const effective = showMinor ? checks : checks.filter((c) => c.level !== 'Minor');
  const map: Record<number, GroupedChecks> = {};
  for (const c of effective) {
    if (!map[c.id]) map[c.id] = { id: c.id, items: [] };
    map[c.id].items.push(c);
  }
  return Object.values(map);
}
