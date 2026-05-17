import { useMantineTheme } from '@mantine/core';
import { IconInfoCircleFilled } from '@tabler/icons-react';

export default function InfoLevelIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return <IconInfoCircleFilled color={theme.colors.blue[5]} size={size} />;
}
