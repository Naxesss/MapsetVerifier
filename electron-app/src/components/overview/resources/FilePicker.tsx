import { Box, Group, Select, Text } from '@mantine/core';
import { KIND_COLORS, type ResourceGroup } from './resourceFiles.ts';
import { countWord } from '../../../utils/countWord.ts';

interface FilePickerProps {
  groups: ResourceGroup[];
  selectedKey: string;
  onSelect: (key: string) => void;
  folderSize: string;
}

/** The file list as one searchable dropdown, for a window too narrow to show it beside the details. */
function FilePicker({ groups, selectedKey, onSelect, folderSize }: FilePickerProps) {
  const files = groups.flatMap((group) => group.files);
  const byKey = new Map(files.map((file) => [file.key, file]));
  const selected = byKey.get(selectedKey);

  return (
    <Group gap="sm" wrap="wrap" align="center">
      <Select
        aria-label="File"
        value={selectedKey}
        onChange={(key) => key && onSelect(key)}
        data={groups.map((group) => ({
          group: group.label,
          items: group.files.map((file) => ({ value: file.key, label: file.name })),
        }))}
        searchable
        // Typing replaces the shown name instead of adding to it.
        onFocus={(event) => event.currentTarget.select()}
        allowDeselect={false}
        nothingFoundMessage="No file matches"
        maxDropdownHeight={360}
        leftSection={selected && <KindDot color={KIND_COLORS[selected.kind]} />}
        renderOption={({ option }) => {
          const file = byKey.get(option.value);
          return (
            <Group gap="sm" wrap="nowrap" w="100%">
              {file && <KindDot color={KIND_COLORS[file.kind]} />}
              <Text size="sm" truncate style={{ flex: 1 }}>
                {option.label}
              </Text>
              {file?.size && (
                <Text size="xs" c="dimmed">
                  {file.size}
                </Text>
              )}
            </Group>
          );
        }}
        style={{ flex: '1 1 280px', maxWidth: 480 }}
      />
      <Text size="sm" c="dimmed">
        {countWord(files.length, 'file')} · {folderSize} in the folder
      </Text>
    </Group>
  );
}

function KindDot({ color }: { color: string }) {
  return <Box w={8} h={8} bg={color} style={{ borderRadius: '50%', flexShrink: 0 }} />;
}

export default FilePicker;
