import { Group, Select, Text } from '@mantine/core';
import { useMemo } from 'react';
import { indexOfEntry, type RangePreset } from './range';
import { useDateTimeFormat } from '../../hooks/useDateTimeFormat';
import type { ApiSnapshotHistoryEntry } from '../../Types';

interface CompareBarProps {
  entries: ApiSnapshotHistoryEntry[];
  baseId?: string;
  targetId?: string;
  preset: RangePreset;
  onPreset: (preset: RangePreset) => void;
  onBase: (id: string) => void;
  onTarget: (id: string) => void;
}

/**
 * What is compared with what: a snapshot to start from (A) and one to end at (B), with presets
 * for the common cases, plus the actions that belong to the comparison.
 */
export default function CompareBar({
  entries,
  baseId,
  targetId,
  preset,
  onPreset,
  onBase,
  onTarget,
}: CompareBarProps) {
  const { formatDateTime } = useDateTimeFormat();

  const label = (entry: ApiSnapshotHistoryEntry) =>
    `${formatDateTime(entry.time)}${entry.pin ? ` · ${entry.pin}` : ''}`;

  const targetIndex = indexOfEntry(entries, targetId);

  // A starts from anything older than B; B can be any snapshot.
  const baseData = useMemo(
    () => entries.slice(targetIndex + 1).map((entry) => ({ value: entry.id, label: label(entry) })),

    [entries, targetIndex, formatDateTime]
  );
  const targetData = useMemo(
    () => entries.map((entry) => ({ value: entry.id, label: label(entry) })),

    [entries, formatDateTime]
  );

  // "Since last pin" starts from a pinned snapshot older than the end, so it needs one.
  const hasPin = entries.slice(targetIndex + 1).some((entry) => entry.pin);

  if (entries.length < 2) return null;

  const compact = {
    size: 'xs' as const,
    allowDeselect: false,
    comboboxProps: { withinPortal: true },
  };

  return (
    <Group gap="xs" wrap="wrap" align="center">
      <Select
        aria-label="Compare"
        {...compact}
        w={150}
        value={preset}
        onChange={(value) => value && onPreset(value as RangePreset)}
        data={[
          { value: 'previous', label: 'Previous snapshot' },
          { value: 'pin', label: 'Since last pin', disabled: !hasPin },
          { value: 'all', label: 'All history' },
          ...(preset === 'custom' ? [{ value: 'custom', label: 'Custom range' }] : []),
        ]}
      />
      <Text size="xs" c="dimmed">
        from
      </Text>
      <Select
        {...compact}
        aria-label="Compare from"
        data={baseData}
        value={baseId ?? null}
        onChange={(value) => value && onBase(value)}
        w={170}
        placeholder="Nothing older"
        disabled={baseData.length === 0}
      />
      <Text size="xs" c="dimmed">
        to
      </Text>
      <Select
        {...compact}
        aria-label="Compare to"
        data={targetData}
        value={targetId ?? null}
        onChange={(value) => value && onTarget(value)}
        w={170}
        disabled={targetData.length === 0}
      />
    </Group>
  );
}
