import { ActionIcon, useMantineTheme } from '@mantine/core';
import { IconMinus, IconSquare, IconX } from '@tabler/icons-react';

/** Minimize, maximize and close for the frameless window, as tall as the bar they sit in. */
export default function WindowControls() {
  const theme = useMantineTheme();
  const iconColor = theme.colors.primary[2];
  const buttonStyle = { height: '100%', width: 44, borderRadius: 0 };

  return (
    <div style={{ display: 'flex', alignSelf: 'stretch', flexShrink: 0 }}>
      <ActionIcon
        variant="subtle"
        color="gray"
        aria-label="Minimize"
        onClick={() => void window.electronAPI?.window.minimize()}
        style={buttonStyle}
      >
        <IconMinus size={20} color={iconColor} />
      </ActionIcon>
      <ActionIcon
        variant="subtle"
        color="gray"
        aria-label="Maximize"
        onClick={() => void window.electronAPI?.window.toggleMaximize()}
        style={buttonStyle}
      >
        <IconSquare size={14} color={iconColor} />
      </ActionIcon>
      <ActionIcon
        variant="subtle"
        color="red"
        aria-label="Close"
        onClick={() => void window.electronAPI?.window.close()}
        style={buttonStyle}
      >
        <IconX size={20} color={theme.colors.red[6]} />
      </ActionIcon>
    </div>
  );
}
