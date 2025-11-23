import {IconCircleCheckFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function NoIssueIcon({ size = 32 }: { size?: number }) {
  const theme = useMantineTheme();
  return (
    <IconCircleCheckFilled
      color={theme.colors.green[5]}
      size={size}
    />
  );
}