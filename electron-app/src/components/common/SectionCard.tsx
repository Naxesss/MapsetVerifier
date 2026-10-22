import { Group, Paper, type PaperProps } from '@mantine/core';
import { CardTitle } from './Headings.tsx';
import { InfoIconTooltip } from './InfoIconTooltip.tsx';
import type { ReactNode } from 'react';

type SectionCardProps = Omit<PaperProps, 'withBorder' | 'radius' | 'p'> & {
  title?: ReactNode;
  /** Short explanation shown in an info tooltip next to the title. */
  info?: ReactNode;
  /** Controls aligned to the right of the title row. */
  actions?: ReactNode;
  /** Padding override for dense cards; defaults to `md`. */
  padding?: PaperProps['p'];
  children?: ReactNode;
};

/**
 * The one card surface: bordered, radius md, padding md, with an optional title row
 * (title, info tooltip, right-side actions).
 */
export default function SectionCard({
  title,
  info,
  actions,
  padding = 'md',
  children,
  ...paperProps
}: SectionCardProps) {
  const hasHeader = !!title || !!actions;

  return (
    <Paper withBorder radius="md" p={padding} {...paperProps}>
      {hasHeader && (
        <Group justify="space-between" align="center" gap="sm" mb="md" wrap="nowrap">
          <Group gap="xs" align="center" wrap="nowrap" style={{ minWidth: 0 }}>
            {typeof title === 'string' ? <CardTitle truncate>{title}</CardTitle> : title}
            {info && <InfoIconTooltip label={info} multiline maw={280} />}
          </Group>
          {actions && (
            <Group gap="xs" align="center" wrap="nowrap" style={{ flexShrink: 0 }}>
              {actions}
            </Group>
          )}
        </Group>
      )}
      {children}
    </Paper>
  );
}
