import { IconPackage } from '@tabler/icons-react';
import PluginManager from './PluginManager';
import { SettingsSection } from './SettingsSection';

export default function PluginSettingsSection() {
  return (
    <SettingsSection
      icon={<IconPackage size={28} />}
      title="Plugins"
      description="Custom check DLLs loaded by the backend from the plugin folder."
    >
      <PluginManager opened />
    </SettingsSection>
  );
}
