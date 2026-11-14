import { alpha, Group, Text, Tooltip, UnstyledButton, useMantineTheme } from '@mantine/core';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { NAV_CONTROL_SIZE, NAV_ICON_SIZE, NAV_INDICATOR_TRANSITION_MS } from './navConfig';
import type { NavEntry } from './navConfig';

export interface NavBarTabProps {
  item: NavEntry;
  activeRoute: string;
  controlRef: (node: HTMLElement | null) => void;
  /** False in smaller windows: only the icon shows, and the name moves to a tooltip. */
  showLabel?: boolean;
}

export function NavBarTab({ item, activeRoute, controlRef, showLabel = true }: NavBarTabProps) {
  const theme = useMantineTheme();
  const [hovered, setHovered] = useState(false);

  const isActive =
    item.to === '/' ? activeRoute === '/' : item.to !== '/' && activeRoute.startsWith(item.to);
  const labelColor = isActive ? theme.black : theme.colors.dark[0];

  const shellStyles = useMemo(
    () => ({
      position: 'relative' as const,
      zIndex: 1,
      borderRadius: 'var(--mantine-radius-default)',
      fontFamily: theme.headings.fontFamily,
      fontWeight: 500 as const,
      fontSize: theme.fontSizes.sm,
      lineHeight: 1,
      color: labelColor,
      transition: `color ${NAV_INDICATOR_TRANSITION_MS}ms ease, background-color 120ms ease`,
    }),
    [labelColor, theme.fontSizes.sm, theme.headings.fontFamily]
  );

  const linkOutline = alpha(theme.colors.primary[5], 0.6);
  const inactiveHoverBg = alpha(theme.white, 0.1);

  const linkStyles = useMemo(
    () => ({
      root: {
        '&:focus-visible': {
          outline: `${linkOutline} solid 2px`,
          outlineOffset: 2,
        },
      },
    }),
    [linkOutline]
  );

  const labelBody = (
    <Group gap={8} px={showLabel ? 10 : 6} h={NAV_CONTROL_SIZE} wrap="nowrap">
      <item.icon size={NAV_ICON_SIZE} aria-hidden />
      {showLabel && (
        <Text component="span" fz="inherit" fw="inherit" lh={1} truncate>
          {item.label}
        </Text>
      )}
    </Group>
  );

  return (
    <Tooltip label={item.label} position="bottom" disabled={showLabel}>
      <UnstyledButton
        ref={controlRef}
        component={Link}
        to={item.to}
        aria-label={showLabel ? undefined : item.label}
        data-nav-route={item.to}
        data-active={isActive || undefined}
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
        style={{
          ...shellStyles,
          ...(hovered && !isActive ? { backgroundColor: inactiveHoverBg } : undefined),
        }}
        styles={linkStyles}
      >
        {labelBody}
      </UnstyledButton>
    </Tooltip>
  );
}
