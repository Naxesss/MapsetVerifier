import { Group, type GroupProps } from '@mantine/core';
import type { ReactNode } from 'react';

type ClickableRowProps = Omit<GroupProps, 'onClick'> & {
  onClick: () => void;
  /** Dimmed until hovered or focused, for rows only shown as context. */
  muted?: boolean;
  children: ReactNode;
};

/**
 * The one row style for anything that opens something (documentation checks, ranking criteria
 * rules, links inside the detail modal). Hover and focus are styled in CSS (`.mv-clickable-row`),
 * so keyboard users get the same highlight as the mouse.
 */
export default function ClickableRow({
  onClick,
  muted,
  className,
  children,
  ...groupProps
}: ClickableRowProps) {
  return (
    <Group
      p="sm"
      w="100%"
      {...groupProps}
      className={className ? `mv-clickable-row ${className}` : 'mv-clickable-row'}
      data-muted={muted || undefined}
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(event) => {
        if (event.target !== event.currentTarget) return;
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onClick();
        }
      }}
    >
      {children}
    </Group>
  );
}
