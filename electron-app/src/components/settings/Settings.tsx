import { Grid, NavLink, Stack, Title } from '@mantine/core';
import {
  IconAdjustments,
  IconAnalyze,
  IconCode,
  IconInfoCircle,
  IconPackage,
  IconRefresh,
  IconSettings,
} from '@tabler/icons-react';
import { Link, Navigate, useParams } from 'react-router-dom';
import AboutSettingsSection from './AboutSettingsSection';
import CheckSettingsSection from './CheckSettingsSection';
import DeveloperSettingsSection from './DeveloperSettingsSection';
import ExperimentalSettingsSection from './ExperimentalSettingsSection';
import GeneralSettingsSection from './GeneralSettingsSection';
import PluginSettingsSection from './PluginSettingsSection';
import UpdateSettingsSection from './UpdateSettingsSection';

type SettingsSectionId =
  | 'general'
  | 'checks'
  | 'experimental'
  | 'plugins'
  | 'updates'
  | 'about'
  | 'developer';

interface SettingsNavEntry {
  id: SettingsSectionId;
  label: string;
  description: string;
  icon: typeof IconSettings;
  devOnly?: boolean;
}

const settingsSections: SettingsNavEntry[] = [
  {
    id: 'general',
    label: 'General',
    description: 'Folders, fonts, and core preferences',
    icon: IconSettings,
  },
  {
    id: 'checks',
    label: 'Checks',
    description: 'Result display and check-run behavior',
    icon: IconAdjustments,
  },
  {
    id: 'experimental',
    label: 'Experimental',
    description: 'Features that are still being tested',
    icon: IconAnalyze,
  },
  {
    id: 'plugins',
    label: 'Plugins',
    description: 'Custom check plugin status',
    icon: IconPackage,
  },
  {
    id: 'updates',
    label: 'Updates',
    description: 'Version checks and beta releases',
    icon: IconRefresh,
  },
  {
    id: 'about',
    label: 'About',
    description: 'Folders and project links',
    icon: IconInfoCircle,
  },
  {
    id: 'developer',
    label: 'Developer',
    description: 'Development-only backend controls',
    icon: IconCode,
    devOnly: true,
  },
];

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
  const visibleSections = settingsSections.filter((section) => isDev || !section.devOnly);
  const routeSection = params.section === 'analysis' ? 'experimental' : params.section;
  const requestedSection = (routeSection ?? 'general') as SettingsSectionId;
  const activeSection = visibleSections.some((section) => section.id === requestedSection)
    ? requestedSection
    : null;

  if (!activeSection) {
    return <Navigate to="/settings" replace />;
  }

  return (
    <Grid>
      <Grid.Col span={{ base: 3 }}>
        <Stack gap="md">
          <Stack gap={2}>
            <Title order={2} size="h3">
              Settings
            </Title>
          </Stack>
          <Stack gap={4}>
            {visibleSections.map((section) => (
              <NavLink
                key={section.id}
                component={Link}
                to={section.id === 'general' ? '/settings' : `/settings/${section.id}`}
                label={section.label}
                description={section.description}
                leftSection={<section.icon size={18} />}
                active={activeSection === section.id}
                variant="light"
                styles={{
                  root: {
                    borderRadius: 5,
                  },
                }}
              />
            ))}
          </Stack>
        </Stack>
      </Grid.Col>
      <Grid.Col span={{ base: 9 }}>
        <Stack gap="md">{renderSettingsSection(activeSection)}</Stack>
      </Grid.Col>
    </Grid>
  );
}
