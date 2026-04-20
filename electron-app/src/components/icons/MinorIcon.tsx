import { useMantineTheme } from '@mantine/core';
import { IconCircleCheckFilled, IconExclamationCircleFilled } from '@tabler/icons-react';

export default function MinorIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  // Overlay icon is half the main icon size
  const overlaySize = Math.round(size / 2);
  return (
    <span style={{ position: 'relative', display: 'inline-block', width: size, height: size }}>
      <IconCircleCheckFilled color={theme.colors.green[5]} size={size} />
      <span
        style={{
          position: 'absolute',
          left: '80%',
          top: '85%',
          transform: 'translate(-50%, -50%)',
          pointerEvents: 'none',
        }}
      >
        <IconExclamationCircleFilled color={theme.colors.orange[5]} size={overlaySize} />
      </span>
    </span>
  );
}
