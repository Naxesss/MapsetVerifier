import { NavLink } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import React, { useState } from 'react';
import SettingsModal from './SettingsModal';

const SettingsButton: React.FC = () => {
  const [opened, setOpened] = useState(false);

  return (
    <>
      <NavLink
        ml={'auto'}
        w={'unset'}
        label={<IconSettings />}
        onClick={() => setOpened(true)}
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
      <SettingsModal opened={opened} onClose={() => setOpened(false)} />
    </>
  );
};

export default SettingsButton;
