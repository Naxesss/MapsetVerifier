import { createContext, useContext } from 'react';
import { ApiDocumentationCheck, ApiRcStatement } from '../../Types';

/** What the detail modal shows: a ranking criteria statement or the documentation of a check. */
export type DetailView =
  | { kind: 'rule'; statement: ApiRcStatement }
  | { kind: 'check'; check: ApiDocumentationCheck };

export function detailKey(view: DetailView) {
  return view.kind === 'rule' ? `rule:${view.statement.id}` : `check:${view.check.id}`;
}

export function detailTitle(view: DetailView) {
  return view.kind === 'rule' ? view.statement.lead.replace(/`/g, '') : view.check.description;
}

interface DetailNavigation {
  /** Shows a view in place of the current one, or goes back to it when it was shown before. */
  open: (view: DetailView) => void;
  /** The view shown before the current one, which "Back" returns to. */
  previous: DetailView | null;
}

export const DetailNavigationContext = createContext<DetailNavigation | null>(null);

/** Navigation of the surrounding detail modal, or null outside of one. */
export function useDetailNavigation() {
  return useContext(DetailNavigationContext);
}
