import { Button, Switch } from '@mantine/core';
import { IconRefresh } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';
import { useUpdater } from '../../context/UpdaterContext';

export default function UpdateSettingsSection() {
  const { settings, setSettings } = useSettings();
  const { checkForUpdates, openUpdater, currentVersion, currentVersionIsPrerelease } = useUpdater();

  return (
    <SettingsSection
      icon={<IconRefresh size={28} />}
      title="Updates"
      description={`Current version: ${currentVersion}`}
    >
      <SettingsRow
        title="Application updates"
        description="Checks for available Mapset Verifier releases."
        control={
          <Button
            size="sm"
            variant="light"
            leftSection={<IconRefresh size={18} />}
            onClick={() => void openUpdater()}
          >
            Check for updates
          </Button>
        }
      />
      <SettingsRow
        title="Receive beta updates"
        description={
          settings.receivePrereleases
            ? 'Includes beta releases like 2.0.0-beta.1 when available.'
            : currentVersionIsPrerelease
              ? 'Only stable releases will be offered. Use Check for updates to return to stable when one is available.'
              : 'Only stable releases will be offered.'
        }
        control={
          <Switch
            checked={settings.receivePrereleases}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setSettings((prev) => ({ ...prev, receivePrereleases: checked }));
              void checkForUpdates({
                silent: false,
                openModal: true,
                allowPrereleaseOverride: checked,
              });
            }}
          />
        }
      />
    </SettingsSection>
  );
}
