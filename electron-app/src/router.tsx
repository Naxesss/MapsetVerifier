import { createHashRouter } from 'react-router-dom';
import App from './App.tsx';
import Checks from './components/checks/Checks.tsx';
import NotFoundDisplay from './components/common/NotFoundDisplay.tsx';
import RequireBeatmapSelection from './components/common/RequireBeatmapSelection.tsx';
import RouterErrorDisplay from './components/common/RouterErrorDisplay.tsx';
import Home from './components/home/Home.tsx';
import Overview from './components/overview/Overview.tsx';
import OpenReferencePanelRoute from './components/reference/OpenReferencePanelRoute.tsx';
import Settings from './components/settings/Settings.tsx';
import Snapshots from './components/snapshots/Snapshots.tsx';

export const router = createHashRouter([
  {
    path: '/',
    element: <App />,
    errorElement: <RouterErrorDisplay />,
    children: [
      { index: true, element: <Home /> },
      { path: 'documentation', element: <OpenReferencePanelRoute tab="checks" /> },
      // Dev only: writes to the catalogue in the source checkout. Left out of production bundles.
      ...(import.meta.env.DEV
        ? [
            {
              path: 'ranking-criteria/review',
              lazy: async () => ({
                Component: (await import('./components/rankingCriteria/RcCurationReview.tsx'))
                  .default,
              }),
            },
          ]
        : []),
      { path: 'ranking-criteria/:page?', element: <OpenReferencePanelRoute tab="rules" /> },
      { path: 'settings/:section?', element: <Settings /> },
      {
        element: <RequireBeatmapSelection />,
        children: [
          { path: 'checks', element: <Checks /> },
          { path: 'snapshots', element: <Snapshots /> },
          { path: 'overview', element: <Overview /> },
        ],
      },
      { path: '*', element: <NotFoundDisplay /> },
    ],
  },
]);
