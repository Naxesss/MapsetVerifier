import React, { useState } from "react";
import { Modal, Button, TextInput, Switch, Group, Stack } from "@mantine/core";
import { useSettings } from "../../context/SettingsContext";
import { open } from '@tauri-apps/plugin-dialog';
import MinorIcon from "../icons/MinorIcon";

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({ opened, onClose }) => {
  const { settings, setSettings } = useSettings();
  const [songFolder, setSongFolder] = useState(settings.songFolder ?? "");
  const [showMinor, setShowMinor] = useState(settings.showMinor);

  // Keep local state in sync when modal is opened or settings change asynchronously
  React.useEffect(() => {
    if (opened) {
      setSongFolder(settings.songFolder ?? "");
      setShowMinor(settings.showMinor);
    }
  }, [opened, settings.songFolder, settings.showMinor]);

  const handleSave = () => {
    setSettings(prev => ({ ...prev, songFolder, showMinor }));
    onClose();
  };

  const pickFolder = async () => {
    try {
      const result = await open({ directory: true });
      if (typeof result === 'string') {
        setSongFolder(result);
      }
    } catch (e: any) {
      console.error('[SettingsModal] Folder pick failed:', e);
      const msg = typeof e === 'string' ? e : (e?.message || 'Unknown error');
      if (msg.includes('Plugin not found')) {
        alert('Folder dialog plugin not initialized. Please rebuild with tauri-plugin-dialog registered.');
      } else {
        alert('Folder picker failed: ' + msg);
      }
    }
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Settings" centered>
      <Stack gap="md">
        <Group align="flex-end" gap="sm">
          <TextInput
            label="osu! Songs Folder"
            style={{ flexGrow: 1 }}
            value={songFolder}
            readOnly
            description="Use the Pick button to select a folder."
            onClick={() => songFolder === '' && pickFolder()}
          />
          <Button variant="light" onClick={pickFolder}>Pick</Button>
        </Group>
        <Switch
          label={
          <Group gap="xs" align="center">
            <MinorIcon /> 
            Show minor issues
          </Group>
        }
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
