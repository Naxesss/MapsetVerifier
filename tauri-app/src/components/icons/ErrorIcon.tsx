import {IconHelpHexagonFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function ErrorIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return (
    <IconHelpHexagonFilled
      color={theme.colors.gray[5]}
      size={size}
    />
  );
}