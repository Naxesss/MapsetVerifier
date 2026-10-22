import { CloseButton, TextInput, type TextInputProps } from '@mantine/core';
import { IconSearch } from '@tabler/icons-react';
import { InfoIconTooltip } from './InfoIconTooltip.tsx';

type SearchInputProps = Omit<
  TextInputProps,
  'value' | 'onChange' | 'leftSection' | 'rightSection' | 'placeholder'
> & {
  value: string;
  onChange: (value: string) => void;
  /** Short, e.g. "Search mapsets…". What it searches goes in `hint`, not here. */
  placeholder: string;
  /** Which fields are searched, shown in a tooltip while the field is empty. */
  hint?: string;
};

/** The one search field: search icon, clear button once there is text, and a hint tooltip. */
export default function SearchInput({
  value,
  onChange,
  placeholder,
  hint,
  ...inputProps
}: SearchInputProps) {
  return (
    <TextInput
      {...inputProps}
      placeholder={placeholder}
      value={value}
      onChange={(event) => onChange(event.currentTarget.value)}
      leftSection={<IconSearch size={18} stroke={1.5} />}
      rightSectionPointerEvents="all"
      rightSection={
        value ? (
          <CloseButton aria-label="Clear search" size="sm" onClick={() => onChange('')} />
        ) : hint ? (
          <InfoIconTooltip label={hint} multiline maw={260} />
        ) : null
      }
    />
  );
}
