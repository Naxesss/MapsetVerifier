import { Group, Stack, Text, ThemeIcon } from '@mantine/core';
import React from 'react';
import { CardTitle } from '../common/Headings.tsx';
import SectionCard from '../common/SectionCard.tsx';

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
    <SectionCard
      title={
        <Group gap="sm" align="center" wrap="nowrap">
          <ThemeIcon size={46} radius="md" variant="light" color="primary">
            {icon}
          </ThemeIcon>
          <Stack gap="2xs" style={{ minWidth: 0 }}>
            <CardTitle>{title}</CardTitle>
            {description && (
              <Text size="sm" c="dimmed">
                {description}
              </Text>
            )}
          </Stack>
        </Group>
      }
    >
      <Stack gap="sm">{children}</Stack>
    </SectionCard>
  );
}

/**
 * Rows that only apply while the row above them is on, indented under it so the dependency shows.
 */
export function SettingsSubRows({ children }: { children: React.ReactNode }) {
  return (
    <Stack gap="sm" pl="md" style={{ borderLeft: '2px solid var(--mantine-color-default-border)' }}>
      {children}
    </Stack>
  );
}

export function SettingsRow({ title, description, control }: SettingsRowProps) {
  // Title and description render as divs: callers pass rows with icons (Group) as the title.
  return (
    <Group justify="space-between" align="center" wrap="nowrap" gap="md">
      <Stack gap="2xs" style={{ minWidth: 0, flex: 1 }}>
        <Text component="div" size="sm" fw={500}>
          {title}
        </Text>
        {description && (
          <Text component="div" size="xs" c="dimmed">
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
