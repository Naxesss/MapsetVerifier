import { NavLink } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

const SettingsButton: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const active = location.pathname.startsWith('/settings');

  return (
    <NavLink
      w={'unset'}
      label={<IconSettings />}
      active={active}
      onClick={() => navigate('/settings')}
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
