import { ActionIcon, Tooltip, useMantineTheme } from '@mantine/core';
import { IconBook } from '@tabler/icons-react';
import { useReferencePanel } from '../../context/ReferencePanelContext.tsx';
import { NAV_CONTROL_SIZE, NAV_ICON_SIZE } from '../navbar/navConfig.ts';

/** Opens and closes the documentation panel, highlighted like the settings button while open. */
export default function ReferencePanelButton() {
  const theme = useMantineTheme();
  const { opened, toggle } = useReferencePanel();
  const label = opened ? 'Close documentation' : 'Documentation';

  return (
    <Tooltip label={label} position="bottom">
      <ActionIcon
        onClick={toggle}
        variant={opened ? 'filled' : 'subtle'}
        color={opened ? 'primary.2' : 'gray'}
        size={NAV_CONTROL_SIZE}
        aria-label={label}
        aria-pressed={opened}
      >
        <IconBook
          size={NAV_ICON_SIZE}
          color={opened ? theme.black : 'var(--mantine-color-white)'}
        />
      </ActionIcon>
    </Tooltip>
  );
}
