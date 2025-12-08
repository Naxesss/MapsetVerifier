import { Modal, Button, TextInput, Switch, Group, Stack, Alert, Divider, Text } from '@mantine/core';
import { open } from '@tauri-apps/plugin-dialog';
import React, { useState } from 'react';
import { useSettings } from '../../context/SettingsContext';
import MinorIcon from '../icons/MinorIcon';

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({ opened, onClose }) => {
  const { settings, setSettings } = useSettings();
  const [songFolder, setSongFolder] = useState(settings.songFolder ?? '');
  const [showMinor, setShowMinor] = useState(settings.showMinor);
  const [showGamemodeDifficultyNames, setShowGamemodeDifficultyNames] = useState(settings.showGamemodeDifficultyNames);
  const [showSnapshotDiffView, setShowSnapshotDiffView] = useState(settings.showSnapshotDiffView);
  const [gateInDev, setGateInDev] = useState(settings.gateInDev);

  // Keep local state in sync when modal is opened or settings change asynchronously
  React.useEffect(() => {
    if (opened) {
      setSongFolder(settings.songFolder ?? '');
      setShowMinor(settings.showMinor);
      setShowGamemodeDifficultyNames(settings.showGamemodeDifficultyNames);
      setShowSnapshotDiffView(settings.showSnapshotDiffView);
      setGateInDev(settings.gateInDev);
    }
  }, [opened, settings.songFolder, settings.showMinor, settings.gateInDev]);

  const handleSave = () => {
    setSettings((prev) => ({
      ...prev,
      songFolder,
      showMinor,
      showGamemodeDifficultyNames,
      showSnapshotDiffView,
      gateInDev
    }));
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
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      if (msg.includes('Plugin not found')) {
        alert(
          'Folder dialog plugin not initialized. Please rebuild with tauri-plugin-dialog registered.'
        );
      } else {
        alert('Folder picker failed: ' + msg);
      }
    }
  };

  const isDev = import.meta.env.DEV;

  return (
    <Modal opened={opened} onClose={onClose} title="Settings" yOffset="120px" size="lg">
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
          <Button variant="light" onClick={pickFolder}>
            Pick
          </Button>
        </Group>
        <Switch
          label={
            <Group gap="xs" align="center">
              <MinorIcon size={16} />
              Show minor issues
            </Group>
          }
          checked={showMinor}
          onChange={(e) => setShowMinor(e.currentTarget.checked)}
        />
        <Switch
          label="Show Gamemode difficulty names"
          checked={showGamemodeDifficultyNames}
          onChange={(e) => setShowGamemodeDifficultyNames(e.currentTarget.checked)}
        />
        <Switch
          label="Show additional info in snapshot comparison"
          checked={showSnapshotDiffView}
          onChange={(e) => setShowSnapshotDiffView(e.currentTarget.checked)}
        />
        {isDev && (
          <>
            <Divider my="sm" />
            <Stack gap="sm">
              <Text size="sm" c="dimmed">
                The following options affect development mode only and have no effect in production builds.
              </Text>
              <Switch
                label="Gate backend in DEV (start sidecar port 5005)"
                checked={gateInDev}
                onChange={(e) => setGateInDev(e.currentTarget.checked)}
              />
              <Alert title="Note" color="yellow" variant="light">
                <Group gap="sm">
                  <Text size="sm">Enabling this option will make the app mimic production mode by running the sidecar.</Text>
                  <Text size="sm">This does need the sidecar to be built beforehand and available in the following folder <code>/src-tauri/bin/server/dist/</code>.</Text>
                  <Text size="sm">Changing this settings may require restarting the application to take</Text>
                </Group>
              </Alert>
            </Stack>
          </>
        )}
        <Group justify="flex-end">
          <Button onClick={handleSave} variant="filled">
            Save
          </Button>
          <Button onClick={onClose} variant="light">
            Cancel
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

export default SettingsModal;
