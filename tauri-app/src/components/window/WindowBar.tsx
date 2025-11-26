import React from "react";
import { Group, ActionIcon, useMantineTheme, useMantineColorScheme, Badge } from "@mantine/core";
import { IconMinus, IconSquare, IconX } from "@tabler/icons-react";
import { getCurrentWindow } from "@tauri-apps/api/window";

const appWindow = getCurrentWindow();

const WindowBar: React.FC = () => {
  const theme = useMantineTheme();
  const { colorScheme } = useMantineColorScheme();
  const isDark = colorScheme === "dark";
  const bgColor = isDark ? theme.colors.dark[8] : theme.colors.gray[0];
  const textColor = theme.colors.primary[2];
  const barHeight = 32;

  // Get version from injected global constant
  const version = typeof TAURI_APP_VERSION !== "undefined" ? TAURI_APP_VERSION : "unknown";
  const isDev = process.env.NODE_ENV === "development";

  return (
    <Group
      gap={0}
      data-tauri-drag-region
      style={{
        width: "100%",
        height: barHeight,
        background: bgColor,
        color: textColor,
        alignItems: "center",
        userSelect: "none",
        borderBottom: `1px solid ${theme.colors.dark[4]}`,
      } as any}
      px={theme.spacing.sm}
      justify="flex-end"
    >
      <Group style={{ flex: 1, alignItems: "center" }} data-tauri-drag-region>
        <span
          style={{
            fontFamily: theme.headings.fontFamily,
            fontWeight: 600,
            fontSize: 14,
            color: textColor,
            letterSpacing: 0.5,
            userSelect: "none",
            paddingLeft: theme.spacing.xs,
            paddingRight: theme.spacing.xs,
          } as any}
          data-tauri-drag-region
        >
          Mapset Verifier
          <span style={{ opacity: 0.7, fontWeight: 400, marginLeft: 4 }}>{version}</span>
          {isDev && (
            <Badge
              color="red"
              size="xs"
              radius="sm"
              style={{ marginLeft: 8, verticalAlign: "middle", opacity: 0.85 }}
              variant="filled"
              data-tauri-drag-region
            >
              DEV
            </Badge>
          )}
        </span>
      </Group>
      <div
        style={{ display: "flex", gap: theme.spacing.xs } as any}
        // Explicitly disable drag in the control area
        data-tauri-drag-region={undefined}
      >
        <ActionIcon
          variant="subtle"
          color={isDark ? "gray" : "dark"}
          aria-label="Minimize"
          onClick={() => appWindow.minimize()}
          // Explicitly disable drag in the control area
          data-tauri-drag-region={undefined}
        >
          <IconMinus size={16} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color={isDark ? "gray" : "dark"}
          aria-label="Maximize"
          onClick={() => appWindow.toggleMaximize()}
          // Explicitly disable drag in the control area
          data-tauri-drag-region={undefined}
        >
          <IconSquare size={16} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color="red"
          aria-label="Close"
          onClick={() => appWindow.close()}
          // Explicitly disable drag in the control area
          data-tauri-drag-region={undefined}
        >
          <IconX size={16} color={theme.colors.red[6]} />
        </ActionIcon>
      </div>
    </Group>
  );
};

export default WindowBar;
