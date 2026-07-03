import { Alert, Switch, Text } from '@mantine/core';
import { IconCode, IconNote } from '@tabler/icons-react';
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
        description="Starts the sidecar on port 5005 to mimic production mode."
        control={
          <Switch
            checked={settings.gateInDev}
            onChange={(e) =>
              setSettings((prev) => ({ ...prev, gateInDev: e.currentTarget.checked }))
            }
          />
        }
      />
      <Alert icon={<IconNote />} title="Note" color="yellow" variant="light">
        <Text size="sm">
          This requires the sidecar to be built beforehand and available in{' '}
          <code>/bin/server/dist/&lt;rid&gt;/</code>. Changing this setting may require restarting
          the application.
        </Text>
      </Alert>
    </SettingsSection>
  );
}
