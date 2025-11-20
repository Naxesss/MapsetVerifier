import {
  IconCircleCheckFilled,
  IconExclamationCircleFilled
} from "@tabler/icons-react";
import {useMantineTheme} from "@mantine/core";

export default function MinorIcon() {
  const theme = useMantineTheme();
  return (
    <span style={{ position: "relative", display: "inline-block", width: 32, height: 32 }}>
      <IconCircleCheckFilled color={theme.colors.green[5]} size={32} />
      <IconExclamationCircleFilled
        color={theme.colors.orange[5]}
        size="16"
        style={{
          position: "absolute",
          bottom: 0,
          right: 0,
          borderRadius: "50%",
        }}
      />
    </span>
  );
}