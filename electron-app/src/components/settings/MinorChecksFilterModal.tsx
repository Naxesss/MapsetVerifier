import {
  Badge,
  Group,
  Modal,
  ScrollArea,
  Stack,
  Switch,
  Text,
  useMantineTheme,
  FocusTrap,
} from '@mantine/core';
import { IconListSearch, IconSearchOff } from '@tabler/icons-react';
import React, { useMemo, useState } from 'react';
import { useSettings } from '../../context/SettingsContext';
import { Z_INDEX } from '../../theme/layers';
import EmptyState from '../common/EmptyState.tsx';
import SearchInput from '../common/SearchInput.tsx';
import {
  dedupeDocumentationChecksById,
  filterDocumentationChecks,
} from '../documentation/filterDocumentationChecks';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import GameModeIcon from '../icons/GameModeIcon.tsx';
import type { ApiDocumentationCheck } from '../../Types';

interface MinorChecksFilterModalProps {
  opened: boolean;
  onClose: () => void;
}

function minorCapableDocumentationChecks(
  allChecks: ApiDocumentationCheck[]
): ApiDocumentationCheck[] {
  const minors = allChecks.filter((c) => c.outcomes.includes('Minor'));
  return dedupeDocumentationChecksById(minors).sort((a, b) =>
    a.description.localeCompare(b.description, undefined, { sensitivity: 'base' })
  );
}

/** Same fields as catalogue search, plus id substring match (e.g. "12" matches id 124). */
function filterMinorChecksList(
  checks: ApiDocumentationCheck[],
  query: string
): ApiDocumentationCheck[] {
  const q = query.trim();
  if (!q) return checks;

  return checks.filter((check) => {
    if (filterDocumentationChecks([check], query).length > 0) return true;
    return String(check.id).includes(q);
  });
}

const MinorChecksFilterModal: React.FC<MinorChecksFilterModalProps> = ({ opened, onClose }) => {
  const theme = useMantineTheme();
  const modeIconColor = theme.colors.gray[5];
  const { settings, setSettings } = useSettings();
  const { allChecks } = useDocumentationChecks();
  const [searchQuery, setSearchQuery] = useState('');

  const minorChecksList = useMemo(() => minorCapableDocumentationChecks(allChecks), [allChecks]);

  const [prevOpened, setPrevOpened] = useState(opened);

  if (opened !== prevOpened) {
    setPrevOpened(opened);
    if (!opened) setSearchQuery('');
  }

  const filteredMinorChecks = useMemo(
    () => filterMinorChecksList(minorChecksList, searchQuery),
    [minorChecksList, searchQuery]
  );

  const toggleVisible = (id: number, visible: boolean) => {
    setSettings((prev) => {
      const next = new Set(prev.hiddenMinorCheckIds);
      if (visible) next.delete(id);
      else next.add(id);
      return { ...prev, hiddenMinorCheckIds: [...next] };
    });
  };

  return (
    <Modal
      zIndex={Z_INDEX.modal}
      opened={opened}
      onClose={onClose}
      title="Negligible check filter"
      size="lg"
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Choose which checks can report negligible issues while &quot;Show negligible issues&quot;
          is on.
        </Text>
        <FocusTrap active={opened}>
          <SearchInput
            placeholder="Search checks…"
            hint="Searches check name, category, author, mode and ID."
            data-autofocus
            value={searchQuery}
            onChange={setSearchQuery}
          />
        </FocusTrap>
        <ScrollArea.Autosize mah={420} offsetScrollbars type="always" scrollbars="y">
          <Stack gap={0}>
            {minorChecksList.length === 0 ? (
              <EmptyState
                icon={IconListSearch}
                title="No checks report negligible issues"
                description="The check catalogue has no checks with a negligible outcome."
              />
            ) : filteredMinorChecks.length === 0 ? (
              <EmptyState icon={IconSearchOff} title="No checks match your search" />
            ) : (
              filteredMinorChecks.map((check) => (
                <Group
                  key={check.id}
                  justify="space-between"
                  align="center"
                  wrap="nowrap"
                  gap="sm"
                  py="xs"
                  pr="xs"
                  style={{ borderBottom: '1px solid var(--mantine-color-dark-5)' }}
                >
                  <Stack gap="xs" style={{ minWidth: 0, flex: 1 }}>
                    <Text size="sm" fw={500} lineClamp={2}>
                      {check.description}
                    </Text>
                    <Group gap="xs" wrap="wrap" align="center">
                      <Group gap="2xs" wrap="nowrap">
                        {check.modes.map((mode) => (
                          <GameModeIcon key={mode} mode={mode} size={15} color={modeIconColor} />
                        ))}
                      </Group>
                      <Badge color="gray">{check.category}</Badge>
                    </Group>
                  </Stack>
                  <Switch
                    checked={!settings.hiddenMinorCheckIds.includes(check.id)}
                    onChange={(e) => toggleVisible(check.id, e.currentTarget.checked)}
                    aria-label={`Show negligible issues for ${check.description}`}
                  />
                </Group>
              ))
            )}
          </Stack>
        </ScrollArea.Autosize>
      </Stack>
    </Modal>
  );
};

export default MinorChecksFilterModal;
