import { IconCamera, IconCheck, IconHome, IconTimeline } from '@tabler/icons-react';

export interface NavEntry {
  to: string;
  icon: typeof IconHome;
  label: string;
}

export const navItems: NavEntry[] = [
  {
    to: '/',
    icon: IconHome,
    label: 'Home',
  },
  {
    to: '/checks',
    icon: IconCheck,
    label: 'Checks',
  },
  {
    to: '/snapshots',
    icon: IconCamera,
    label: 'Snapshots',
  },
  {
    to: '/overview',
    icon: IconTimeline,
    label: 'Overview',
  },
];

export const NAV_INDICATOR_TRANSITION_MS = 220;

/** Height of every navbar control (page links, tips, documentation and settings buttons). */
export const NAV_CONTROL_SIZE = 32;

/** Icon size inside the navbar controls. */
export const NAV_ICON_SIZE = 20;

/**
 * From this window width the top bar shows "Mapset Verifier" next to the logo; below it only the
 * logo and version. The startup bar and the nav bar both use it, so the name never appears or
 * vanishes when the app finishes loading.
 */
export const SHOW_APP_NAME_QUERY = '(min-width: 1150px)';

/** From this window width the page links show their names; below it only their icons. */
export const SHOW_NAV_LABELS_QUERY = '(min-width: 1050px)';

export function getActiveNavRoute(pathname: string): string {
  return (
    navItems.find((item) => (item.to === '/' ? pathname === '/' : pathname.startsWith(item.to)))
      ?.to ?? ''
  );
}
