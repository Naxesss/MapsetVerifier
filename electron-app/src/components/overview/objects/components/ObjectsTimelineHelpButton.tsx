import { ActionIcon, Button, Tooltip } from '@mantine/core';
import { IconHelpCircle } from '@tabler/icons-react';
import { useState } from 'react';
import ObjectsTimelineHelpModal from './ObjectsTimelineHelpModal.tsx';

type ObjectsTimelineHelpButtonProps = {
  showHitsoundSection?: boolean;
  size?: 'xs' | 'sm';
};

export default function ObjectsTimelineHelpButton({
  showHitsoundSection = true,
  size = 'sm',
}: ObjectsTimelineHelpButtonProps) {
  const [opened, setOpened] = useState(false);

  return (
    <>
      <Button
        variant="subtle"
        size={size}
        leftSection={<IconHelpCircle size={16} />}
        onClick={() => setOpened(true)}
      >
        How to use
      </Button>

      <ObjectsTimelineHelpModal
        opened={opened}
        onClose={() => setOpened(false)}
        showHitsoundSection={showHitsoundSection}
      />
    </>
  );
}
