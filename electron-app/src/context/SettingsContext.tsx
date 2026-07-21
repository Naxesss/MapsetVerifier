import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { parseTimelineThemeVariant } from '../components/overview/objects/timelineTheme/selection.ts';
import { BACKEND_BASE_URL } from '../Constants.ts';
import { DEFAULT_UI_FONT_FAMILY, parseUiFontFamily, type UiFontFamily } from '../theme/fonts';
import { isSemverPreRelease } from '../utils/isSemverPreRelease';
import type { TimelineThemeVariant } from '../components/overview/objects/timelineTheme/types.ts';

/** What the Star Rating overview chart shows by default: the cumulative line, the strain overlay, or both. */
export type DifficultyStrainDisplayMode = 'both' | 'starRatingOnly' | 'strainOnly';

// Type-safe Settings type
export type Settings = {
  songFolder?: string;
  showMinor: boolean;
  /** When showMinor is on, Minor results for these check ids are hidden. When showMinor is off, ignored. */
  hiddenMinorCheckIds: number[];
  showGamemodeDifficultyNames: boolean;
  showAdvancedAudioAnalysis: boolean;
  /** What the Star Rating overview chart shows by default. */
  difficultyStrainDisplayMode: DifficultyStrainDisplayMode;
  /** Excludes osu!standard's Aim skill(s) from the combined strain line. */
  excludeAimFromCombinedStrain: boolean;
  lazerLookupEnabled: boolean;
  receivePrereleases: boolean;
  uiFontFamily: UiFontFamily;
  /** Objects overview timeline circle style (synced across game modes). */
  timelineThemeVariant: TimelineThemeVariant;
  /** When enabled, switching mapsets navigates to the checks tab. */
  goToChecksOnMapsetSwitch: boolean;
  /** Show "what changed since last check run" on the checks tab. */
  showCheckRunDelta: boolean;
  /** Include the unchanged-issues tab in the check run delta panel. */
  checkRunDeltaShowUnchanged: boolean;
  /** Automatically create a snapshot before running checks. */
  autoCreateSnapshotOnCheckRun: boolean;
  /** Load external check DLLs from CustomChecks. */
  customChecksEnabled: boolean;
  /** Enables bookmarking/pinning beatmapsets for quick lookup in the sidebar. */
  bookmarksEnabled: boolean;
  /** Folder names of bookmarked beatmapsets. */
  bookmarkedFolders: string[];
  // DEV-only: whether to gate Backend in development mode
  gateInDev: boolean;
};

// Context type includes settings and setter
interface SettingsContextType {
  settings: Settings;
  loaded: boolean;
  setSettings: React.Dispatch<React.SetStateAction<Settings>>;
}

const defaultSettings: Settings = {
  songFolder: undefined,
  showMinor: false,
  hiddenMinorCheckIds: [],
  showGamemodeDifficultyNames: true,
  showAdvancedAudioAnalysis: false,
  difficultyStrainDisplayMode: 'strainOnly',
  excludeAimFromCombinedStrain: true,
  lazerLookupEnabled: false,
  receivePrereleases: false,
  uiFontFamily: DEFAULT_UI_FONT_FAMILY,
  timelineThemeVariant: 'default',
  goToChecksOnMapsetSwitch: true,
  showCheckRunDelta: true,
  checkRunDeltaShowUnchanged: false,
  autoCreateSnapshotOnCheckRun: true,
  customChecksEnabled: false,
  bookmarksEnabled: false,
  bookmarkedFolders: [],
  gateInDev: false,
};

async function resolveDefaultReceivePrereleases() {
  const api = window.electronAPI;
  if (!api) return defaultSettings.receivePrereleases;

  try {
    const version = await api.getVersion();
    return isSemverPreRelease(version);
  } catch {
    return defaultSettings.receivePrereleases;
  }
}

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export const SettingsProvider = ({ children }: { children: ReactNode }) => {
  const [loaded, setLoaded] = useState(false);
  const [settings, setSettings] = useState<Settings>(defaultSettings);

  async function loadSettings(): Promise<string | undefined> {
    const api = window.electronAPI;
    const inferredReceivePrereleases = await resolveDefaultReceivePrereleases();

    if (!api) {
      setSettings({
        ...defaultSettings,
        receivePrereleases: inferredReceivePrereleases,
      });
      setLoaded(true);
      return undefined;
    }

    try {
      const text = await api.settings.read();
      if (!text) {
        setSettings({
          ...defaultSettings,
          receivePrereleases: inferredReceivePrereleases,
        });
        return undefined;
      }

      const loaded = JSON.parse(text);

      if (
        loaded?.lazerLookupEnabled === undefined &&
        loaded?.experimentalLazerLookup !== undefined
      ) {
        loaded.lazerLookupEnabled = loaded.experimentalLazerLookup;
      }

      if (loaded?.receivePrereleases === undefined) {
        loaded.receivePrereleases = inferredReceivePrereleases;
      }

      setSettings({
        ...defaultSettings,
        ...loaded,
        hiddenMinorCheckIds: Array.isArray(loaded?.hiddenMinorCheckIds)
          ? loaded.hiddenMinorCheckIds
          : defaultSettings.hiddenMinorCheckIds,
        bookmarkedFolders: Array.isArray(loaded?.bookmarkedFolders)
          ? loaded.bookmarkedFolders
          : defaultSettings.bookmarkedFolders,
        uiFontFamily: parseUiFontFamily(loaded?.uiFontFamily),
        timelineThemeVariant: parseTimelineThemeVariant(loaded?.timelineThemeVariant ?? null),
      });

      const savedSongFolder =
        typeof loaded?.songFolder === 'string' ? loaded.songFolder.trim() : undefined;
      return savedSongFolder || undefined;
    } catch (e) {
      console.error('[Settings] Error loading settings. Using defaults.', e);
      setSettings({
        ...defaultSettings,
        receivePrereleases: inferredReceivePrereleases,
      });
      return undefined;
    } finally {
      setLoaded(true);
    }
  }

  async function saveSettings(newSettings: Settings) {
    const api = window.electronAPI;
    if (!api) return;
    try {
      await api.settings.write(JSON.stringify(newSettings, null, 2));
    } catch (e) {
      console.error('[Settings] Error saving settings:', e);
    }
  }

  async function findOsuLocationAndSet(savedSongFolder?: string) {
    if (savedSongFolder) {
      return;
    }

    try {
      const res = await fetch(`${BACKEND_BASE_URL}/beatmap/songsFolder`);

      if (res.ok) {
        const data = await res.json();
        const songFolder = data.songsFolder;
        setSettings((prev) => ({ ...prev, songFolder: songFolder }));
        return;
      }
    } catch (e) {
      console.error('[Settings] Error finding osu! location:', e);
    }
  }

  useEffect(() => {
    let cancelled = false;

    void Promise.resolve().then(() => {
      if (cancelled) return;
      loadSettings().then((savedSongFolder) => {
        if (!cancelled) findOsuLocationAndSet(savedSongFolder);
      });
    });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (settings != defaultSettings) {
      saveSettings(settings);
    }
  }, [settings]);

  return (
    <SettingsContext.Provider value={{ settings, loaded, setSettings }}>
      {children}
    </SettingsContext.Provider>
  );
};

export const useSettings = () => {
  const context = useContext(SettingsContext);
  if (!context) throw new Error('useSettings must be used within SettingsProvider');
  return context;
};
