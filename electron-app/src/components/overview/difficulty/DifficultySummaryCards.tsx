import { Paper, Stack, Text } from '@mantine/core';

export function SummaryCard({ label, value, subValue }: { label: string; value: string; subValue?: string }) {
  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase">{label}</Text>
        <Text fw={700} size="lg">{value}</Text>
        {subValue && <Text size="xs" c="dimmed">{subValue}</Text>}
      </Stack>
    </Paper>
  );
}
