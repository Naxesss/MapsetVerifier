import { Stack } from '@mantine/core';
import { Navigate, useParams } from 'react-router-dom';
import AboutSettingsSection from './AboutSettingsSection';
import CheckSettingsSection from './CheckSettingsSection';
import DeveloperSettingsSection from './DeveloperSettingsSection';
import ExperimentalSettingsSection from './ExperimentalSettingsSection';
import GeneralSettingsSection from './GeneralSettingsSection';
import PluginSettingsSection from './PluginSettingsSection';
import { resolveSettingsSection, type SettingsSectionId } from './settingsSections';
import UpdateSettingsSection from './UpdateSettingsSection';

function renderSettingsSection(section: SettingsSectionId) {
  switch (section) {
    case 'general':
      return <GeneralSettingsSection />;
    case 'checks':
      return <CheckSettingsSection />;
    case 'experimental':
      return <ExperimentalSettingsSection />;
    case 'plugins':
      return <PluginSettingsSection />;
    case 'updates':
      return <UpdateSettingsSection />;
    case 'about':
      return <AboutSettingsSection />;
    case 'developer':
      return <DeveloperSettingsSection />;
  }
}

export default function Settings() {
  const params = useParams();
  const isDev = import.meta.env.DEV;
  const activeSection = resolveSettingsSection(params.section, isDev);

  if (!activeSection) {
    return <Navigate to="/settings" replace />;
  }

  return (
    <Stack gap="md" py="md" px="md" style={{ viewTransitionName: 'settings-content' }}>
      {renderSettingsSection(activeSection)}
    </Stack>
  );
}
