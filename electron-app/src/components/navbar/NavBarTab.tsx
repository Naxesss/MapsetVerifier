import { alpha, Group, Text, UnstyledButton, useMantineTheme } from '@mantine/core';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { NAV_INDICATOR_TRANSITION_MS } from './navConfig';
import type { NavEntry } from './navConfig';

export interface NavBarTabProps {
  item: NavEntry;
  activeRoute: string;
  controlRef: (node: HTMLElement | null) => void;
}

export function NavBarTab({ item, activeRoute, controlRef }: NavBarTabProps) {
  const theme = useMantineTheme();
  const disabled = item.disabled ?? false;
  const [hovered, setHovered] = useState(false);

  const isActive =
    item.to === '/' ? activeRoute === '/' : item.to !== '/' && activeRoute.startsWith(item.to);
  const labelColor = isActive ? theme.black : theme.colors.dark[0];

  const shellStyles = useMemo(
    () => ({
      position: 'relative' as const,
      zIndex: 1,
      borderRadius: 5,
      fontFamily: theme.headings.fontFamily,
      fontWeight: 500 as const,
      fontSize: theme.fontSizes.sm,
      lineHeight: 1,
      color: labelColor,
      viewTransitionName: 'none',
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
    <Group gap="sm" px="sm" py="sm" wrap="nowrap">
      <item.icon aria-hidden />
      <Text component="span" fz="inherit" fw="inherit" lh={1} truncate>
        {item.label}
      </Text>
    </Group>
  );

  if (disabled) {
    return (
      <UnstyledButton
        ref={controlRef}
        type="button"
        disabled
        aria-disabled
        data-nav-route={item.to}
        opacity={0.45}
        style={shellStyles}
      >
        {labelBody}
      </UnstyledButton>
    );
  }

  return (
    <UnstyledButton
      ref={controlRef}
      component={Link}
      to={item.to}
      viewTransition
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
  );
}
