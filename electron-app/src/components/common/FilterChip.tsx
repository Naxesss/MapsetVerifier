import { Button, type MantineColor } from '@mantine/core';
import type { ReactNode } from 'react';

interface FilterChipProps {
  label: ReactNode;
  /** How many items this chip keeps, shown after the label. */
  count?: number;
  /** Colour while active; the icon carries the colour while it is not. */
  color: MantineColor;
  icon?: ReactNode;
  active: boolean;
  onClick: () => void;
}

/**
 * The one quick-filter pill (severities on Checks, change types on Snapshots, coverage on Ranking
 * criteria): its icon in the item's colour, the label and a count, and a tinted fill while active.
 */
export default function FilterChip({
  label,
  count,
  color,
  icon,
  active,
  onClick,
}: FilterChipProps) {
  return (
    <Button
      size="compact-sm"
      radius="xl"
      variant={active ? 'light' : 'subtle'}
      color={active ? color : 'gray'}
      leftSection={icon}
      aria-pressed={active}
      onClick={onClick}
    >
      {label}
      {count !== undefined && ` ${count}`}
    </Button>
  );
}
