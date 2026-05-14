import { Alert, Button, Group, Modal, Stack, Text } from '@mantine/core';
import { IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';
import React from 'react';

interface ObjectsTimelineAudioWarningModalProps {
  opened: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Confirms enabling experimental Objects overview timeline playback (audio + scrub UX). */
const ObjectsTimelineAudioWarningModal: React.FC<ObjectsTimelineAudioWarningModalProps> = ({
  opened,
  onConfirm,
  onCancel,
}) => {
  return (
    <Modal
      zIndex={400}
      opened={opened}
      onClose={onCancel}
      title="Enable timeline audio playback?"
      centered
    >
      <Stack gap="md">
        <Alert icon={<IconAlertTriangle />} color="yellow" variant="light" title="Before you enable this">
          <Text size="sm">
            Please note that unexpected behavior or performance issues may occur, and this might not work correctly on certain beatmaps.
          </Text>
        </Alert>

        <Alert icon={<IconInfoCircle />} color="blue" variant="light" title="How this works">
          <Text size="sm">
            This unlocks playback and timeline controls in the Objects Overview,
            including synchronized scrolling with map audio where available.
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

export default ObjectsTimelineAudioWarningModal;
