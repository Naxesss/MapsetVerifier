import { appConfigDir } from '@tauri-apps/api/path';
import { readTextFile, writeTextFile, exists, BaseDirectory, mkdir } from '@tauri-apps/plugin-fs';
import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';

// Type-safe Settings type
export type Settings = {
  songFolder?: string;
  showMinor: boolean;
};

// Context type includes settings and setter
interface SettingsContextType {
  settings: Settings;
  setSettings: React.Dispatch<React.SetStateAction<Settings>>;
}

const defaultSettings: Settings = {
  songFolder: undefined,
  showMinor: false,
};

const MAIN_SETTINGS_FILE = 'settings.json';

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export const SettingsProvider = ({ children }: { children: ReactNode }) => {
  console.warn('[SettingsProvider] Mounted');
  const [settings, setSettings] = useState<Settings>(defaultSettings);

  async function ensureAppConfigDir() {
    try {
      console.warn('[Settings] Ensuring AppConfig directory exists');
      await mkdir('', { baseDir: BaseDirectory.AppConfig, recursive: true });
      const dir = await appConfigDir();
      console.warn('[Settings] BaseDirectory.AppConfig resolved path:', dir);
      return dir;
    } catch (e) {
      console.error('[Settings] Error ensuring AppConfig directory:', e);
    }
  }

  async function loadSettings() {
    await ensureAppConfigDir();
    try {
      console.warn('[Settings] Checking if settings file exists in AppConfig:', MAIN_SETTINGS_FILE);
      const fileExists = await exists(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
      if (!fileExists) {
        console.warn('[Settings] Settings file does not exist. Using default settings.');
        setSettings(defaultSettings);
        return;
      }
      console.warn('[Settings] Settings file exists. Reading file...');
      const text = await readTextFile(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
      const loaded = JSON.parse(text);
      console.warn('[Settings] Loaded settings:', loaded);
      setSettings({ ...defaultSettings, ...loaded });
    } catch (e) {
      console.error('[Settings] Error loading settings. Using defaults.', e);
      setSettings(defaultSettings);
    }
  }

  async function saveSettings(newSettings: Settings) {
    await ensureAppConfigDir();
    try {
      console.warn('[Settings] Saving settings to AppConfig:', MAIN_SETTINGS_FILE, newSettings);
      await writeTextFile(MAIN_SETTINGS_FILE, JSON.stringify(newSettings, null, 2), {
        baseDir: BaseDirectory.AppConfig,
      });
      console.warn('[Settings] Settings saved successfully.');
    } catch (e) {
      console.error('[Settings] Error saving settings:', e);
    }
  }

  async function findOsuLocationAndSet() {
    try {
      console.warn('[Settings] Invoking find_osu_location command...');
      const res = await fetch('http://localhost:5005/beatmap/songsFolder');

      if (res.ok) {
        const data = await res.json();
        const songFolder = data.songsFolder;
        console.warn('[Settings] Found osu! location via API:', songFolder);
        setSettings((prev) => ({ ...prev, songFolder: songFolder }));
        return;
      } else {
        console.warn('[Settings] osu! location not found.');
      }
    } catch (e) {
      console.error('[Settings] Error finding osu! location:', e);
    }
  }

  useEffect(() => {
    loadSettings().then(() => {
      console.warn('[Settings] Settings loaded.');
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
