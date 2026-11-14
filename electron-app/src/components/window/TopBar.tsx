import { Group } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import AppBrand from './AppBrand';
import WindowControls from './WindowControls';
import { SHOW_APP_NAME_QUERY } from '../navbar/navConfig';
import type { ReactNode } from 'react';

interface TopBarProps {
  /** Between the brand and the right side, such as the page links. */
  children?: ReactNode;
  /** Buttons on the right, just before the window controls. */
  actions?: ReactNode;
}

/**
 * The inside of the bar at the top of the window, shared by the window bar (startup and setup) and
 * the nav bar, so both put the logo and the window controls in the same spot. Goes inside an element
 * with the `mv-top-bar` class, which gives the bar its look.
 */
export default function TopBar({ children, actions }: TopBarProps) {
  // The version always shows; the name gives way in smaller windows.
  const showName = useMediaQuery(SHOW_APP_NAME_QUERY) ?? true;

  return (
    <Group h="100%" pl="md" gap="xs" wrap="nowrap">
      <AppBrand showName={showName} />
      {children}
      <Group h="100%" gap="xs" ml="auto" wrap="nowrap">
        {actions}
        <WindowControls />
      </Group>
    </Group>
  );
}
