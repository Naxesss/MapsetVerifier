import { formatGameModeLabel } from '../../utils/gameMode';
import type { ApiDocumentationCheck } from '../../Types';

/** First occurrence wins (stable order from general → Standard → Taiko → Catch → Mania merge). */
export function dedupeDocumentationChecksById(checks: ApiDocumentationCheck[]): ApiDocumentationCheck[] {
  const map = new Map<number, ApiDocumentationCheck>();
  for (const check of checks) {
    if (!map.has(check.id)) {
      map.set(check.id, check);
    }
  }
  return [...map.values()];
}

export function filterDocumentationChecks(
  checks: ApiDocumentationCheck[],
  query: string,
): ApiDocumentationCheck[] {
  const q = query.trim().toLowerCase();
  if (!q) return checks;

  return checks.filter((check) => {
    if (check.description.toLowerCase().includes(q)) return true;
    if (check.category.toLowerCase().includes(q)) return true;
    if (check.author.toLowerCase().includes(q)) return true;
    for (const mode of check.modes) {
      if (String(mode).toLowerCase().includes(q)) return true;
      if (formatGameModeLabel(mode).toLowerCase().includes(q)) return true;
    }
    return false;
  });
}
