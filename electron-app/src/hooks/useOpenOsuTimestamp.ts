import { notifications } from '@mantine/notifications';
import { useCallback } from 'react';
import { buildOsuEditHref } from '../components/common/osuLinkUtils.ts';
import { useSettings } from '../context/SettingsContext';

export function useOpenOsuTimestamp() {
  const { settings } = useSettings();

  return useCallback(
    async (timestamp: string) => {
      const url = timestamp.startsWith('osu:') ? timestamp : buildOsuEditHref(timestamp);
      const openOsuUrl = window.electronAPI?.shell.openOsuUrl;
      if (!openOsuUrl) {
        window.location.href = url;
        return;
      }

      const target = settings.timestampOpenTarget;
      const path =
        target === 'stable'
          ? settings.timestampOpenStablePath
          : target === 'lazer'
            ? settings.timestampOpenLazerPath
            : target === 'custom'
              ? settings.timestampOpenCustomCommand
              : undefined;

      const result = await openOsuUrl({
        url,
        target,
        path,
        songsFolder: settings.songFolder,
      });

      if (!result.ok) {
        notifications.show({
          message: result.error ?? 'Could not open the timestamp in osu!.',
          color: 'red',
        });
      }
    },
    [
      settings.songFolder,
      settings.timestampOpenCustomCommand,
      settings.timestampOpenLazerPath,
      settings.timestampOpenStablePath,
      settings.timestampOpenTarget,
    ]
  );
}
