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
          top: '80%',
          transform: 'translate(-50%, -50%)',
          width: overlaySize,
          height: overlaySize,
          pointerEvents: 'none',
        }}
      >
        {/* Small black backdrop behind the exclamation cutout so it isn't transparent. */}
        {/* Yes this is hacky, yes I have no shame —Hivie */}
        <span
          style={{
            position: 'absolute',
            left: '50%',
            top: '50%',
            transform: 'translate(-50%, -50%)',
            width: Math.round(overlaySize * 0.72),
            height: Math.round(overlaySize * 0.72),
            borderRadius: '50%',
            backgroundColor: theme.black,
          }}
        />
        <IconExclamationCircleFilled
          color={theme.colors.orange[5]}
          size={overlaySize}
          style={{ position: 'absolute', inset: 0 }}
        />
      </span>
    </span>
  );
}
