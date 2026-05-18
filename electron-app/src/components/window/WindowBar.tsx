import { Group, ActionIcon, useMantineTheme, useMantineColorScheme, Badge } from '@mantine/core';
import { IconMinus, IconSquare, IconX } from '@tabler/icons-react';
import React, { useEffect, useState } from 'react';
import iconUrl from '../../assets/icon.png';
import { isSemverPreRelease } from '../../utils/isSemverPreRelease';

const dragStyle = { WebkitAppRegion: 'drag' } as React.CSSProperties;
const noDragStyle = { WebkitAppRegion: 'no-drag' } as React.CSSProperties;

const WindowBar: React.FC = () => {
  const theme = useMantineTheme();
  const { colorScheme } = useMantineColorScheme();
  const isDark = colorScheme === 'dark';
  const bgColor = isDark ? theme.colors.dark[8] : theme.colors.gray[0];
  const textColor = theme.colors.primary[2];
  const barHeight = 32;

  const [version, setVersion] = useState<string>('unknown');
  const [isPrerelease, setIsPrerelease] = useState<boolean>(false);
  const isDev = import.meta.env.DEV;

  useEffect(() => {
    window.electronAPI
      ?.getVersion()
      .then((version) => {
        setVersion(version);
        setIsPrerelease(isSemverPreRelease(version));
      })
      .catch(() => setVersion('unknown'));
  }, []);

  const minimize = () => void window.electronAPI?.window.minimize();
  const toggleMaximize = () => void window.electronAPI?.window.toggleMaximize();
  const close = () => void window.electronAPI?.window.close();

  return (
    <Group
      gap={0}
      style={
        {
          ...dragStyle,
          position: 'fixed',
          top: 0,
          left: 0,
          width: '100%',
          zIndex: 2000,
          height: barHeight,
          background: bgColor,
          color: textColor,
          alignItems: 'center',
          userSelect: 'none',
          borderBottom: `1px solid ${theme.colors.dark[4]}`,
        } as React.CSSProperties
      }
      pl={theme.spacing.sm}
      justify="flex-end"
    >
      <Group
        gap={6}
        align="center"
        wrap="nowrap"
        style={{ ...dragStyle, flex: 1 } as React.CSSProperties}
      >
        <img
          src={iconUrl}
          alt=""
          width={18}
          height={18}
          draggable={false}
          style={
            {
              ...dragStyle,
              display: 'block',
              objectFit: 'contain',
              flexShrink: 0,
            } as React.CSSProperties
          }
        />
        <span
          style={
            {
              ...dragStyle,
              fontFamily: theme.headings.fontFamily,
              fontWeight: 600,
              fontSize: 14,
              color: textColor,
              letterSpacing: 0.5,
              userSelect: 'none',
              paddingLeft: theme.spacing.xs,
              paddingRight: theme.spacing.xs,
            } as React.CSSProperties
          }
        >
          Mapset Verifier
          <span style={{ opacity: 0.7, fontWeight: 400, marginLeft: 4 }}>{version}</span>
          {isDev && (
            <Badge
              color="red"
              size="xs"
              radius="sm"
              variant="filled"
              style={{ marginLeft: 8, verticalAlign: 'middle', opacity: 0.85 }}
            >
              DEV
            </Badge>
          )}
          {isPrerelease && (
            <Badge
              color="orange"
              size="xs"
              radius="sm"
              variant="filled"
              style={{ marginLeft: 8, verticalAlign: 'middle', opacity: 0.85 }}
            >
              Beta
            </Badge>
          )}
        </span>
      </Group>
      <div
        style={
          {
            ...noDragStyle,
            display: 'flex',
            alignSelf: 'stretch',
            height: '100%',
            gap: 0,
          } as React.CSSProperties
        }
      >
        <ActionIcon
          variant="subtle"
          color={isDark ? 'gray' : 'dark'}
          aria-label="Minimize"
          onClick={minimize}
          style={
            { ...noDragStyle, height: '100%', width: 36, borderRadius: 0 } as React.CSSProperties
          }
        >
          <IconMinus size={20} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color={isDark ? 'gray' : 'dark'}
          aria-label="Maximize"
          onClick={toggleMaximize}
          style={
            { ...noDragStyle, height: '100%', width: 36, borderRadius: 0 } as React.CSSProperties
          }
        >
          <IconSquare size={14} color={textColor} />
        </ActionIcon>
        <ActionIcon
          variant="subtle"
          color="red"
          aria-label="Close"
          onClick={close}
          style={
            { ...noDragStyle, height: '100%', width: 36, borderRadius: 0 } as React.CSSProperties
          }
        >
          <IconX size={20} color={theme.colors.red[6]} />
        </ActionIcon>
      </div>
    </Group>
  );
};

export default WindowBar;
