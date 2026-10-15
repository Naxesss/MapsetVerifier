import type {
  ApiRcSource,
  ApiRcStatement,
  DifficultyLevel,
  Mode,
  RcCoverage,
  RcKind,
} from '../../Types';

const OSU_WEB = 'https://osu.ppy.sh';

/** Wiki paths of the pages in the snapshot, used to keep links between them inside the app. */
const WIKI_PATH_TO_PAGE: Record<string, string> = {
  ranking_criteria: 'general',
  'ranking_criteria/osu!': 'osu',
  'ranking_criteria/osu!taiko': 'taiko',
  'ranking_criteria/osu!catch': 'catch',
  'ranking_criteria/osu!mania': 'mania',
  'ranking_criteria/metadata': 'metadata',
  'ranking_criteria/scaling_bpm': 'scaling-bpm',
  'ranking_criteria/difficulty_naming': 'difficulty-naming',
};

const PAGE_TO_WIKI_PATH: Record<string, string> = {
  general: 'Ranking_criteria',
  osu: 'Ranking_criteria/osu!',
  taiko: 'Ranking_criteria/osu!taiko',
  catch: 'Ranking_criteria/osu!catch',
  mania: 'Ranking_criteria/osu!mania',
  metadata: 'Ranking_criteria/Metadata',
  'scaling-bpm': 'Ranking_criteria/Scaling_BPM',
  'difficulty-naming': 'Ranking_criteria/Difficulty_naming',
};

export function rankingCriteriaRoute(page: string, ruleId?: string) {
  const base = `/ranking-criteria/${encodeURIComponent(page)}`;
  return ruleId ? `${base}?rule=${encodeURIComponent(ruleId)}` : base;
}

export type ResolvedWikiLink =
  | { kind: 'page'; page: string; anchor?: string }
  | { kind: 'anchor'; anchor: string }
  | { kind: 'external'; href: string };

/**
 * Resolves a link as written in the wiki markdown. Links to other snapshotted RC pages stay in the
 * app; everything else opens on the osu! website.
 */
export function resolveWikiLink(href: string, currentPage: string): ResolvedWikiLink {
  if (href.startsWith('#')) {
    return { kind: 'anchor', anchor: href.slice(1) };
  }

  if (/^[a-z]+:/i.test(href)) {
    return { kind: 'external', href };
  }

  const [pathPart, anchor] = href.split('#');
  let wikiPath: string;

  if (pathPart.startsWith('/wiki/')) {
    wikiPath = pathPart.slice('/wiki/'.length);
  } else if (pathPart.startsWith('/')) {
    return { kind: 'external', href: OSU_WEB + href };
  } else {
    // Relative links resolve against the current article, which is a folder in the osu-wiki.
    wikiPath = `${PAGE_TO_WIKI_PATH[currentPage] ?? 'Ranking_criteria'}/${pathPart}`;
  }

  const normalized = decodeURIComponent(wikiPath).replace(/\/+$/, '');
  const page = WIKI_PATH_TO_PAGE[normalized.toLowerCase()];

  if (page) {
    return { kind: 'page', page, anchor };
  }

  return {
    kind: 'external',
    href: `${OSU_WEB}/wiki/en/${normalized}${anchor ? '#' + anchor : ''}`,
  };
}

export function openExternal(href: string) {
  if (window.electronAPI?.shell.openExternal) {
    return window.electronAPI.shell.openExternal(href);
  }

  window.open(href, '_blank', 'noopener,noreferrer');
}

/** Heading anchor in the same shape osu-web uses: lower-cased words joined by dashes. */
export function headingAnchor(text: string) {
  return text
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, '-')
    .replace(/^-+|-+$/g, '');
}

const ICON_MODE: Record<string, Mode> = { o: 'Standard', t: 'Taiko', c: 'Catch', m: 'Mania' };

/** A star rating inside each difficulty's range, so the icon gets that difficulty's colour. */
const ICON_STAR_RATING: Record<string, number> = {
  easy: 1.5,
  normal: 2.3,
  hard: 3.4,
  insane: 4.6,
  expert: 5.9,
  extra: 7,
};

/** Parses a wiki difficulty icon such as `/wiki/shared/diff/normal-c.png?20211215`. */
export function parseDifficultyIcon(src: string | undefined) {
  const match = src?.match(/\/wiki\/shared\/diff\/([a-z]+)-([otcm])\.png/);
  if (!match) return null;

  return {
    mode: ICON_MODE[match[2]],
    starRating: ICON_STAR_RATING[match[1]] ?? 0,
  };
}

export const KIND_COLOR: Record<RcKind, string> = {
  Rule: 'red',
  Guideline: 'orange',
  Allowance: 'blue',
};

export function formatDifficulties(difficulties: DifficultyLevel[]) {
  return difficulties.filter((difficulty) => difficulty !== 'Ultra').join(', ');
}

const PAGE_MODE: Record<string, Mode> = {
  osu: 'Standard',
  taiko: 'Taiko',
  catch: 'Catch',
  mania: 'Mania',
};

/** The game mode a page's statements apply to, or null for pages which apply to every mode. */
export function pageMode(page: string): Mode | null {
  return PAGE_MODE[page] ?? null;
}

/** A star rating inside a difficulty level's range, for colouring its icon. */
export function difficultyStarRating(difficulty: DifficultyLevel) {
  return ICON_STAR_RATING[difficulty.toLowerCase()] ?? ICON_STAR_RATING.extra;
}

export type CoverageFilter = 'all' | 'covered' | 'partial' | 'outdated' | 'manual' | 'uncovered';

/** Outdated statements still have a check, it just needs reviewing against the new wording. */
export function isCovered(coverage: RcCoverage) {
  return coverage === 'Covered' || coverage === 'Partial' || coverage === 'Outdated';
}

/** Compares the wiki between a statement's last reviewed wording and the current snapshot on GitHub. */
export function wikiCompareUrl(source: ApiRcSource, fromCommit: string) {
  return `https://github.com/${source.repository}/compare/${fromCommit}...${source.commit}`;
}

/** Allowances and manual-only statements have nothing for a check to cover. */
export function isCoverable(coverage: RcCoverage) {
  return coverage !== 'Informational' && coverage !== 'Manual';
}

export function matchesCoverageFilter(coverage: RcCoverage, filter: CoverageFilter) {
  switch (filter) {
    case 'covered':
      return coverage === 'Covered';
    case 'partial':
      return coverage === 'Partial';
    case 'outdated':
      return coverage === 'Outdated';
    case 'manual':
      return coverage === 'Manual';
    case 'uncovered':
      return coverage === 'Uncovered';
    default:
      return true;
  }
}

/** Distinct check names linking to a statement, in link order. */
export function linkedCheckNames(statement: ApiRcStatement): string[] {
  return [...new Set(statement.links.map((link) => link.checkName))];
}

export function matchesSearch(statement: ApiRcStatement, query: string) {
  const q = query.trim().toLowerCase();
  if (!q) return true;

  return [
    statement.lead,
    statement.parentLead ?? '',
    statement.pageTitle,
    statement.id,
    ...statement.path,
    ...statement.links.map((link) => link.checkName),
  ].some((text) => text.toLowerCase().includes(q));
}
