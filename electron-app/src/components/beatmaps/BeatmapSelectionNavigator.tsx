import { useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';

const BEATMAP_ROUTES = new Set(['/checks', '/snapshots', '/overview']);

function shouldNavigateToChecks(pathname: string, goToChecksOnMapsetSwitch: boolean) {
  if (pathname === '/checks') return false;

  if (goToChecksOnMapsetSwitch) {
    return true;
  }

  return !BEATMAP_ROUTES.has(pathname);
}

export default function BeatmapSelectionNavigator() {
  const { beatmapFolderPath } = useBeatmap();
  const { settings } = useSettings();
  const location = useLocation();
  const navigate = useNavigate();
  const prevPathRef = useRef(beatmapFolderPath);
  const skipInitialRef = useRef(true);

  useEffect(() => {
    if (skipInitialRef.current) {
      skipInitialRef.current = false;
      prevPathRef.current = beatmapFolderPath;
      return;
    }

    if (beatmapFolderPath === prevPathRef.current) return;
    prevPathRef.current = beatmapFolderPath;

    if (!beatmapFolderPath) return;
    if (!shouldNavigateToChecks(location.pathname, settings.goToChecksOnMapsetSwitch)) return;

    navigate('/checks', { viewTransition: true });
  }, [beatmapFolderPath, location.pathname, navigate, settings.goToChecksOnMapsetSwitch]);

  return null;
}
