import { ActionIcon, Tooltip, useMantineTheme } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import React, { useLayoutEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { NAV_CONTROL_SIZE } from '../navbar/navConfig.ts';

const SettingsButton: React.FC = () => {
  const theme = useMantineTheme();
  const location = useLocation();
  const navigate = useNavigate();
  const active = location.pathname.startsWith('/settings');
  const previousPathRef = useRef<string>('/');

  useLayoutEffect(() => {
    if (!active) {
      previousPathRef.current = location.pathname + location.search;
    }
  }, [active, location.pathname, location.search]);

  const handleClick = () => {
    if (active) {
      navigate(previousPathRef.current);
    } else {
      navigate('/settings');
    }
  };

  return (
    <Tooltip label={active ? 'Close settings' : 'Settings'} position="bottom">
      <ActionIcon
        onClick={handleClick}
        variant={active ? 'filled' : 'subtle'}
        color={active ? 'primary.2' : 'gray'}
        size={NAV_CONTROL_SIZE}
        aria-label={active ? 'Close settings' : 'Settings'}
        aria-pressed={active}
      >
        <IconSettings color={active ? theme.black : 'var(--mantine-color-white)'} />
      </ActionIcon>
    </Tooltip>
  );
};

export default SettingsButton;
