import { Box, Menu, Text } from '@mantine/core';
import { IconCopy, IconPlayerPlay } from '@tabler/icons-react';

interface TimestampContextMenuProps {
  opened: boolean;
  anchorX: number | null;
  anchorY: number | null;
  timestampLabel?: string;
  onClose: () => void;
  onGoToTimestamp: () => void;
  onCopyTimestamp: () => void;
  goToLabel?: string;
  /** Render in a portal so the menu escapes overflow/stacking (default). Charts pass false. */
  withinPortal?: boolean;
}

export function TimestampContextMenu({
  opened,
  anchorX,
  anchorY,
  timestampLabel,
  onClose,
  onGoToTimestamp,
  onCopyTimestamp,
  goToLabel = 'Go to timestamp',
  withinPortal = true,
}: TimestampContextMenuProps) {
  return (
    <Menu
      opened={opened}
      onClose={onClose}
      withinPortal={withinPortal}
      closeOnItemClick
      closeOnClickOutside
      clickOutsideEvents={['mousedown', 'touchstart', 'keydown', 'pointerdown']}
      withArrow
      position="bottom-start"
    >
      <Menu.Target>
        <Box
          aria-hidden
          style={{
            position: 'absolute',
            left: anchorX ?? -9999,
            top: anchorY ?? -9999,
            width: 1,
            height: 1,
            pointerEvents: 'none',
          }}
        />
      </Menu.Target>
      <Menu.Dropdown>
        {timestampLabel ? (
          <Text size="xs" c="dimmed" px="sm" pt={6} pb={4} style={{ lineHeight: 1.35 }}>
            {timestampLabel}
          </Text>
        ) : null}
        <Menu.Item leftSection={<IconPlayerPlay size={14} />} onClick={onGoToTimestamp}>
          {goToLabel}
        </Menu.Item>
        <Menu.Item leftSection={<IconCopy size={14} />} onClick={onCopyTimestamp}>
          Copy timestamp
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
