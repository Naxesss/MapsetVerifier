import { Text } from '@mantine/core';
import { createHashRouter } from 'react-router-dom';
import App from './App.tsx';
import Checks from './components/checks/Checks.tsx';
import RequireBeatmapSelection from './components/common/RequireBeatmapSelection.tsx';
import RouterErrorDisplay from './components/common/RouterErrorDisplay.tsx';
import Documentation from './components/documentation/Documentation.tsx';
import Home from './components/home/Home.tsx';
import Overview from './components/overview/Overview.tsx';
import RankingCriteria from './components/rankingCriteria/RankingCriteria.tsx';
import Settings from './components/settings/Settings.tsx';
import Snapshots from './components/snapshots/Snapshots.tsx';

export const router = createHashRouter([
  {
    path: '/',
    element: <App />,
    errorElement: <RouterErrorDisplay />,
    children: [
      { index: true, element: <Home /> },
      { path: 'documentation', element: <Documentation /> },
      { path: 'ranking-criteria/:page?', element: <RankingCriteria /> },
      { path: 'settings/:section?', element: <Settings /> },
      {
        element: <RequireBeatmapSelection />,
        children: [
          { path: 'checks', element: <Checks /> },
          { path: 'snapshots', element: <Snapshots /> },
          { path: 'overview', element: <Overview /> },
        ],
      },
      { path: '*', element: <Text>404</Text> },
    ],
  },
]);
