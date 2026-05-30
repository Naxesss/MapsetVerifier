import { AppShell, Burger, Group, useMantineTheme } from '@mantine/core';
import { useLocation } from 'react-router-dom';
import { MainNavRail } from './MainNavRail';
import { getActiveNavRoute } from './navConfig';
import PageHintsButton from './PageHintsButton.tsx';
import Beatmaps from '../beatmaps/Beatmaps.tsx';
import SettingsButton from '../settings/SettingsButton';

interface NavBarsProps {
  desktopOpened: boolean;
  toggleDesktop: () => void;
}

function NavBars(props: NavBarsProps) {
  const theme = useMantineTheme();
  const location = useLocation();
  const activeRoute = getActiveNavRoute(location.pathname);

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
          <Burger opened={props.desktopOpened} onClick={props.toggleDesktop} size="sm" />
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
      <AppShell.Navbar style={{ viewTransitionName: 'app-sidebar' }}>
        <Beatmaps />
      </AppShell.Navbar>
    </>
  );
}

export default NavBars;
