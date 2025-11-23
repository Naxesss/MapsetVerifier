import {IconExclamationCircleFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function WarningIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return (
    <IconExclamationCircleFilled
      color={theme.colors.orange[5]}
      size={size}
    />
  );
}