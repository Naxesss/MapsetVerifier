import {
  IconAdjustmentsHorizontal,
  IconChartLine,
  IconMusic,
  IconInfoCircle,
  IconTimeline,
  IconVideo,
  type Icon,
} from '@tabler/icons-react';
import type { OverviewTab } from '../navbar/pageHints.tsx';

export const SECTION_ICONS: Record<OverviewTab, Icon> = {
  General: IconInfoCircle,
  Objects: IconTimeline,
  Beatmap: IconAdjustmentsHorizontal,
  Difficulty: IconChartLine,
  Audio: IconMusic,
  Video: IconVideo,
};

/** The Overview's pages in the order the views list them. */
export const OVERVIEW_SECTIONS: OverviewTab[] = [
  'General',
  'Objects',
  'Beatmap',
  'Difficulty',
  'Audio',
  'Video',
];

/** The pages that compare difficulties, and so follow the game mode and difficulty picks. */
const DIFFICULTY_SECTIONS = new Set<OverviewTab>(['Objects', 'Beatmap', 'Difficulty']);

export const usesDifficultyPicks = (section: OverviewTab) => DIFFICULTY_SECTIONS.has(section);
