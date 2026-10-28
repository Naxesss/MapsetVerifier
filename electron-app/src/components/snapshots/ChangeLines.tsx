import { Box, Button, Group, Stack, Text } from '@mantine/core';
import { useState } from 'react';
import { DiffOpIcon } from './ChangeCounts';
import OsuLink from '../common/OsuLink.tsx';
import type { ChangeLine } from './describe';

const FIRST_PAGE = 8;
const NEXT_PAGE = 60;

/** One line of a change: what happened to an object or timing line, with its details underneath. */
export function ChangeLineRow({ line }: { line: ChangeLine }) {
  return (
    <Box
      p="xs"
      px="sm"
      style={{
        borderRadius: 'var(--mantine-radius-sm)',
        background: 'var(--mantine-color-dark-7)',
        boxShadow: `inset 3px 0 0 0 ${
          line.op === 'Added'
            ? 'var(--mantine-color-green-6)'
            : line.op === 'Removed'
              ? 'var(--mantine-color-red-6)'
              : 'var(--mantine-color-yellow-6)'
        }`,
      }}
    >
      <Group gap="xs" wrap="nowrap" align="flex-start">
        <Box mt={3} style={{ display: 'flex', flexShrink: 0 }}>
          <DiffOpIcon op={line.op} />
        </Box>
        <Text size="sm" style={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
          <OsuLink text={line.stamp + line.text} />
        </Text>
      </Group>
      {line.details.length > 0 && (
        <Stack gap={2} pl="lg" mt="2xs">
          {line.details.map((detail) => (
            <Text key={detail} size="xs" c="dimmed">
              {detail}
            </Text>
          ))}
        </Stack>
      )}
    </Box>
  );
}

/** A list of change lines that shows the first few and pages in the rest on request. */
export default function ChangeLines({ lines }: { lines: ChangeLine[] }) {
  const [shown, setShown] = useState(FIRST_PAGE);
  const remaining = lines.length - shown;

  return (
    <Stack gap="xs">
      {lines.slice(0, shown).map((line) => (
        <ChangeLineRow key={line.key} line={line} />
      ))}
      {remaining > 0 && (
        <Button
          variant="subtle"
          size="compact-sm"
          style={{ alignSelf: 'flex-start' }}
          onClick={() => setShown((current) => current + NEXT_PAGE)}
        >
          Show {Math.min(remaining, NEXT_PAGE)} more ({remaining} left)
        </Button>
      )}
    </Stack>
  );
}
