import { Alert, Button, Group, Modal, Stack } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import React from 'react';

interface AdvancedAudioWarningModalProps {
  opened: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

const AdvancedAudioWarningModal: React.FC<AdvancedAudioWarningModalProps> = ({
  opened,
  onConfirm,
  onCancel,
}) => {
  return (
    <Modal opened={opened} onClose={onCancel} title="Enable advanced audio analysis?" centered>
      <Stack gap="md">
        <Alert icon={<IconAlertTriangle />} color="yellow" variant="light" title="Warning">
          This displays additional and highly-technical audio analysis information which is not necessary for most modding purposes.
          Are you sure you want to enable this?
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

export default AdvancedAudioWarningModal;
