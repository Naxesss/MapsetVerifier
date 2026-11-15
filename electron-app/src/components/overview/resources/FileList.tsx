import { Box, Group, NavLink, ScrollArea, Stack, Text } from '@mantine/core';
import { useState } from 'react';
import { KIND_COLORS, type ResourceGroup } from './resourceFiles.ts';
import { countWord } from '../../../utils/countWord.ts';
import { MicroLabel } from '../../common/Headings.tsx';
import SearchInput from '../../common/SearchInput.tsx';
import SectionCard from '../../common/SectionCard.tsx';

interface FileListProps {
  groups: ResourceGroup[];
  selectedKey: string;
  onSelect: (key: string) => void;
  folderSize: string;
}

/**
 * The mapset's files by kind, to pick one from. Stays in view while long details scroll, and
 * scrolls on its own when the files don't fit, as a set can have hundreds of hit sounds.
 */
function FileList({ groups, selectedKey, onSelect, folderSize }: FileListProps) {
  const [query, setQuery] = useState('');
  const needle = query.trim().toLowerCase();
  const shown = groups
    .map((group) => ({
      ...group,
      files: needle
        ? group.files.filter((file) => file.name.toLowerCase().includes(needle))
        : group.files,
    }))
    .filter((group) => group.files.length > 0);
  const fileCount = groups.reduce((total, group) => total + group.files.length, 0);

  return (
    <SectionCard
      padding="sm"
      style={{
        width: 280,
        flexShrink: 0,
        position: 'sticky',
        top: 'var(--mantine-spacing-md)',
        display: 'flex',
        flexDirection: 'column',
        maxHeight: 'calc(100dvh - var(--mv-top-bar-height) - 2 * var(--mantine-spacing-md))',
      }}
    >
      <SearchInput value={query} onChange={setQuery} placeholder="Filter files…" size="sm" />
      {/* Scrolls like the mapset list: the scrollbar keeps a gap from the rows. The card only has
          a max height, so the viewport shrinks as a flex item rather than by its usual 100%. */}
      <ScrollArea
        type="auto"
        scrollbars="y"
        offsetScrollbars="y"
        mt="xs"
        style={{ flex: '1 1 auto', minHeight: 0 }}
        styles={{
          root: { display: 'flex', flexDirection: 'column' },
          viewport: { flex: '1 1 auto', minHeight: 0, height: 'auto' },
        }}
      >
        <Box component="nav" aria-label="Mapset files">
          {shown.length === 0 && (
            <Text size="sm" c="dimmed" p="sm">
              No file matches.
            </Text>
          )}
          {shown.map((group) => (
            <Stack key={group.kind} gap={2} mt="sm">
              <Group justify="space-between" px="sm">
                <MicroLabel>{group.label}</MicroLabel>
                {group.files.length > 1 && (
                  <Text size="xs" c="dimmed">
                    {group.files.length}
                  </Text>
                )}
              </Group>
              {group.files.map((file) => (
                <NavLink
                  key={file.key}
                  active={file.key === selectedKey}
                  variant="light"
                  onClick={() => onSelect(file.key)}
                  leftSection={
                    <Box
                      w={8}
                      h={8}
                      bg={KIND_COLORS[file.kind]}
                      style={{ borderRadius: '50%', flexShrink: 0 }}
                    />
                  }
                  label={
                    <Text size="sm" truncate>
                      {file.name}
                    </Text>
                  }
                  rightSection={
                    file.size && (
                      <Text size="xs" c="dimmed">
                        {file.size}
                      </Text>
                    )
                  }
                  styles={{ root: { borderRadius: 'var(--mantine-radius-default)' } }}
                />
              ))}
            </Stack>
          ))}
        </Box>
      </ScrollArea>
      <Group
        justify="space-between"
        px="sm"
        pt="sm"
        mt="xs"
        style={{ borderTop: '1px solid var(--mantine-color-default-border)' }}
      >
        <Text size="xs" c="dimmed">
          {countWord(fileCount, 'file')}
        </Text>
        <Text size="xs" c="dimmed">
          {folderSize} in the folder
        </Text>
      </Group>
    </SectionCard>
  );
}

export default FileList;
