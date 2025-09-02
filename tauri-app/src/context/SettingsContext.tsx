import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { readTextFile, writeTextFile, exists, BaseDirectory, mkdir } from '@tauri-apps/plugin-fs';

// Type-safe Settings type
export type Settings = {
    songFolder: string;
    showMinor: boolean;
    showListing: boolean;
};

// Context type includes settings and setter
interface SettingsContextType {
    settings: Settings;
    setSettings: React.Dispatch<React.SetStateAction<Settings>>;
}

const defaultSettings: Settings = {
    songFolder: '',
    showMinor: false,
    showListing: true,
};

const MAIN_SETTINGS_FILE = 'settings.json';

const SettingsContext = createContext<SettingsContextType | undefined>(undefined);

export const SettingsProvider = ({ children }: { children: ReactNode }) => {
    console.log('[SettingsProvider] Mounted');
    const [settings, setSettings] = useState<Settings>(defaultSettings);

    async function ensureAppConfigDir() {
        try {
            await mkdir('', { baseDir: BaseDirectory.AppConfig, recursive: true });
            console.log('[Settings] Ensured AppConfig directory exists');
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

    useEffect(() => {
        loadSettings();
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