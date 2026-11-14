import { AppShell } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { useLocation } from 'react-router-dom';
import { MainNavRail } from './MainNavRail';
import { getActiveNavRoute, SHOW_NAV_LABELS_QUERY } from './navConfig';
import PageHintsButton from './PageHintsButton.tsx';
import { Z_INDEX } from '../../theme/layers.ts';
import Beatmaps from '../beatmaps/Beatmaps.tsx';
import ReferencePanelButton from '../reference/ReferencePanelButton.tsx';
import SettingsButton from '../settings/SettingsButton';
import SettingsSidebar from '../settings/SettingsSidebar';
import { useRegisterNavBar } from '../window/navBarShown.ts';
import TopBar from '../window/TopBar.tsx';

interface NavBarsProps {
  showBeatmapSidebar: boolean;
}

function NavBars(props: NavBarsProps) {
  const location = useLocation();
  const activeRoute = getActiveNavRoute(location.pathname);

  // The page link labels give way in smaller windows.
  const showTabLabels = useMediaQuery(SHOW_NAV_LABELS_QUERY) ?? true;

  useRegisterNavBar();

  return (
    <>
      <AppShell.Header className="mv-top-bar" style={{ zIndex: Z_INDEX.topBar }}>
        <TopBar
          actions={
            <>
              <PageHintsButton />
              <ReferencePanelButton />
              <SettingsButton />
            </>
          }
        >
          {/* Space, not a divider, separates the app's name from the pages. */}
          <MainNavRail activeRoute={activeRoute} showLabels={showTabLabels} ml={12} />
        </TopBar>
      </AppShell.Header>
      <AppShell.Navbar>
        {props.showBeatmapSidebar ? <Beatmaps /> : <SettingsSidebar />}
      </AppShell.Navbar>
    </>
  );
}

export default NavBars;
