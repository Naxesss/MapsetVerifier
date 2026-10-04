import { Group, Text } from '@mantine/core';
import { IconArrowsExchange, IconMinus, IconPlus } from '@tabler/icons-react';
import type { ApiSnapshotCounts, DiffType } from '../../Types';

/** The icon of an added, removed or changed item, in the colour used for it everywhere. */
export function DiffOpIcon({ op, size = 14 }: { op: DiffType; size?: number }) {
  switch (op) {
    case 'Added':
      return <IconPlus size={size} color="var(--mantine-color-green-6)" aria-label="Added" />;
    case 'Removed':
      return <IconMinus size={size} color="var(--mantine-color-red-6)" aria-label="Removed" />;
    default:
      return (
        <IconArrowsExchange
          size={size}
          color="var(--mantine-color-yellow-6)"
          aria-label="Changed"
        />
      );
  }
}

/** Added, removed and changed counts as coloured numbers (+2 −1 ~3), or why there are none. */
export default function ChangeCounts({
  counts,
  empty = 'No changes',
}: {
  counts: Pick<ApiSnapshotCounts, 'added' | 'removed' | 'changed'>;
  empty?: string | null;
}) {
  if (counts.added + counts.removed + counts.changed === 0) {
    return empty ? (
      <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
        {empty}
      </Text>
    ) : null;
  }

  return (
    <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
      {counts.added > 0 && (
        <Text size="xs" fw={600} c="green.5">
          +{counts.added}
        </Text>
      )}
      {counts.removed > 0 && (
        <Text size="xs" fw={600} c="red.5">
          {'−'}
          {counts.removed}
        </Text>
      )}
      {counts.changed > 0 && (
        <Text size="xs" fw={600} c="yellow.5">
          ~{counts.changed}
        </Text>
      )}
    </Group>
  );
}
