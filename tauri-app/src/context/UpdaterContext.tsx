import { check, type DownloadEvent, type Update } from '@tauri-apps/plugin-updater';
import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';

export type UpdaterStatus =
  | 'idle'
  | 'checking'
  | 'up-to-date'
  | 'available'
  | 'downloading'
  | 'installing'
  | 'installed'
  | 'error';

interface CheckForUpdatesOptions {
  silent?: boolean;
  openModal?: boolean;
  openModalOnAvailable?: boolean;
}

interface UpdaterContextType {
  opened: boolean;
  currentVersion: string;
  status: UpdaterStatus;
  availableUpdate: Update | null;
  errorMessage: string | null;
  downloadedBytes: number;
  totalBytes: number | null;
  progress: number;
  completedVersion: string | null;
  checkForUpdates: (options?: CheckForUpdatesOptions) => Promise<Update | null>;
  openUpdater: () => Promise<void>;
  closeUpdater: () => void;
  installUpdate: () => Promise<void>;
}

const UpdaterContext = createContext<UpdaterContextType | undefined>(undefined);
let startupCheckTriggered = false;

function isTauriRuntime() {
  return typeof window !== 'undefined' && ('__TAURI_INTERNALS__' in window || '__TAURI__' in window);
}

export const UpdaterProvider = ({ children }: { children: React.ReactNode }) => {
  const currentVersion = typeof TAURI_APP_VERSION !== 'undefined' ? TAURI_APP_VERSION : 'unknown';
  const [opened, setOpened] = useState(false);
  const [status, setStatus] = useState<UpdaterStatus>('idle');
  const [availableUpdate, setAvailableUpdate] = useState<Update | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadedBytes, setDownloadedBytes] = useState(0);
  const [totalBytes, setTotalBytes] = useState<number | null>(null);
  const [progress, setProgress] = useState(0);
  const [completedVersion, setCompletedVersion] = useState<string | null>(null);
  const updateHandleRef = useRef<Update | null>(null);

  const clearUpdateHandle = useCallback(async () => {
    if (updateHandleRef.current) {
      try {
        await updateHandleRef.current.close();
      } catch {
        // eslint-disable-next-line no-console
        console.info('[Updater] Failed to close stale update handle.');
      }
    }

    updateHandleRef.current = null;
    setAvailableUpdate(null);
  }, []);

  const resetProgress = useCallback(() => {
    setDownloadedBytes(0);
    setTotalBytes(null);
    setProgress(0);
  }, []);

  const checkForUpdates = useCallback(
    async ({ silent = false, openModal = false, openModalOnAvailable = false }: CheckForUpdatesOptions = {}) => {
      if (!isTauriRuntime()) {
        // eslint-disable-next-line no-console
        console.info('[Updater] Skipping update check outside the Tauri runtime.');
        await clearUpdateHandle();
        setErrorMessage(null);
        setStatus('idle');
        return null;
      }

      await clearUpdateHandle();
      resetProgress();
      setCompletedVersion(null);
      setErrorMessage(null);

      if (openModal) {
        setOpened(true);
      }

      if (!silent) {
        setStatus('checking');
      }

      try {
        const update = await check();

        if (!update) {
          // eslint-disable-next-line no-console
          console.info('[Updater] No update available.');
          setStatus(silent ? 'idle' : 'up-to-date');
          return null;
        }

        updateHandleRef.current = update;
        setAvailableUpdate(update);
        setStatus('available');

        if (openModalOnAvailable) {
          setOpened(true);
        }

        return update;
      } catch (error) {
        // eslint-disable-next-line no-console
        console.error('[Updater] Failed to check for updates:', error);
        if (silent) {
          setStatus('idle');
          setErrorMessage(null);
          return null;
        }

        setStatus('error');
        setErrorMessage(error instanceof Error ? error.message : 'Failed to check for updates.');
        return null;
      }
    },
    [clearUpdateHandle, resetProgress]
  );

  const installUpdate = useCallback(async () => {
    if (!availableUpdate) return;

    setOpened(true);
    setStatus('downloading');
    setErrorMessage(null);
    resetProgress();

    try {
      let downloaded = 0;
      let contentLength: number | null = null;

      await availableUpdate.downloadAndInstall((event: DownloadEvent) => {
        switch (event.event) {
          case 'Started':
            contentLength = event.data.contentLength ?? null;
            setTotalBytes(contentLength);
            break;
          case 'Progress':
            downloaded += event.data.chunkLength;
            setDownloadedBytes(downloaded);
            if (contentLength && contentLength > 0) {
              setProgress(Math.min(100, Math.round((downloaded / contentLength) * 100)));
            }
            break;
          case 'Finished':
            setStatus('installing');
            setProgress(100);
            break;
        }
      });

      setCompletedVersion(availableUpdate.version);
      setStatus('installed');
      setProgress(100);
      await clearUpdateHandle();
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('[Updater] Failed to download or install the update:', error);
      setStatus('error');
      setErrorMessage(error instanceof Error ? error.message : 'Failed to download and install the update.');
    }
  }, [availableUpdate, clearUpdateHandle, resetProgress]);

  const closeUpdater = useCallback(() => {
    if (status === 'checking' || status === 'downloading' || status === 'installing') {
      return;
    }

    setOpened(false);
    if (status === 'available') {
      void clearUpdateHandle();
    }
  }, [status]);

  const openUpdater = useCallback(async () => {
    await checkForUpdates({ silent: false, openModal: true });
  }, [checkForUpdates]);

  useEffect(() => {
    if (startupCheckTriggered) return;
    startupCheckTriggered = true;
    void checkForUpdates({ silent: true, openModalOnAvailable: true });
  }, [checkForUpdates]);

  const value = useMemo(
    () => ({
      opened,
      currentVersion,
      status,
      availableUpdate,
      errorMessage,
      downloadedBytes,
      totalBytes,
      progress,
      completedVersion,
      checkForUpdates,
      openUpdater,
      closeUpdater,
      installUpdate,
    }),
    [availableUpdate, checkForUpdates, closeUpdater, completedVersion, currentVersion, downloadedBytes, errorMessage, installUpdate, openUpdater, opened, progress, status, totalBytes]
  );

  return <UpdaterContext.Provider value={value}>{children}</UpdaterContext.Provider>;
};

export const useUpdater = () => {
  const context = useContext(UpdaterContext);
  if (!context) throw new Error('useUpdater must be used within UpdaterProvider');
  return context;
};
