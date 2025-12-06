import { appConfigDir } from '@tauri-apps/api/path';
import { readTextFile, writeTextFile, exists, BaseDirectory, mkdir } from '@tauri-apps/plugin-fs';
import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';

// Type-safe Settings type
export type Settings = {
  songFolder?: string;
  showMinor: boolean;
  showGamemodeDifficultyNames: boolean;
  showSnapshotAdditionalInfo: boolean;
  // DEV-only: whether to gate Backend in development mode
  gateInDev: boolean;
};

// Context type includes settings and setter
interface SettingsContextType {
  settings: Settings;
  setSettings: React.Dispatch<React.SetStateAction<Settings>>;
}

const defaultSettings: Settings = {
  songFolder: undefined,
  showMinor: false,
  showGamemodeDifficultyNames: true,
  showSnapshotAdditionalInfo: false,
  gateInDev: false,
};

const MAIN_SETTINGS_FILE = 'settings.json';

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export const SettingsProvider = ({ children }: { children: ReactNode }) => {
  const [settings, setSettings] = useState<Settings>(defaultSettings);

  async function ensureAppConfigDir() {
    try {
      await mkdir('', { baseDir: BaseDirectory.AppConfig, recursive: true });
      const dir = await appConfigDir();
      return dir;
    } catch (e) {
      console.error('[Settings] Error ensuring AppConfig directory:', e);
    }
  }

  async function loadSettings() {
    await ensureAppConfigDir();
    try {
      const fileExists = await exists(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
      if (!fileExists) {
        setSettings(defaultSettings);
        return;
      }
      const text = await readTextFile(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
      const loaded = JSON.parse(text);
      // Merge loaded settings with defaults to ensure new keys exist
      setSettings({ ...defaultSettings, ...loaded });
    } catch (e) {
      console.error('[Settings] Error loading settings. Using defaults.', e);
      setSettings(defaultSettings);
    }
  }

  async function saveSettings(newSettings: Settings) {
    await ensureAppConfigDir();
    try {
      await writeTextFile(MAIN_SETTINGS_FILE, JSON.stringify(newSettings, null, 2), {
        baseDir: BaseDirectory.AppConfig,
      });
    } catch (e) {
      console.error('[Settings] Error saving settings:', e);
    }
  }

  async function findOsuLocationAndSet() {
    try {
      const res = await fetch('http://localhost:5005/beatmap/songsFolder');

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
    <SettingsContext.Provider value={{ settings, setSettings }}>
      {children}
    </SettingsContext.Provider>
  );
};

export const useSettings = () => {
  const context = useContext(SettingsContext);
  if (!context) throw new Error('useSettings must be used within SettingsProvider');
  return context;
};
