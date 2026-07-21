import {
  IconAdjustments,
  IconAnalyze,
  IconChartAreaLine,
  IconCode,
  IconInfoCircle,
  IconPackage,
  IconRefresh,
  IconSettings,
} from '@tabler/icons-react';

export type SettingsSectionId =
  | 'general'
  | 'checks'
  | 'overview'
  | 'experimental'
  | 'plugins'
  | 'updates'
  | 'about'
  | 'developer';

export interface SettingsNavEntry {
  id: SettingsSectionId;
  label: string;
  description: string;
  icon: typeof IconSettings;
  devOnly?: boolean;
}

export const settingsSections: SettingsNavEntry[] = [
  {
    id: 'general',
    label: 'General',
    description: 'Folders, fonts, and core preferences',
    icon: IconSettings,
  },
  {
    id: 'checks',
    label: 'Checks',
    description: 'Result display and check-run behavior',
    icon: IconAdjustments,
  },
  {
    id: 'overview',
    label: 'Overview',
    description: 'What the beatmap overview page displays',
    icon: IconChartAreaLine,
  },
  {
    id: 'experimental',
    label: 'Experimental',
    description: 'Features that are still being tested',
    icon: IconAnalyze,
  },
  {
    id: 'plugins',
    label: 'Plugins',
    description: 'Custom check plugin status',
    icon: IconPackage,
  },
  {
    id: 'updates',
    label: 'Updates',
    description: 'Version checks and beta releases',
    icon: IconRefresh,
  },
  {
    id: 'about',
    label: 'About',
    description: 'Folders and project links',
    icon: IconInfoCircle,
  },
  {
    id: 'developer',
    label: 'Developer',
    description: 'Development-only backend controls',
    icon: IconCode,
    devOnly: true,
  },
];

export function resolveSettingsSection(
  section: string | undefined,
  isDev: boolean
): SettingsSectionId | null {
  const visibleSections = settingsSections.filter((entry) => isDev || !entry.devOnly);
  const routeSection = section === 'analysis' ? 'experimental' : section;
  const requestedSection = (routeSection ?? 'general') as SettingsSectionId;
  return visibleSections.some((entry) => entry.id === requestedSection) ? requestedSection : null;
}
