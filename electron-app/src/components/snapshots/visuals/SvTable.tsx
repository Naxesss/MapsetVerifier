import { Table, Text } from '@mantine/core';
import AppTable from '../../common/AppTable.tsx';
import OsuLink from '../../common/OsuLink.tsx';
import { formatStamp } from '../describe';
import type { ApiSnapshotChange } from '../../../Types';

/**
 * A hunk's slider velocity changes, the ones the table shows: a green line's SV changed, or a
 * green line added or removed with the SV it has (or had).
 */
export function svChanges(changes: ApiSnapshotChange[]) {
  return changes
    .filter(
      (c) =>
        (c.field === 'Sv' && c.before != null && c.after != null) ||
        (c.field === 'GreenLine' && c.op === 'Added' && c.after != null) ||
        (c.field === 'GreenLine' && c.op === 'Removed' && c.before != null)
    )
    .sort((a, b) => a.time - b.time);
}

export function hasSvChanges(changes: ApiSnapshotChange[]) {
  return svChanges(changes).length > 0;
}

const multiplier = (value: number) => `${Math.round(value * 100) / 100}x`;

function ChangeCell({ change }: { change: ApiSnapshotChange }) {
  if (change.op === 'Added')
    return (
      <Text size="sm" c="green.5">
        Added
      </Text>
    );
  if (change.op === 'Removed')
    return (
      <Text size="sm" c="red.5">
        Removed
      </Text>
    );

  const delta = Number(change.after) - Number(change.before);
  return (
    <Text size="sm" c={delta > 0 ? 'green.5' : 'red.5'}>
      {delta > 0 ? '+' : '−'}
      {multiplier(Math.abs(delta))}
    </Text>
  );
}

/**
 * A hunk's slider velocity changes, one timing line per row: where it is, what it was, what it is
 * now and by how much it went up or down, or that the line is new or gone. A curve of only the
 * changed lines could not show the lines between them, so it read as shapes the map does not
 * have; the numbers say it plainly.
 */
export default function SvTable({ changes }: { changes: ApiSnapshotChange[] }) {
  const rows = svChanges(changes);

  return (
    <AppTable verticalSpacing={4} highlightOnHover={false}>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Timing line</Table.Th>
          <Table.Th>Before</Table.Th>
          <Table.Th>After</Table.Th>
          <Table.Th>Change</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {rows.map((c) => (
          <Table.Tr key={`${c.time}-${c.op}`}>
            <Table.Td>
              <Text size="sm">
                <OsuLink text={formatStamp(c.time)} disableSeparators />
              </Text>
            </Table.Td>
            <Table.Td>
              <Text size="sm" c="dimmed">
                {c.before != null ? multiplier(Number(c.before)) : '–'}
              </Text>
            </Table.Td>
            <Table.Td>
              <Text size="sm" fw={600}>
                {c.after != null ? multiplier(Number(c.after)) : '–'}
              </Text>
            </Table.Td>
            <Table.Td>
              <ChangeCell change={c} />
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </AppTable>
  );
}
