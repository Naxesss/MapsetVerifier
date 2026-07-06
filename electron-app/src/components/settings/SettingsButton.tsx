import { NavLink } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import React, { useLayoutEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

const SettingsButton: React.FC = () => {
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
    <NavLink
      onClick={handleClick}
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
