import { Button, Group, Modal, Stack, TextInput } from '@mantine/core';
import { useState } from 'react';

interface PinModalProps {
  opened: boolean;
  /** The current milestone name when renaming; empty for a new one. */
  initialName: string;
  onClose: () => void;
  onSave: (name: string) => void;
}

/** Names a snapshot as a milestone, e.g. "After mod round 2". */
export default function PinModal({ opened, initialName, onClose, onSave }: PinModalProps) {
  // Re-keyed by its parent per snapshot, so the field starts from that snapshot's name.
  const [name, setName] = useState(initialName);

  const save = () => {
    if (name.trim()) onSave(name.trim());
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Pin as milestone" centered size="sm">
      <Stack gap="md">
        <TextInput
          data-autofocus
          label="Name"
          description="Milestones are easy to compare against, e.g. after a round of mods."
          placeholder="After mod round 2"
          value={name}
          maxLength={60}
          onChange={(event) => setName(event.currentTarget.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') save();
          }}
        />
        <Group justify="flex-end" gap="sm">
          <Button variant="default" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={save} disabled={!name.trim()}>
            Pin
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
