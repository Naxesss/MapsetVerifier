import { Alert, Button, Group, List, Modal, Stack, Text } from '@mantine/core';
import { IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';
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

        <Alert icon={<IconInfoCircle />} color="blue" variant="light" title="How this works">
          <Text size="sm">
            Mapset Verifier reads your osu!(lazer) library directly, so your mapsets are always
            browsable in the sidebar — no need to keep the editor open. Opening the editor normally
            (not &quot;Edit externally&quot;) also lets MV highlight whichever map you currently
            have open, as a shortcut.
          </Text>
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
