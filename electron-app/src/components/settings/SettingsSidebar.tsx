import { Box, NavLink, Stack, Title } from '@mantine/core';
import { Link, useParams } from 'react-router-dom';
import { resolveSettingsSection, settingsSections } from './settingsSections';

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
        viewTransitionName: 'settings-toc',
      }}
    >
      <Box py="xs" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
        <Title order={2} size="h3" ta="center" m={0}>
          Settings
        </Title>
      </Box>
      <Stack gap={4} mt="md" style={{ flex: 1, minHeight: 0, overflowY: 'auto' }}>
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
    </Box>
  );
}
