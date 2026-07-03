import { Group, Paper, Stack, Text, ThemeIcon } from '@mantine/core';
import React from 'react';

interface SettingsSectionProps {
  title: string;
  description?: string;
  icon: React.ReactNode;
  children: React.ReactNode;
}

interface SettingsRowProps {
  title: React.ReactNode;
  description?: React.ReactNode;
  control: React.ReactNode;
}

export function SettingsSection({ title, description, icon, children }: SettingsSectionProps) {
  return (
    <Paper withBorder radius="sm" p="md">
      <Stack gap="md">
        <Group gap="sm" align="center" wrap="nowrap">
          <ThemeIcon size={46} radius="sm" variant="light" color="primary">
            {icon}
          </ThemeIcon>
          <Stack gap={2} style={{ minWidth: 0 }}>
            <Text fw={700}>{title}</Text>
            {description && (
              <Text size="sm" c="dimmed">
                {description}
              </Text>
            )}
          </Stack>
        </Group>
        <Stack gap="sm">{children}</Stack>
      </Stack>
    </Paper>
  );
}

export function SettingsRow({ title, description, control }: SettingsRowProps) {
  return (
    <Group justify="space-between" align="center" wrap="nowrap" gap="md">
      <Stack gap={2} style={{ minWidth: 0, flex: 1 }}>
        <Text size="sm" fw={500}>
          {title}
        </Text>
        {description && (
          <Text size="xs" c="dimmed">
            {description}
          </Text>
        )}
      </Stack>
      <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
        {control}
      </Group>
    </Group>
  );
}
