import React from "react";
import { Group, ActionIcon, useMantineTheme, useMantineColorScheme } from "@mantine/core";
import { IconMinus, IconSquare, IconX } from "@tabler/icons-react";
import { Window } from "@tauri-apps/api/window";

const appWindow = Window.getCurrent();

const WindowBar: React.FC = () => {
  const theme = useMantineTheme();
  const { colorScheme } = useMantineColorScheme();
  const isDark = colorScheme === "dark";
  const bgColor = isDark ? theme.colors.dark[8] : theme.colors.gray[0];
  const textColor = theme.colors.primary[2];
  const barHeight = 32;

  // Get version from injected global constant
  const version = typeof TAURI_APP_VERSION !== "undefined" ? TAURI_APP_VERSION : "unknown";
  console.log("TAURI_APP_VERSION:", import.meta.env);
  

  return (
    <Group
      gap={0}
      style={{
        width: "100%",
        height: barHeight,
        background: bgColor,
        color: textColor,
        WebkitAppRegion: "drag",
        alignItems: "center",
        userSelect: "none",
        borderBottom: `1px solid ${theme.colors.dark[4]}`,
      } as any}
      px={theme.spacing.sm}
      justify="flex-end"
    >
      <Group style={{ flex: 1, alignItems: "center" }}>
        <span
          style={{
            fontWeight: 600,
            fontSize: 14,
            color: textColor,
            letterSpacing: 0.5,
            userSelect: "none",
            paddingLeft: theme.spacing.xs,
            paddingRight: theme.spacing.xs,
            WebkitAppRegion: "drag",
          } as any}
        >
          Mapset Verifier
          <span style={{ opacity: 0.7, fontWeight: 400, marginLeft: 4 }}>{version}</span>
        </span>
      </Group>
      <div style={{ WebkitAppRegion: "no-drag", display: "flex", gap: theme.spacing.xs } as any}>
        <ActionIcon
          variant="subtle"
          color={isDark ? "gray" : "dark"}
          aria-label="Minimize"
          onClick={() => appWindow.minimize()}
        >
          <IconMinus size={16} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color={isDark ? "gray" : "dark"}
          aria-label="Maximize"
          onClick={() => appWindow.toggleMaximize()}
        >
          <IconSquare size={16} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color="red"
          aria-label="Close"
          onClick={() => appWindow.close()}
        >
          <IconX size={16} color={theme.colors.red[6]} />
        </ActionIcon>
      </div>
    </Group>
  );
};

export default WindowBar;
