import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { detailKey, type DetailView } from '../components/details/detailNavigation';
import type { ApiDocumentationCheck, ApiRcStatement } from '../Types';

/** Which list the reference panel shows when no rule or check is open. */
export type ReferenceTab = 'rules' | 'checks';

export const DEFAULT_RC_PAGE = 'general';

interface ReferencePanelContextType {
  opened: boolean;
  tab: ReferenceTab;
  /** The ranking criteria page the rules list shows. */
  rcPage: string;
  /** The rules and checks opened in turn, the last one shown; empty while a list is shown. */
  history: DetailView[];
  toggle: () => void;
  close: () => void;
  setTab: (tab: ReferenceTab) => void;
  /** Opens the panel on a ranking criteria page. */
  showRcPage: (page: string) => void;
  /**
   * Shows a rule or check. From outside the panel it opens on just that; inside it, it replaces what
   * is shown, or goes back to it when it was shown before.
   */
  openDetail: (view: DetailView) => void;
  openRule: (statement: ApiRcStatement) => void;
  openCheck: (check: ApiDocumentationCheck) => void;
  /** Goes back one step, to the previous rule or check or to the list. */
  back: () => void;
}

const ReferencePanelContext = createContext<ReferencePanelContextType | null>(null);

/**
 * The documentation panel: ranking criteria and check documentation, slid over whatever page is open
 * so looking something up doesn't mean leaving it.
 */
export function ReferencePanelProvider({ children }: { children: ReactNode }) {
  const [opened, setOpened] = useState(false);
  const [tab, setTab] = useState<ReferenceTab>('rules');
  const [rcPage, setRcPage] = useState(DEFAULT_RC_PAGE);
  const [history, setHistory] = useState<DetailView[]>([]);

  const toggle = useCallback(() => setOpened((value) => !value), []);
  const close = useCallback(() => setOpened(false), []);

  const showRcPage = useCallback((page: string) => {
    setTab('rules');
    setRcPage(page);
    setHistory([]);
    setOpened(true);
  }, []);

  const openDetail = useCallback(
    (view: DetailView) => {
      if (!opened) {
        // Opened from elsewhere: going back lands on the list the rule or check is in.
        if (view.kind === 'rule') {
          setTab('rules');
          setRcPage(view.statement.page);
        } else {
          setTab('checks');
        }
        setHistory([view]);
        setOpened(true);
        return;
      }

      setHistory((shown) => {
        const index = shown.findIndex((item) => detailKey(item) === detailKey(view));
        return index >= 0 ? shown.slice(0, index + 1) : [...shown, view];
      });
    },
    [opened]
  );

  const openRule = useCallback(
    (statement: ApiRcStatement) => openDetail({ kind: 'rule', statement }),
    [openDetail]
  );
  const openCheck = useCallback(
    (check: ApiDocumentationCheck) => openDetail({ kind: 'check', check }),
    [openDetail]
  );
  const back = useCallback(() => setHistory((shown) => shown.slice(0, -1)), []);

  const value = useMemo(
    () => ({
      opened,
      tab,
      rcPage,
      history,
      toggle,
      close,
      setTab,
      showRcPage,
      openDetail,
      openRule,
      openCheck,
      back,
    }),
    [opened, tab, rcPage, history, toggle, close, showRcPage, openDetail, openRule, openCheck, back]
  );

  return <ReferencePanelContext.Provider value={value}>{children}</ReferencePanelContext.Provider>;
}

export function useReferencePanel() {
  const context = useContext(ReferencePanelContext);
  if (!context) throw new Error('useReferencePanel must be used within a ReferencePanelProvider');
  return context;
}
