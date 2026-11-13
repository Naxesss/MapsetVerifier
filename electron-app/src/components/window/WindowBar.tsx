import { Group, useMantineTheme } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import React from 'react';
import AppBrand from './AppBrand';
import { useNavBarShown } from './topChrome';
import WindowControls from './WindowControls';
import { NAV_BAR_HEIGHT, Z_INDEX } from '../../theme/layers';
import { SHOW_APP_NAME_QUERY } from '../navbar/navConfig';

/**
 * The top bar for the screens before the app is ready (backend start, setup). It matches the nav
 * bar's height, colour and logo position, so when the nav bar takes over nothing jumps; it only
 * gains the page links.
 */
const WindowBar: React.FC = () => {
  const theme = useMantineTheme();
  const navBarShown = useNavBarShown();
  const showName = useMediaQuery(SHOW_APP_NAME_QUERY) ?? true;

  if (navBarShown) return null;

  return (
    <Group
      gap={0}
      className="mv-drag-region"
      wrap="nowrap"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        zIndex: Z_INDEX.windowBar,
        height: NAV_BAR_HEIGHT,
        background: theme.colors.dark[8],
        alignItems: 'center',
        userSelect: 'none',
        borderBottom: `1px solid ${theme.colors.dark[4]}`,
      }}
      pl="md"
    >
      <AppBrand showName={showName} />
      <div style={{ flex: 1 }} />
      <WindowControls />
    </Group>
  );
};

export default WindowBar;
