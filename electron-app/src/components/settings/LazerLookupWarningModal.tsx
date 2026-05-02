import { Alert, Button, Group, List, Modal, Stack, Text } from '@mantine/core';
import { IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';
import React from 'react';

interface LazerLookupWarningModalProps {
  opened: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

const LazerLookupWarningModal: React.FC<LazerLookupWarningModalProps> = ({ opened, onConfirm, onCancel }) => {
  return (
    <Modal opened={opened} onClose={onCancel} title="Enable experimental osu!(lazer) support?" centered>
      <Stack gap="md">
        <Alert icon={<IconAlertTriangle />} color="yellow" variant="light" title="Before you enable this">
          <List spacing={4} size="sm">
            <List.Item>This feature currently works on Windows only.</List.Item>
            <List.Item>Because it is experimental, unexpected behavior or issues may occur.</List.Item>
          </List>
        </Alert>

        <Alert icon={<IconInfoCircle />} color="blue" variant="light" title="How this works">
          <Text size="sm">
            Mapset Verifier detects beatmaps that you open from the osu! editor via <strong>Files → Edit externally</strong>,
            then loads that beatmap directly in the sidebar.
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
