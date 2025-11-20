import {IconCircleXFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function ErrorIcon() {
  const theme = useMantineTheme();
  return (
    <IconCircleXFilled
      color={theme.colors.red[5]}
      size={32}
    />
  );
}