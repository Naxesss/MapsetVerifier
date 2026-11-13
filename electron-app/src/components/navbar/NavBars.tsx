import { AppShell, Group, useMantineTheme } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { useLocation } from 'react-router-dom';
import { MainNavRail } from './MainNavRail';
import { getActiveNavRoute } from './navConfig';
import PageHintsButton from './PageHintsButton.tsx';
import { NAV_BAR_HEIGHT, Z_INDEX } from '../../theme/layers.ts';
import Beatmaps from '../beatmaps/Beatmaps.tsx';
import ReferencePanelButton from '../reference/ReferencePanelButton.tsx';
import SettingsButton from '../settings/SettingsButton';
import SettingsSidebar from '../settings/SettingsSidebar';
import AppBrand from '../window/AppBrand.tsx';
import { useRegisterNavBar } from '../window/topChrome.ts';
import WindowControls from '../window/WindowControls.tsx';

interface NavBarsProps {
  showBeatmapSidebar: boolean;
}

function NavBars(props: NavBarsProps) {
  const theme = useMantineTheme();
  const location = useLocation();
  const activeRoute = getActiveNavRoute(location.pathname);

  // The version always shows; the name and the page link labels give way in smaller windows.
  const showName = useMediaQuery('(min-width: 1150px)') ?? true;
  const showTabLabels = useMediaQuery('(min-width: 1050px)') ?? true;

  useRegisterNavBar();

  return (
    <>
      <AppShell.Header
        className="mv-drag-region"
        style={{
          height: NAV_BAR_HEIGHT,
          zIndex: Z_INDEX.windowBar,
          fontFamily: theme.headings.fontFamily,
          background: theme.colors.dark[8],
          userSelect: 'none',
        }}
      >
        <Group h={NAV_BAR_HEIGHT} pl="md" gap="xs" wrap="nowrap">
          {/* The app's name comes first, like most desktop apps. Space, not a divider, separates
              it from the pages. */}
          <AppBrand showName={showName} />
          <div aria-hidden style={{ width: 8, flexShrink: 0 }} />
          <MainNavRail activeRoute={activeRoute} showLabels={showTabLabels} />
          <Group gap="xs" ml="auto" wrap="nowrap">
            <PageHintsButton />
            <ReferencePanelButton />
            <SettingsButton />
          </Group>
          <WindowControls />
        </Group>
      </AppShell.Header>
      <AppShell.Navbar>
        {props.showBeatmapSidebar ? <Beatmaps /> : <SettingsSidebar />}
      </AppShell.Navbar>
    </>
  );
}

export default NavBars;
