import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { readTextFile, writeTextFile, exists, BaseDirectory, mkdir } from '@tauri-apps/plugin-fs';
import {appConfigDir} from "@tauri-apps/api/path";

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
    console.log('[SettingsProvider] Mounted');
    const [settings, setSettings] = useState<Settings>(defaultSettings);

    async function ensureAppConfigDir() {
        try {
            console.log('[Settings] Ensuring AppConfig directory exists');
            await mkdir('', { baseDir: BaseDirectory.AppConfig, recursive: true });
            const dir = await appConfigDir();
            console.log('[Settings] BaseDirectory.AppConfig resolved path:', dir);
            return dir;
        } catch (e) {
            console.error('[Settings] Error ensuring AppConfig directory:', e);
        }
    }

    async function loadSettings() {
        await ensureAppConfigDir();
        try {
            console.log('[Settings] Checking if settings file exists in AppConfig:', MAIN_SETTINGS_FILE);
            const fileExists = await exists(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
            if (!fileExists) {
                console.log('[Settings] Settings file does not exist. Using default settings.');
                setSettings(defaultSettings);
                return;
            }
            console.log('[Settings] Settings file exists. Reading file...');
            const text = await readTextFile(MAIN_SETTINGS_FILE, { baseDir: BaseDirectory.AppConfig });
            const loaded = JSON.parse(text);
            console.log('[Settings] Loaded settings:', loaded);
            setSettings({ ...defaultSettings, ...loaded });
        } catch (e) {
            console.error('[Settings] Error loading settings. Using defaults.', e);
            setSettings(defaultSettings);
        }
    }

    async function saveSettings(newSettings: Settings) {
        await ensureAppConfigDir();
        try {
            console.log('[Settings] Saving settings to AppConfig:', MAIN_SETTINGS_FILE, newSettings);
            await writeTextFile(MAIN_SETTINGS_FILE, JSON.stringify(newSettings, null, 2), { baseDir: BaseDirectory.AppConfig });
            console.log('[Settings] Settings saved successfully.');
        } catch (e) {
            console.error('[Settings] Error saving settings:', e);
        }
    }

    async function findOsuLocationAndSet() {
      try {
        console.log('[Settings] Invoking find_osu_location command...');
        const res = await fetch("http://localhost:5005/beatmap/songsFolder");
        
        if (res.ok) {
            const data = await res.json();
            let songFolder = data.songsFolder;
            console.log('[Settings] Found osu! location via API:', songFolder);
            setSettings(prev => ({ ...prev, songFolder: songFolder }));
            return;
        } else {
          console.log('[Settings] osu! location not found.');
        }
      } catch (e) {
        console.error('[Settings] Error finding osu! location:', e);
      }
    }

    useEffect(() => {
        loadSettings().then(() => {
            console.log('[Settings] Settings loaded.');
            findOsuLocationAndSet();
        })
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