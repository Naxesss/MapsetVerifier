import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { BACKEND_BASE_URL } from '../Constants.ts';

// Type-safe Settings type
export type Settings = {
  songFolder?: string;
  showMinor: boolean;
  showGamemodeDifficultyNames: boolean;
  showSnapshotDiffView: boolean;
  showAdvancedAudioAnalysis: boolean;
  lazerLookupEnabled: boolean;
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
  showGamemodeDifficultyNames: true,
  showSnapshotDiffView: false,
  showAdvancedAudioAnalysis: false,
  lazerLookupEnabled: false,
  gateInDev: false,
};

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export const SettingsProvider = ({ children }: { children: ReactNode }) => {
  const [loaded, setLoaded] = useState(false);
  const [settings, setSettings] = useState<Settings>(defaultSettings);

  async function loadSettings() {
    const api = window.electronAPI;
    if (!api) {
      setSettings(defaultSettings);
      setLoaded(true);
      return;
    }
    try {
      const text = await api.settings.read();
      if (!text) {
        setSettings(defaultSettings);
      } else {
        const loaded = JSON.parse(text);

        if (
          loaded?.lazerLookupEnabled === undefined &&
          loaded?.experimentalLazerLookup !== undefined
        ) {
          loaded.lazerLookupEnabled = loaded.experimentalLazerLookup;
        }

        setSettings({ ...defaultSettings, ...loaded });
      }
    } catch (e) {
      console.error('[Settings] Error loading settings. Using defaults.', e);
      setSettings(defaultSettings);
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

  async function findOsuLocationAndSet() {
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
    loadSettings().then(() => {
      findOsuLocationAndSet();
    });
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
