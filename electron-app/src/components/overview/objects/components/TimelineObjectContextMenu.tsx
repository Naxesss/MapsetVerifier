import { Box, Menu } from '@mantine/core';
import { IconCopy, IconPlayerPlay } from '@tabler/icons-react';

interface TimelineObjectContextMenuProps {
  opened: boolean;
  anchorX: number | null;
  anchorY: number | null;
  onClose: () => void;
  onGoToObject: () => void;
  onCopyTimestamp: () => void;
}

export default function TimelineObjectContextMenu({
  opened,
  anchorX,
  anchorY,
  onClose,
  onGoToObject,
  onCopyTimestamp,
}: TimelineObjectContextMenuProps) {
  return (
    <Menu
      opened={opened}
      onClose={onClose}
      withinPortal
      closeOnItemClick
      closeOnClickOutside
      withArrow
    >
      <Menu.Target>
        <Box
          style={{
            position: 'absolute',
            left: anchorX ?? -9999,
            top: anchorY ?? -9999,
            width: 1,
            height: 1,
          }}
        />
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Item leftSection={<IconPlayerPlay size={14} />} onClick={onGoToObject}>
          Go to object
        </Menu.Item>
        <Menu.Item leftSection={<IconCopy size={14} />} onClick={onCopyTimestamp}>
          Copy timestamp
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
