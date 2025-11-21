import {IconHelpHexagonFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function ErrorIcon() {
  const theme = useMantineTheme();
  return (
    <IconHelpHexagonFilled
      color={theme.colors.gray[5]}
      size={32}
    />
  );
}