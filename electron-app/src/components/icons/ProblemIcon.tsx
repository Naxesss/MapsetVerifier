import { useMantineTheme } from '@mantine/core';
import { IconCircleXFilled } from '@tabler/icons-react';

export default function ProblemIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return <IconCircleXFilled color={theme.colors.red[6]} size={size} />;
}
