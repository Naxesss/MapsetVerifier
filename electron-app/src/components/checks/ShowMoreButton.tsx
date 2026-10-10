import { UnstyledButton } from '@mantine/core';

interface ShowMoreButtonProps {
  expanded: boolean;
  /** Issues left out while collapsed. */
  hiddenCount: number;
  onToggle: () => void;
}

/**
 * "Show N more" under a group of issues. A real button, so Enter and Space work, padded like the
 * issue rows above it so its text lines up with theirs.
 */
export default function ShowMoreButton({ expanded, hiddenCount, onToggle }: ShowMoreButtonProps) {
  return (
    <UnstyledButton
      px="xs"
      fz="sm"
      fw={500}
      aria-expanded={expanded}
      onClick={onToggle}
      style={{ alignSelf: 'flex-start', color: 'var(--mantine-color-blue-6)' }}
    >
      {expanded ? 'Hide extra issues' : `Show ${hiddenCount} more`}
    </UnstyledButton>
  );
}
