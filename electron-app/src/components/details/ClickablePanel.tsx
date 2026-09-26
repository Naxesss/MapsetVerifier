import { Group, useMantineTheme } from '@mantine/core';
import { IconChevronRight } from '@tabler/icons-react';
import { ReactNode, useState } from 'react';

interface ClickablePanelProps {
  children: ReactNode;
  onClick: () => void;
}

/** A row that opens something else in the detail modal, highlighted on hover like the documentation rows. */
export default function ClickablePanel({ children, onClick }: ClickablePanelProps) {
  const theme = useMantineTheme();
  const [hovered, setHovered] = useState(false);
  const background = hovered
    ? theme.variantColorResolver({ variant: 'light', theme, color: 'blue' }).background
    : theme.variantColorResolver({ variant: 'light', theme, color: 'gray' }).background;

  return (
    <Group
      p="sm"
      w="100%"
      wrap="nowrap"
      style={{
        background,
        borderRadius: theme.defaultRadius,
        cursor: 'pointer',
        transition: 'background 0.2s',
      }}
      role="button"
      tabIndex={0}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onClick={onClick}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onClick();
        }
      }}
    >
      <div style={{ flex: 1, minWidth: 0 }}>{children}</div>
      <IconChevronRight size={18} color="var(--mantine-color-dimmed)" style={{ flexShrink: 0 }} />
    </Group>
  );
}
