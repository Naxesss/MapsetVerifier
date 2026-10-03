import { ActionIcon, Box, CopyButton, Pill, Text, Tooltip } from '@mantine/core';
import { IconCheck, IconCopy } from '@tabler/icons-react';
import type { ClipboardEvent } from 'react';

/** Chips are boxes, so the browser copies each on its own line; paste them as one line instead. */
function copyAsOneLine(event: ClipboardEvent<HTMLDivElement>) {
  const selected = window.getSelection()?.toString() ?? '';
  if (!selected) return;
  event.preventDefault();
  event.clipboardData.setData('text/plain', selected.replace(/\s+/g, ' ').trim());
}

/**
 * A mapset's tags as chips in their written order, like the osu! website shows them. The chips
 * stay selectable and a selection copies as "tag tag tag"; the copy button copies the tags exactly
 * as written.
 */
export default function TagChips({ tags }: { tags: string }) {
  const words = tags.trim().split(/\s+/).filter(Boolean);

  if (words.length === 0) {
    return (
      <Text size="sm" c="dimmed">
        None
      </Text>
    );
  }

  return (
    <Box style={{ lineHeight: 2 }} onCopy={copyAsOneLine}>
      {words.map((word, index) => (
        <span key={`${word}-${index}`}>
          <Pill size="md" style={{ userSelect: 'text', WebkitUserSelect: 'text' }}>
            {word}
          </Pill>{' '}
        </span>
      ))}
      <CopyButton value={tags}>
        {({ copied, copy }) => (
          <Tooltip label={copied ? 'Copied' : 'Copy tags'}>
            <ActionIcon
              variant="subtle"
              color={copied ? 'teal' : 'gray'}
              size="sm"
              aria-label="Copy tags"
              onClick={copy}
              style={{ verticalAlign: 'middle' }}
            >
              {copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
            </ActionIcon>
          </Tooltip>
        )}
      </CopyButton>
    </Box>
  );
}
