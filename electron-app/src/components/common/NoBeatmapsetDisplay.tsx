import { Stack, Text } from '@mantine/core';
import { IconGhost2 } from '@tabler/icons-react';

function NoBeatmapsetDisplay() {
  return (
    <Stack
      h="100%"
      mih={280}
      justify="center"
      align="center"
      gap="sm"
      style={{ textAlign: 'center' }}
    >
      <IconGhost2
        size={112}
        stroke={1.4}
        style={{
          opacity: 0.22,
          color: 'var(--mantine-color-primary-2)',
          userSelect: 'none',
          pointerEvents: 'none',
        }}
      />
      <Text fw={700} size="lg">
        No beatmapset selected
      </Text>
      <Text size="sm" c="dimmed">
        Please select a beatmapset on the left side menu
      </Text>
    </Stack>
  );
}

export default NoBeatmapsetDisplay;
