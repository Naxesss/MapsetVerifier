import React, { useState } from "react";
import { Modal, Button, TextInput, Switch, Group, Stack } from "@mantine/core";
import { useSettings } from "../../context/SettingsContext";

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({ opened, onClose }) => {
  const { settings, setSettings } = useSettings();
  const [songFolder, setSongFolder] = useState(settings.songFolder ?? "");
  const [showMinor, setShowMinor] = useState(settings.showMinor);

  const handleSave = () => {
    setSettings(prev => ({ ...prev, songFolder, showMinor }));
    onClose();
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Settings" centered>
      <Stack gap="md">
        <TextInput
          label="osu! Songs Folder"
          value={songFolder}
          onChange={e => setSongFolder(e.currentTarget.value)}
          placeholder="Path to osu! songs folder"
        />
        <Switch
          label="Show minor issues"
          checked={showMinor}
          onChange={e => setShowMinor(e.currentTarget.checked)}
        />
        <Group justify="flex-end">
          <Button onClick={handleSave} variant="filled">Save</Button>
          <Button onClick={onClose} variant="light">Cancel</Button>
        </Group>
      </Stack>
    </Modal>
  );
};

export default SettingsModal;
