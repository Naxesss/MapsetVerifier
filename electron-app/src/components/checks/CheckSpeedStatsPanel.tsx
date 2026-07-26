import { Badge, Box, Group, ScrollArea, Table, Text } from '@mantine/core';
import { IconClockBolt } from '@tabler/icons-react';
import { useMemo } from 'react';
import type { ApiCheckTimingReport } from '../../Types';

interface CheckSpeedStatsPanelProps {
  report: ApiCheckTimingReport;
}

function formatMs(ms: number) {
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)} s`;
  return `${ms} ms`;
}

function CheckSpeedStatsPanel({ report }: CheckSpeedStatsPanelProps) {
  const sortedChecks = useMemo(
    () => [...report.checks].sort((a, b) => b.elapsedMs - a.elapsedMs),
    [report.checks]
  );

  const combinedMs = useMemo(
    () => report.checks.reduce((sum, check) => sum + check.elapsedMs, 0),
    [report.checks]
  );

  return (
    <Box
      p="xs"
      style={{
        borderRadius: 'var(--mantine-radius-md)',
        border: '1px solid var(--mantine-color-dark-4)',
        backgroundColor: 'var(--mantine-color-dark-7)',
      }}
    >
      <Group justify="space-between" mb={6}>
        <Group gap={6}>
          <IconClockBolt size={16} />
          <Text size="sm" fw={600}>
            Check speed stats
          </Text>
        </Group>
        <Group gap="md">
          <Text size="xs" c="dimmed">
            Total run time:{' '}
            <Text span fw={600} c="gray.2" inherit>
              {formatMs(report.totalElapsedMs)}
            </Text>
          </Text>
          <Text size="xs" c="dimmed">
            Combined check time:{' '}
            <Text span fw={600} c="gray.2" inherit>
              {formatMs(combinedMs)}
            </Text>
          </Text>
        </Group>
      </Group>
      <Text size="xs" c="dimmed" mb={6}>
        Checks run in parallel, so combined check time will usually exceed the total run time.
      </Text>
      <ScrollArea.Autosize mah={260}>
        <Table striped highlightOnHover verticalSpacing={4} fz="xs">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Check</Table.Th>
              <Table.Th>Difficulty</Table.Th>
              <Table.Th style={{ textAlign: 'right' }}>Time</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {sortedChecks.map((check, index) => (
              <Table.Tr key={`${check.checkName}-${check.difficulty ?? 'general'}-${index}`}>
                <Table.Td>{check.checkName}</Table.Td>
                <Table.Td>
                  {check.difficulty ? (
                    <Badge size="xs" variant="light" color="gray">
                      {check.difficulty}
                    </Badge>
                  ) : (
                    <Text size="xs" c="dimmed">
                      —
                    </Text>
                  )}
                </Table.Td>
                <Table.Td style={{ textAlign: 'right' }}>{formatMs(check.elapsedMs)}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </ScrollArea.Autosize>
    </Box>
  );
}

export default CheckSpeedStatsPanel;
