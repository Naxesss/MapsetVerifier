import { Button, Group, TextInput } from '@mantine/core';
import { IconFolder } from '@tabler/icons-react';
import { notifyError } from '../../utils/notify.tsx';

interface FolderFieldProps {
  label: string;
  description?: string;
  placeholder?: string;
  value: string | undefined;
  /** Called with the folder the user picked. */
  onChange: (folder: string) => void;
}

/**
 * A folder setting: the path, read-only, with a Browse button that opens the system folder picker.
 * Clicking the empty field opens the picker too.
 */
export default function FolderField({
  label,
  description,
  placeholder,
  value,
  onChange,
}: FolderFieldProps) {
  const browse = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        onChange(result);
      }
    } catch (e: any) {
      console.error(`[Settings] Picking the ${label} failed:`, e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      notifyError(`Couldn't open the folder picker: ${msg}`);
    }
  };

  return (
    <Group align="flex-end" gap="sm" wrap="nowrap">
      <TextInput
        label={label}
        description={description}
        placeholder={placeholder}
        value={value ?? ''}
        readOnly
        style={{ flex: 1, minWidth: 0 }}
        onClick={() => !value && void browse()}
      />
      <Button
        size="sm"
        variant="light"
        leftSection={<IconFolder size={18} />}
        onClick={() => void browse()}
      >
        Browse
      </Button>
    </Group>
  );
}
