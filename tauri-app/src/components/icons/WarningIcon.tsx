import { useMantineTheme } from '@mantine/core';
import { IconExclamationCircleFilled } from '@tabler/icons-react';

export default function WarningIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return <IconExclamationCircleFilled color={theme.colors.orange[5]} size={size} />;
}
