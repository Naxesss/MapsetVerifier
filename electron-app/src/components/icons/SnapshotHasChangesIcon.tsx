import { useMantineTheme } from '@mantine/core';
import { IconCirclePlusFilled } from '@tabler/icons-react';

export default function SnapshotHasChangesIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return <IconCirclePlusFilled color={theme.colors.blue[6]} size={size} />;
}
