import { Group, Select, Text, useMantineTheme } from '@mantine/core';
import { IconBook } from '@tabler/icons-react';
import { pageMode } from './rcUtils';
import { ApiRcPageSummary } from '../../Types';
import GameModeIcon from '../icons/GameModeIcon';

interface RcPageSelectProps {
  pages: ApiRcPageSummary[];
  value: string;
  disabled?: boolean;
  /** Covered and coverable statement counts of a page, or null while they load. */
  coverageOf: (key: string) => { covered: number; total: number } | null;
  onChange: (key: string) => void;
}

function PageIcon({ pageKey }: { pageKey: string }) {
  const theme = useMantineTheme();
  const mode = pageMode(pageKey);

  return mode ? (
    <GameModeIcon mode={mode} size={16} color={theme.colors.gray[4]} />
  ) : (
    <IconBook size={16} color={theme.colors.gray[5]} />
  );
}

function groupOf(page: ApiRcPageSummary) {
  if (!page.hasStatements) return 'Reference';
  return pageMode(page.key) ? 'Game modes' : 'General';
}

/** Picks the ranking criteria page to show; scales to any number of pages and window widths. */
export default function RcPageSelect({
  pages,
  value,
  disabled,
  coverageOf,
  onChange,
}: RcPageSelectProps) {
  const groups = ['General', 'Game modes', 'Reference']
    .map((group) => ({
      group,
      items: pages
        .filter((page) => groupOf(page) === group)
        .map((page) => ({ value: page.key, label: page.title })),
    }))
    .filter((group) => group.items.length > 0);

  return (
    <Select
      aria-label="Ranking criteria page"
      w={240}
      data={groups}
      value={value}
      disabled={disabled}
      allowDeselect={false}
      maxDropdownHeight={420}
      checkIconPosition="right"
      comboboxProps={{ withinPortal: true }}
      leftSection={<PageIcon pageKey={value} />}
      onChange={(key) => key && onChange(key)}
      renderOption={({ option }) => {
        const coverage = pages.find((page) => page.key === option.value)?.hasStatements
          ? coverageOf(option.value)
          : null;

        return (
          <Group gap="sm" wrap="nowrap" w="100%">
            <PageIcon pageKey={option.value} />
            <Text size="sm" style={{ flex: 1 }}>
              {option.label}
            </Text>
            {coverage && (
              <Text size="xs" c="dimmed">
                {coverage.covered}/{coverage.total}
              </Text>
            )}
          </Group>
        );
      }}
    />
  );
}
