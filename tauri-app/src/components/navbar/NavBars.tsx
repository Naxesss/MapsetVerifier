import {IconBook, IconCamera, IconCheck, IconGauge, IconHome, IconSettings, IconTimeline} from "@tabler/icons-react";
import {useState} from "react";
import {AppShell, Burger, Group, NavLink} from "@mantine/core";
import Beatmaps from "../beatmaps/Beatmaps.tsx";
import {Link, useLocation} from "react-router-dom";

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
    label: 'Snapshots'
  },
  {
    to: '/overview',
    icon: IconTimeline,
    label: 'Overview'
  }
];

interface NavBarsProps {
  desktopOpened: boolean
  toggleDesktop: () => void
}

function NavBars(props: NavBarsProps) {
  const location = useLocation();
  const items = navItems.map((item) => (
    <NavLink
      w={"unset"}
      href="#required-for-focus"
      to={item.to}
      key={item.label}
      component={Link}
      active={location.pathname === item.to}
      label={item.label}
      leftSection={<item.icon />}
      styles={{
        root: {
          borderRadius: 5,
        },
      }}
    />
  ));

  return (
    <>
      <AppShell.Header>
        <Group h="100%" px="md" wrap="nowrap">
          <Burger opened={props.desktopOpened} onClick={props.toggleDesktop} size="sm" />
          <Group gap="xs" style={{ flex: 1 }} wrap="nowrap">
            {items}
            <NavLink
              ml={"auto"}
              w={"unset"}
              href="#required-for-focus"
              label={<IconSettings />}
              styles={{
                root: {
                  borderRadius: "100%",
                  
                  // To center the icon properly
                  justifyContent: "center",
                  paddingLeft: 8,
                  paddingRight: 8
                },
                label: { width: "100%", display: "flex", justifyContent: "center" }
              }}
            />
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