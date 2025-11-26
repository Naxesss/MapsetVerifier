import {IconBook, IconCamera, IconCheck, IconHome, IconTimeline} from "@tabler/icons-react";
import {AppShell, Burger, Group, NavLink, useMantineTheme} from "@mantine/core";
import Beatmaps from "../beatmaps/Beatmaps.tsx";
import {Link, useLocation} from "react-router-dom";
import SettingsButton from "../settings/SettingsButton";
import WindowBar from "../window/WindowBar.tsx";

const navItems = [
  {
    to: '/',
    icon: IconHome,
    label: 'Home'
  },
  { 
    to: '/documentation',
    icon: IconBook,
    label: 'Documentation'
  },
  {
    to: '/checks',
    icon: IconCheck,
    label: 'Checks'
  },
  {
    to: '/snapshots',
    icon: IconCamera,
    label: 'Snapshots',
    disabled: true
  },
  {
    to: '/overview',
    icon: IconTimeline,
    label: 'Overview',
    disabled: true
  }
];

interface NavBarsProps {
  desktopOpened: boolean
  toggleDesktop: () => void
}

function NavBars(props: NavBarsProps) {
  const theme = useMantineTheme();
  const location = useLocation();
  const items = navItems.map((item) => (
    <NavLink
      w={"unset"}
      to={item.to}
      key={item.label}
      component={Link}
      active={(location.pathname.startsWith(item.to) && item.to !== '/') || (location.pathname === item.to && item.to === '/')}
      label={item.label}
      leftSection={<item.icon />}
      disabled={item.disabled}
      styles={{
        root: {
          borderRadius: 5,
        },
      }}
    />
  ));

  return (
    <>
      <AppShell.Header
        style={{
          fontFamily: theme.headings.fontFamily,
          background: theme.colors.dark[8]
        }}
      >
        <WindowBar />
        <Group h={60} px="md" wrap="nowrap">
          <Burger opened={props.desktopOpened} onClick={props.toggleDesktop} size="sm" />
          <Group gap="xs" style={{ flex: 1 }} wrap="nowrap">
            {items}
            <SettingsButton />
          </Group>
        </Group>
      </AppShell.Header>
      <AppShell.Navbar>
        <Beatmaps />
      </AppShell.Navbar>
    </>
  )
}

export default NavBars;