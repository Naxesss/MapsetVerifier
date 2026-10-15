import { Box, FloatingIndicator, Group, useMantineTheme } from '@mantine/core';
import { useCallback, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { NavBarTab } from './NavBarTab';
import { NAV_INDICATOR_TRANSITION_MS, navItems } from './navConfig';

export interface MainNavRailProps {
  activeRoute: string;
}

/** Sliding selected pill synced to the active route (Mantine FloatingIndicator pattern). */
export function MainNavRail({ activeRoute }: MainNavRailProps) {
  const theme = useMantineTheme();
  const [navRootEl, setNavRootEl] = useState<HTMLElement | null>(null);
  const [targetControl, setTargetControl] = useState<HTMLElement | null>(null);
  const controlsRefs = useRef<Partial<Record<string, HTMLElement | null>>>({});

  const attachNavRoot = useCallback(
    (el: HTMLElement | null) => setNavRootEl((prev) => (prev === el ? prev : el)),
    []
  );

  const setControlRef = useMemo(() => {
    const refs: Partial<Record<string, (node: HTMLElement | null) => void>> = {};
    for (const item of navItems) {
      const href = item.to;
      refs[href] = (node) => {
        controlsRefs.current[href] = node;
      };
    }
    return refs as Record<string, (node: HTMLElement | null) => void>;
  }, []);

  const selectionFill = theme.colors.primary[2];

  useLayoutEffect(() => {
    const node = controlsRefs.current[activeRoute] ?? null;
    setTargetControl((prev) => (prev === node ? prev : node));
  }, [activeRoute, navRootEl]);

  return (
    <Box
      ref={attachNavRoot}
      component="nav"
      aria-label="Main pages"
      pos="relative"
      p={0}
      style={{ display: 'inline-flex', alignItems: 'stretch' }}
    >
      {targetControl && (
        <FloatingIndicator
          parent={navRootEl}
          target={targetControl}
          transitionDuration={NAV_INDICATOR_TRANSITION_MS}
          styles={{
            root: {
              backgroundColor: selectionFill,
              borderRadius: 5,
              boxShadow: 'none',
              zIndex: 0,
              pointerEvents: 'none',
            },
          }}
        />
      )}
      <Group gap="xs" wrap="nowrap">
        {navItems.map((item) => (
          <NavBarTab
            key={item.to}
            item={item}
            activeRoute={activeRoute}
            controlRef={setControlRef[item.to]}
          />
        ))}
      </Group>
    </Box>
  );
}
