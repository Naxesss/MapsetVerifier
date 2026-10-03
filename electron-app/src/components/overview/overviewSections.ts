import {
  IconAdjustmentsHorizontal,
  IconChartLine,
  IconLayoutDashboard,
  IconMusic,
  IconInfoCircle,
  IconTimeline,
  IconVideo,
  type Icon,
} from '@tabler/icons-react';
import type { OverviewTab } from '../navbar/pageHints.tsx';

export const SECTION_ICONS: Record<OverviewTab, Icon> = {
  Summary: IconLayoutDashboard,
  General: IconInfoCircle,
  Objects: IconTimeline,
  Beatmap: IconAdjustmentsHorizontal,
  Difficulty: IconChartLine,
  Audio: IconMusic,
  Video: IconVideo,
};

/**
 * The pages that compare difficulties, and so follow the game mode and difficulty picks. The
 * summary always covers every difficulty.
 */
const DIFFICULTY_SECTIONS = new Set<OverviewTab>(['Objects', 'Beatmap', 'Difficulty']);

export const usesDifficultyPicks = (section: OverviewTab) => DIFFICULTY_SECTIONS.has(section);
