import {IconCircleCheckFilled} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function NoIssueIcon() {
  const theme = useMantineTheme();
  return (
    <IconCircleCheckFilled
      color={theme.colors.green[5]}
      size={32}
    />
  );
}