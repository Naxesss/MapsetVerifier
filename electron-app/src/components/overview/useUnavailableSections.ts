import { useVideoAnalysis } from './video/hooks/useVideoAnalysis.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import type { OverviewTab } from '../navbar/pageHints.tsx';

/**
 * The Overview pages that have nothing to show for this mapset, with why. They stay in the page
 * menu, greyed out, and are skipped when stepping through the pages. A page counts as available
 * until its analysis says otherwise.
 */
export function useUnavailableSections(): Partial<Record<OverviewTab, string>> {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const { data } = useVideoAnalysis({ folder, songFolder: settings.songFolder });

  return data?.success && data.videos.length === 0 ? { Video: 'No video' } : {};
}
