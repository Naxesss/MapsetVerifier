import { IconBook, IconCamera, IconCheck, IconHome, IconTimeline } from '@tabler/icons-react';

export interface NavEntry {
  to: string;
  icon: typeof IconHome;
  label: string;
  disabled?: boolean;
}

export const navItems: NavEntry[] = [
  {
    to: '/',
    icon: IconHome,
    label: 'Home',
  },
  {
    to: '/documentation',
    icon: IconBook,
    label: 'Documentation',
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
    disabled: false,
  },
  {
    to: '/overview',
    icon: IconTimeline,
    label: 'Overview',
    disabled: false,
  },
];

export const NAV_INDICATOR_TRANSITION_MS = 220;

export function getActiveNavRoute(pathname: string): string {
  return (
    navItems.find((item) => {
      if (item.disabled) {
        return false;
      }
      return item.to === '/' ? pathname === '/' : pathname.startsWith(item.to);
    })?.to ?? ''
  );
}
