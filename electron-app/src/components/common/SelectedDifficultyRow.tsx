import { Group, Text } from '@mantine/core';
import type { ReactNode } from 'react';

interface SelectedDifficultyRowProps {
  /** Status and mode icons, 32px, in front of the name. */
  icons: ReactNode;
  name: ReactNode;
  /** Badges after the name, such as the interpreted level and the star rating. */
  badges?: ReactNode;
  /** Page-specific controls on the right, such as "Interpreted as" on Checks. */
  actions?: ReactNode;
}

/**
 * The one line above a difficulty's content on Checks and Snapshots: what is selected on the left,
 * what you can do with it on the right. Its height stays the same with or without actions.
 */
export default function SelectedDifficultyRow({
  icons,
  name,
  badges,
  actions,
}: SelectedDifficultyRowProps) {
  return (
    <Group justify="space-between" align="center" gap="sm" mih="var(--mv-control-height)">
      <Group gap="xs" align="center" wrap="nowrap" style={{ minWidth: 0 }}>
        {icons}
        <Text truncate>{name}</Text>
        {badges}
      </Group>
      {actions && (
        <Group gap="sm" align="center" wrap="nowrap">
          {actions}
        </Group>
      )}
    </Group>
  );
}
