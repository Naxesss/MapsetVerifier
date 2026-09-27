import { Stack, Text } from '@mantine/core';
import type { Icon } from '@tabler/icons-react';
import type { ReactNode } from 'react';

interface EmptyStateProps {
  icon: Icon;
  title: string;
  description?: ReactNode;
  action?: ReactNode;
  /** Fill the available height, for empty states that replace a whole page. */
  fullHeight?: boolean;
}

/** The one way to say "nothing here": centred icon, one-line title, one sentence, optional action. */
export default function EmptyState({
  icon: IconComponent,
  title,
  description,
  action,
  fullHeight = false,
}: EmptyStateProps) {
  return (
    <Stack
      h={fullHeight ? '100%' : undefined}
      mih={fullHeight ? 280 : undefined}
      py={fullHeight ? undefined : 'lg'}
      justify="center"
      align="center"
      gap="sm"
      style={{ textAlign: 'center' }}
    >
      <IconComponent
        size={fullHeight ? 96 : 48}
        stroke={1.4}
        aria-hidden
        style={{
          opacity: 0.3,
          color: 'var(--mantine-color-primary-2)',
          userSelect: 'none',
          pointerEvents: 'none',
        }}
      />
      <Text fw={600} size="md">
        {title}
      </Text>
      {description && (
        <Text size="sm" c="dimmed" maw={420}>
          {description}
        </Text>
      )}
      {action}
    </Stack>
  );
}
