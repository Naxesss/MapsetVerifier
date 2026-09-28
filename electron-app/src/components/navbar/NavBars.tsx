import { ActionIcon, AppShell, Group, Tooltip, useMantineTheme } from '@mantine/core';
import { IconLayoutSidebarLeftCollapse, IconLayoutSidebarLeftExpand } from '@tabler/icons-react';
import { useLocation } from 'react-router-dom';
import { MainNavRail } from './MainNavRail';
import { getActiveNavRoute, NAV_CONTROL_SIZE } from './navConfig';
import PageHintsButton from './PageHintsButton.tsx';
import Beatmaps from '../beatmaps/Beatmaps.tsx';
import SettingsButton from '../settings/SettingsButton';
import SettingsSidebar from '../settings/SettingsSidebar';

interface NavBarsProps {
  desktopOpened: boolean;
  showBeatmapSidebar: boolean;
  toggleDesktop?: () => void;
}

function NavBars(props: NavBarsProps) {
  const theme = useMantineTheme();
  const location = useLocation();
  const activeRoute = getActiveNavRoute(location.pathname);
  const sidebarToggleLabel = props.desktopOpened ? 'Hide mapset list' : 'Show mapset list';
  const sidebarToggleDisabled = !props.toggleDesktop;

  return (
    <>
      <AppShell.Header
        style={{
          marginTop: 32,
          height: 60,
          fontFamily: theme.headings.fontFamily,
          background: theme.colors.dark[8],
        }}
      >
        <Group h={60} px="md" wrap="nowrap">
          {/* Tooltips open below: above the navbar is the window's title bar. */}
          <Tooltip label={sidebarToggleLabel} position="bottom" disabled={sidebarToggleDisabled}>
            <ActionIcon
              variant="subtle"
              color="gray"
              size={NAV_CONTROL_SIZE}
              onClick={props.toggleDesktop}
              disabled={sidebarToggleDisabled}
              aria-label={sidebarToggleLabel}
              styles={
                sidebarToggleDisabled
                  ? { root: { backgroundColor: 'transparent', opacity: 0.35 } }
                  : undefined
              }
            >
              {props.desktopOpened ? (
                <IconLayoutSidebarLeftCollapse color="var(--mantine-color-white)" />
              ) : (
                <IconLayoutSidebarLeftExpand color="var(--mantine-color-white)" />
              )}
            </ActionIcon>
          </Tooltip>
          <Group
            gap="xs"
            justify="space-between"
            align="center"
            wrap="nowrap"
            style={{ flex: 1, minWidth: 0 }}
          >
            <MainNavRail activeRoute={activeRoute} />
            <Group gap="xs" ml="auto" wrap="nowrap">
              <PageHintsButton />
              <SettingsButton />
            </Group>
          </Group>
        </Group>
      </AppShell.Header>
      <AppShell.Navbar>
        {props.showBeatmapSidebar ? <Beatmaps /> : <SettingsSidebar />}
      </AppShell.Navbar>
    </>
  );
}

export default NavBars;
