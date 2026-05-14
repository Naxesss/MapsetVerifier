import { Text } from '@mantine/core';
import { createHashRouter } from 'react-router-dom';
import App from './App.tsx';
import Checks from './components/checks/Checks.tsx';
import RequireBeatmapSelection from './components/common/RequireBeatmapSelection.tsx';
import Documentation from './components/documentation/Documentation.tsx';
import Home from './components/home/Home.tsx';
import Overview from './components/overview/Overview.tsx';
import Snapshots from './components/snapshots/Snapshots.tsx';

export const router = createHashRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <Home /> },
      { path: 'documentation', element: <Documentation /> },
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
