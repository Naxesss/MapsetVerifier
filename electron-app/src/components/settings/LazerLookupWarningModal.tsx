import { Alert, Button, Group, List, Modal, Stack } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import React from 'react';

interface LazerLookupWarningModalProps {
  opened: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

const LazerLookupWarningModal: React.FC<LazerLookupWarningModalProps> = ({
  opened,
  onConfirm,
  onCancel,
}) => {
  return (
    <Modal
      zIndex={400}
      opened={opened}
      onClose={onCancel}
      title="Enable experimental osu!(lazer) support?"
      centered
    >
      <Stack gap="md">
        <Alert
          icon={<IconAlertTriangle />}
          color="yellow"
          variant="light"
          title="Before you enable this"
        >
          <List spacing={4} size="sm">
            <List.Item>This feature currently works on Windows only.</List.Item>
            <List.Item>
              Because it is experimental, unexpected behavior or issues may occur.
            </List.Item>
          </List>
        </Alert>

        <Group justify="flex-end">
          <Button variant="subtle" onClick={onCancel}>
            Cancel
          </Button>
          <Button color="yellow" variant="light" onClick={onConfirm}>
            Enable anyway
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

export default LazerLookupWarningModal;
