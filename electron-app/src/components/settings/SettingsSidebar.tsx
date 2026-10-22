import { Box, NavLink, Stack } from '@mantine/core';
import { Link, useParams } from 'react-router-dom';
import { resolveSettingsSection, settingsSections } from './settingsSections';
import { SectionTitle } from '../common/Headings';

export default function SettingsSidebar() {
  const params = useParams();
  const isDev = import.meta.env.DEV;
  const visibleSections = settingsSections.filter((section) => isDev || !section.devOnly);
  const activeSection = resolveSettingsSection(params.section, isDev);

  return (
    <Box
      component="nav"
      aria-label="Settings sections"
      py="md"
      px="sm"
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
      }}
    >
      <Box py="xs" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
        <SectionTitle ta="center">Settings</SectionTitle>
      </Box>
      <Stack gap="xs" mt="md" style={{ flex: 1, minHeight: 0, overflowY: 'auto' }}>
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
                borderRadius: 'var(--mantine-radius-default)',
              },
            }}
          />
        ))}
      </Stack>
    </Box>
  );
}
