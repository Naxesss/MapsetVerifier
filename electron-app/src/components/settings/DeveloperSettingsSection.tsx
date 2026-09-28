import { Button, Switch } from '@mantine/core';
import { IconCode, IconRotateClockwise2 } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';

export default function DeveloperSettingsSection() {
  const { settings, setSettings } = useSettings();

  return (
    <SettingsSection
      icon={<IconCode size={28} />}
      title="Developer"
      description="Development-mode options that have no effect in production builds."
    >
      <SettingsRow
        title="Gate backend in DEV"
        description="Starts the sidecar on port 5005 to mimic production mode. Needs a sidecar built into /bin/server/dist/<rid>/, and may need an app restart after changing it."
        control={
          <Switch
            checked={settings.gateInDev}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setSettings((prev) => ({ ...prev, gateInDev: checked }));
            }}
          />
        }
      />
      <SettingsRow
        title="Replay first-launch setup"
        description="Resets the setup wizard so it shows again immediately."
        control={
          <Button
            size="sm"
            variant="light"
            leftSection={<IconRotateClockwise2 size={18} />}
            onClick={() => setSettings((prev) => ({ ...prev, hasCompletedSetup: false }))}
          >
            Reset
          </Button>
        }
      />
      <SettingsRow
        title="Show check speed stats"
        description="Shows how long each check took to run, plus the combined check run time, on the Checks page."
        control={
          <Switch
            checked={settings.showCheckSpeedStats}
            onChange={(e) => {
              const checked = e.currentTarget.checked;
              setSettings((prev) => ({ ...prev, showCheckSpeedStats: checked }));
            }}
          />
        }
      />
    </SettingsSection>
  );
}
