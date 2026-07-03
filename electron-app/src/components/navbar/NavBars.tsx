import { AppShell, Box, Burger, Group, useMantineTheme } from '@mantine/core';
import { useLocation } from 'react-router-dom';
import { MainNavRail } from './MainNavRail';
import { getActiveNavRoute } from './navConfig';
import PageHintsButton from './PageHintsButton.tsx';
import Beatmaps from '../beatmaps/Beatmaps.tsx';
import SettingsButton from '../settings/SettingsButton';

interface NavBarsProps {
  desktopOpened: boolean;
  showBeatmapSidebar: boolean;
  toggleDesktop: () => void;
}

function NavBars(props: NavBarsProps) {
  const theme = useMantineTheme();
  const location = useLocation();
  const activeRoute = getActiveNavRoute(location.pathname);
  const sidebarToggleDisabled = !props.showBeatmapSidebar;

  return (
    <>
      <AppShell.Header
        style={{
          marginTop: 32,
          height: 60,
          fontFamily: theme.headings.fontFamily,
          background: theme.colors.dark[8],
          viewTransitionName: 'app-header',
        }}
      >
        <Group h={60} px="md" wrap="nowrap">
          <Box
            component="span"
            style={{ cursor: sidebarToggleDisabled ? 'not-allowed' : undefined }}
          >
            <Burger
              opened={props.showBeatmapSidebar && props.desktopOpened}
              onClick={props.showBeatmapSidebar ? props.toggleDesktop : undefined}
              disabled={sidebarToggleDisabled}
              size="sm"
              aria-label="Toggle beatmap sidebar"
              styles={{
                root: sidebarToggleDisabled
                  ? {
                      opacity: 0.35,
                      cursor: 'not-allowed',
                    }
                  : undefined,
              }}
            />
          </Box>
          <Group
            gap="xs"
            justify="space-between"
            align="center"
            wrap="nowrap"
            style={{ flex: 1, minWidth: 0 }}
          >
            <MainNavRail activeRoute={activeRoute} />
            <Group gap={4} ml="auto" wrap="nowrap">
              <PageHintsButton />
              <SettingsButton />
            </Group>
          </Group>
        </Group>
      </AppShell.Header>
      {props.showBeatmapSidebar && (
        <AppShell.Navbar style={{ viewTransitionName: 'app-sidebar' }}>
          <Beatmaps />
        </AppShell.Navbar>
      )}
    </>
  );
}

export default NavBars;
