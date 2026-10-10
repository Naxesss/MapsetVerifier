import { Group, Select, Text, useMantineTheme } from '@mantine/core';
import { IconListCheck } from '@tabler/icons-react';
import { formatGameModeLabel } from '../../utils/gameMode';
import GameModeIcon from '../icons/GameModeIcon';
import type { Mode } from '../../Types';

/** 'general' or one of the game modes. */
export type DocumentationCategory = 'general' | Mode;

const CATEGORIES: DocumentationCategory[] = ['general', 'Standard', 'Taiko', 'Catch', 'Mania'];

export function documentationCategoryLabel(category: DocumentationCategory) {
  return category === 'general' ? 'General' : formatGameModeLabel(category);
}

function CategoryIcon({ category }: { category: DocumentationCategory | null }) {
  const theme = useMantineTheme();

  return category && category !== 'general' ? (
    <GameModeIcon mode={category} size={16} color={theme.colors.gray[4]} />
  ) : (
    <IconListCheck size={16} color={theme.colors.gray[5]} />
  );
}

interface DocumentationModeSelectProps {
  value: DocumentationCategory;
  disabled?: boolean;
  /** Number of checks per category, or null while they load. */
  countOf: (category: DocumentationCategory) => number | null;
  onChange: (category: DocumentationCategory) => void;
}

/** Picks which checks to list; the same control Ranking criteria uses to pick a page. */
export default function DocumentationModeSelect({
  value,
  disabled,
  countOf,
  onChange,
}: DocumentationModeSelectProps) {
  return (
    <Select
      aria-label="Check category"
      w={240}
      data={CATEGORIES.map((category) => ({
        value: category,
        label: documentationCategoryLabel(category),
      }))}
      // Search results come from every category, so none is selected meanwhile.
      value={disabled ? null : value}
      placeholder="All categories"
      disabled={disabled}
      allowDeselect={false}
      checkIconPosition="right"
      comboboxProps={{ withinPortal: true }}
      leftSection={<CategoryIcon category={disabled ? null : value} />}
      onChange={(category) => category && onChange(category as DocumentationCategory)}
      renderOption={({ option }) => {
        const count = countOf(option.value as DocumentationCategory);

        return (
          <Group gap="sm" wrap="nowrap" w="100%">
            <CategoryIcon category={option.value as DocumentationCategory} />
            <Text size="sm" style={{ flex: 1 }}>
              {option.label}
            </Text>
            {count !== null && (
              <Text size="xs" c="dimmed">
                {count}
              </Text>
            )}
          </Group>
        );
      }}
    />
  );
}
