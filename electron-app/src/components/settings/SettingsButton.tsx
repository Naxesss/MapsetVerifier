import { NavLink } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import React from 'react';
import { Link, useLocation } from 'react-router-dom';

const SettingsButton: React.FC = () => {
  const location = useLocation();
  const active = location.pathname.startsWith('/settings');

  return (
    <NavLink
      component={Link}
      to="/settings"
      viewTransition
      w={'unset'}
      label={<IconSettings />}
      active={active}
      styles={{
        root: {
          borderRadius: '100%',
          justifyContent: 'center',
          paddingLeft: 8,
          paddingRight: 8,
        },
        label: { width: '100%', display: 'flex', justifyContent: 'center' },
      }}
    />
  );
};

export default SettingsButton;
