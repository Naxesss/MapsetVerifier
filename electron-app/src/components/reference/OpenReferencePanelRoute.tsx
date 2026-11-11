import { useEffect } from 'react';
import { Navigate, useParams } from 'react-router-dom';
import { DEFAULT_RC_PAGE, useReferencePanel } from '../../context/ReferencePanelContext.tsx';

/**
 * The old Documentation and Ranking criteria page addresses: they open the documentation panel on
 * the matching list instead, over the home page.
 */
export default function OpenReferencePanelRoute({ tab }: { tab: 'rules' | 'checks' }) {
  const { page } = useParams();
  const { showRcPage, setTab } = useReferencePanel();

  useEffect(() => {
    showRcPage(tab === 'rules' ? (page ?? DEFAULT_RC_PAGE) : DEFAULT_RC_PAGE);
    if (tab === 'checks') setTab('checks');
  }, [tab, page, showRcPage, setTab]);

  return <Navigate to="/" replace />;
}
