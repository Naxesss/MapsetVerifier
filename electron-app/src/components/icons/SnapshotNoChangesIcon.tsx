import { useMantineTheme } from '@mantine/core';
import { IconCircleMinus } from '@tabler/icons-react';

export default function SnapshotNoChangesIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return <IconCircleMinus color={theme.colors.gray[5]} size={size} />;
}
