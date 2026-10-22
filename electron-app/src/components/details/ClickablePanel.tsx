import { IconChevronRight } from '@tabler/icons-react';
import { ReactNode } from 'react';
import ClickableRow from '../common/ClickableRow.tsx';

interface ClickablePanelProps {
  children: ReactNode;
  onClick: () => void;
}

/** A row that opens something else in the detail modal, with a chevron to say so. */
export default function ClickablePanel({ children, onClick }: ClickablePanelProps) {
  return (
    <ClickableRow wrap="nowrap" onClick={onClick}>
      <div style={{ flex: 1, minWidth: 0 }}>{children}</div>
      <IconChevronRight size={18} color="var(--mantine-color-dimmed)" style={{ flexShrink: 0 }} />
    </ClickableRow>
  );
}
