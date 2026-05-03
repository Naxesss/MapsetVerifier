import {
  Modal,
  Button,
  TextInput,
  Switch,
  Group,
  Stack,
  Alert,
  Divider,
  Text,
} from '@mantine/core';
import { IconAlertTriangle, IconFolder, IconNote, IconRefresh } from '@tabler/icons-react';
import React, { useEffect, useState } from 'react';
import LazerLookupWarningModal from './LazerLookupWarningModal';
import { useSettings } from '../../context/SettingsContext';
import { useUpdater } from '../../context/UpdaterContext';
import MinorIcon from '../icons/MinorIcon';

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

const SettingsModal: React.FC<SettingsModalProps> = ({ opened, onClose }) => {
  const { settings, setSettings } = useSettings();
  const { openUpdater } = useUpdater();
  const [songFolder, setSongFolder] = useState(settings.songFolder ?? '');
  const [showMinor, setShowMinor] = useState(settings.showMinor);
  const [showGamemodeDifficultyNames, setShowGamemodeDifficultyNames] = useState(
    settings.showGamemodeDifficultyNames
  );
  const [showSnapshotDiffView, setShowSnapshotDiffView] = useState(settings.showSnapshotDiffView);
  const [showAdvancedAudioAnalysis, setShowAdvancedAudioAnalysis] = useState(
    settings.showAdvancedAudioAnalysis
  );
  const [lazerLookupEnabled, setLazerLookupEnabled] = useState(settings.lazerLookupEnabled);
  const [gateInDev, setGateInDev] = useState(settings.gateInDev);
  const [lazerWarningOpened, setLazerWarningOpened] = useState(false);

  // Keep local state in sync when modal is opened or settings change asynchronously
  useEffect(() => {
    if (opened) {
      setSongFolder(settings.songFolder ?? '');
      setShowMinor(settings.showMinor);
      setShowGamemodeDifficultyNames(settings.showGamemodeDifficultyNames);
      setShowSnapshotDiffView(settings.showSnapshotDiffView);
      setShowAdvancedAudioAnalysis(settings.showAdvancedAudioAnalysis);
      setLazerLookupEnabled(settings.lazerLookupEnabled);
      setGateInDev(settings.gateInDev);
    }
  }, [
    opened,
    settings.songFolder,
    settings.showMinor,
    settings.showGamemodeDifficultyNames,
    settings.showSnapshotDiffView,
    settings.showAdvancedAudioAnalysis,
    settings.lazerLookupEnabled,
    settings.gateInDev,
  ]);

  const pickFolder = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSongFolder(result);
        setSettings((prev) => ({ ...prev, songFolder: result }));
      }
    } catch (e: any) {
      console.error('[SettingsModal] Folder pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Folder picker failed: ' + msg);
    }
  };

  const isDev = import.meta.env.DEV;
  const [currentVersion, setCurrentVersion] = useState<string>('unknown');
  useEffect(() => {
    window.electronAPI
      ?.getVersion()
      .then(setCurrentVersion)
      .catch(() => setCurrentVersion('unknown'));
  }, []);

  return (
    <>
      <Modal opened={opened} onClose={onClose} title="Settings" yOffset="120px" size="lg">
        <Stack gap="md">
          <Group align="flex-end" gap="sm">
            <TextInput
              label="osu! Songs Folder"
              style={{ flexGrow: 1 }}
              value={songFolder}
              readOnly
              onClick={() => songFolder === '' && pickFolder()}
            />
            <Button leftSection={<IconFolder size={18} />} variant="light" onClick={pickFolder}>
              Browse
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
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowMinor(checked);
              setSettings((prev) => ({ ...prev, showMinor: checked }));
            }}
          />
          <Switch
            label="Use difficulty names from corresponding game modes"
            checked={showGamemodeDifficultyNames}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowGamemodeDifficultyNames(checked);
              setSettings((prev) => ({ ...prev, showGamemodeDifficultyNames: checked }));
            }}
          />
          <Switch
            label="Show additional info in snapshot comparison"
            checked={showSnapshotDiffView}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowSnapshotDiffView(checked);
              setSettings((prev) => ({ ...prev, showSnapshotDiffView: checked }));
            }}
          />
          <Switch
            label="Show advanced audio analysis"
            checked={showAdvancedAudioAnalysis}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setShowAdvancedAudioAnalysis(checked);
              setSettings((prev) => ({ ...prev, showAdvancedAudioAnalysis: checked }));
            }}
          />
          <Switch
            label={
              <Group gap="xs" align="center">
                <IconAlertTriangle size={16} color="var(--mantine-color-yellow-5)" />
                <Group gap="sm">
                  <Text size="xs" c="yellow">
                    experimental
                  </Text>
                  <Text size="sm">osu!(lazer) support</Text>
                </Group>
              </Group>
            }
            checked={lazerLookupEnabled}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              if (!checked) {
                setLazerLookupEnabled(false);
                setSettings((prev) => ({ ...prev, lazerLookupEnabled: false }));
                return;
              }
              if (!lazerLookupEnabled) {
                setLazerWarningOpened(true);
              }
            }}
          />
          <Divider my="xs" />
          <Group justify="space-between" align="end">
            <div>
              <Text fw={500}>Application updates</Text>
              <Text size="sm" c="dimmed">
                Current version: {currentVersion}
              </Text>
            </div>
            <Button
              leftSection={<IconRefresh size={18} />}
              variant="light"
              onClick={() => void openUpdater()}
            >
              Check for updates
            </Button>
          </Group>
          {isDev && (
            <>
              <Divider my="sm" />
              <Stack gap="sm">
                <Text size="sm" c="dimmed">
                  The following options affect development mode only and have no effect in
                  production builds.
                </Text>
                <Switch
                  label="Gate backend in DEV (start sidecar port 5005)"
                  checked={gateInDev}
                  onChange={(e) => {
                    const checked = e.currentTarget.checked;
                    setGateInDev(checked);
                    setSettings((prev) => ({ ...prev, gateInDev: checked }));
                  }}
                />
                <Alert icon={<IconNote />} title="Note" color="yellow" variant="light">
                  <Group gap="sm">
                    <Text size="sm">
                      Enabling this option will make the app mimic production mode by running the
                      sidecar.
                    </Text>
                    <Text size="sm">
                      This does need the sidecar to be built beforehand and available in the
                      following folder <code>/bin/server/dist/&lt;rid&gt;/</code>.
                    </Text>
                    <Text size="sm">
                      Changing this settings may require restarting the application to take
                    </Text>
                  </Group>
                </Alert>
              </Stack>
            </>
          )}
        </Stack>
      </Modal>
      <LazerLookupWarningModal
        opened={lazerWarningOpened}
        onCancel={() => setLazerWarningOpened(false)}
        onConfirm={() => {
          setLazerLookupEnabled(true);
          setSettings((prev) => ({ ...prev, lazerLookupEnabled: true }));
          setLazerWarningOpened(false);
        }}
      />
    </>
  );
};

export default SettingsModal;
